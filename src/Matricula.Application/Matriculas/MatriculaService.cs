using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Application.Matriculas;

/// <summary>
/// Matrícula, retiro y cambio de grupo (RF-51 a RF-53). El estudiante opera sobre sí mismo (RN-18); al administrador
/// no se le aplican las restricciones de estado del grupo ni de fechas (supuesto 1 de la spec).
/// </summary>
public class MatriculaService(IAppDbContext db, ICurrentUser currentUser, IFechaLocal fechaLocal)
{
    private const string MensajeConcurrencia =
        "El grupo cambió mientras se procesaba la solicitud (por ejemplo, se ocupó el último cupo). Intente de nuevo.";

    /// <summary>RF-51 por un administrador, para cualquier estudiante.</summary>
    public Task<MatriculaResponse> MatricularAsync(MatricularRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // El validador ya exige estos datos.
        return MatricularAsync(request.EstudianteId!.Value, request.GrupoId!.Value, cancellationToken);
    }

    /// <summary>RF-51 para el estudiante de la sesión: su id sale del token, nunca del body (RN-18).</summary>
    public Task<MatriculaResponse> MatricularPropioAsync(
        MatricularPropioRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MatricularAsync(EstudianteActual(), request.GrupoId!.Value, cancellationToken);
    }

    /// <summary>
    /// RF-54: una matrícula. El estudiante solo ve las suyas (RN-18).
    /// </summary>
    public async Task<MatriculaResponse> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var matricula = await db.Matriculas.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(MatriculaProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"No existe la matrícula {id}.");

        if (currentUser.Rol == nameof(Rol.Estudiante) && matricula.EstudianteId != EstudianteActual())
        {
            throw new ForbiddenException("No tiene acceso a esta matrícula.", "RN-18");
        }

        return matricula;
    }

    /// <summary>
    /// RF-52: la matrícula queda Retirada con la fecha de hoy y el cupo se libera. El estudiante solo retira las suyas
    /// (RN-18), con el grupo Abierto (RN-11) y hasta la fecha límite de retiro (RN-10); al administrador no se le
    /// aplican el estado ni la fecha (supuesto 1).
    /// </summary>
    public async Task RetirarAsync(int id, CancellationToken cancellationToken)
    {
        var matricula = await BuscarPropiaAsync(id, cancellationToken);
        var hoy = fechaLocal.Hoy();

        ValidarRetiro(matricula, hoy);

        matricula.Retirar(hoy, MotivoRetiro.RetiroVoluntario);
        matricula.Grupo.LiberarCupo();
        await GuardarAsync(cancellationToken);
    }

    /// <summary>
    /// RF-53: mueve al estudiante a otro grupo del mismo curso y periodo. El origen sigue las reglas del retiro (RN-10,
    /// RN-11) y el destino las de matricular salvo RN-04 (supuesto 3); RN-05 y RN-06 no cuentan la matrícula de origen.
    /// Todo se guarda junto: si algo falla, la matrícula original y los cupos quedan como estaban.
    /// </summary>
    public async Task<MatriculaResponse> CambiarGrupoAsync(
        int id, CambioGrupoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var origen = await BuscarPropiaAsync(id, cancellationToken);
        var hoy = fechaLocal.Hoy();
        ValidarRetiro(origen, hoy);

        // El validador ya exige el destino.
        var destinoId = request.GrupoDestinoId!.Value;
        if (destinoId == origen.GrupoId)
        {
            throw new BusinessRuleException("RF-53", "El estudiante ya está matriculado en ese grupo.");
        }

        var destino = await BuscarGrupoAsync(destinoId, cancellationToken);
        if (destino.CursoId != origen.Grupo.CursoId || destino.PeriodoId != origen.Grupo.PeriodoId)
        {
            throw new BusinessRuleException(
                "RF-53",
                $"El grupo {destino.Nrc} no es del mismo curso y periodo que el grupo {origen.Grupo.Nrc}.");
        }

        await ValidarEstudianteActivoAsync(origen.EstudianteId, cancellationToken);
        destino.ValidarMovimiento(EsAdministrador());
        await db.ValidarCursoNoRepetidoAsync(origen.EstudianteId, destino, origen.Id, cancellationToken);
        await db.ValidarSinChoquesAsync(origen.EstudianteId, destino, origen.Id, cancellationToken);
        destino.OcuparCupo();

        origen.Retirar(hoy, MotivoRetiro.CambioGrupo);
        origen.Grupo.LiberarCupo();
        var nueva = new MatriculaEntidad(origen.EstudianteId, destino.Id, hoy);
        db.Matriculas.Add(nueva);

        // Un solo guardado: es transaccional, y xmin protege los dos grupos.
        await GuardarAsync(cancellationToken);
        return await ObtenerAsync(nueva.Id, cancellationToken);
    }

    // Reglas para dejar un grupo, compartidas por el retiro y el origen de un cambio de grupo (RF-52, RF-53).
    private void ValidarRetiro(MatriculaEntidad matricula, DateOnly hoy)
    {
        if (matricula.Estado == EstadoMatricula.Retirada)
        {
            throw new BusinessRuleException("RF-52", "La matrícula ya está retirada.");
        }

        var esAdministrador = EsAdministrador();
        matricula.Grupo.ValidarMovimiento(esAdministrador);

        var periodo = matricula.Grupo.Periodo;
        if (!esAdministrador && !periodo.PermiteRetiro(hoy))
        {
            throw new BusinessRuleException(
                "RN-10",
                $"La fecha límite de retiro del periodo {periodo.Nombre} fue el {periodo.FechaLimiteRetiro:dd/MM/yyyy}.");
        }
    }

    // Con seguimiento y con el grupo (su periodo y sus bloques), porque retirar y cambiar modifican el grupo. RN-18: un
    // estudiante solo opera sobre sus matrículas.
    private async Task<MatriculaEntidad> BuscarPropiaAsync(int id, CancellationToken cancellationToken)
    {
        var matricula = await db.Matriculas
            .Include(m => m.Grupo).ThenInclude(g => g.Periodo)
            .Include(m => m.Grupo).ThenInclude(g => g.Horarios)
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No existe la matrícula {id}.");

        if (currentUser.Rol == nameof(Rol.Estudiante) && matricula.EstudianteId != EstudianteActual())
        {
            throw new ForbiddenException("No tiene acceso a esta matrícula.", "RN-18");
        }

        return matricula;
    }

    /// <summary>
    /// RF-20 y RF-54 (consultas 2 y 10) para el administrador. Incluye matrículas de grupos y estudiantes inactivos
    /// (RN-40).
    /// </summary>
    public async Task<PagedResult<MatriculaResponse>> ListarDeEstudianteAsync(
        int estudianteId, ListarMatriculasRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Estudiantes.AnyAsync(e => e.Id == estudianteId, cancellationToken))
        {
            throw new NotFoundException($"No existe el estudiante {estudianteId}.");
        }

        return await ListarAsync(estudianteId, request, cancellationToken);
    }

    /// <summary>RF-20 y RF-54 para el estudiante de la sesión (RN-18).</summary>
    public Task<PagedResult<MatriculaResponse>> ListarMiasAsync(
        ListarMatriculasRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ListarAsync(EstudianteActual(), request, cancellationToken);
    }

    /// <summary>
    /// RF-49 (consulta 3): los estudiantes de un grupo, para el administrador o el profesor del grupo (RN-18). Ordena
    /// por apellidos, nombre e id de la matrícula.
    /// </summary>
    public async Task<PagedResult<EstudianteGrupoResponse>> ListarEstudiantesGrupoAsync(
        int grupoId, ListarEstudiantesGrupoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await db.Grupos.AsNoTracking()
            .Where(g => g.Id == grupoId)
            .Select(g => new { g.ProfesorId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"No existe el grupo {grupoId}.");

        if (currentUser.Rol == nameof(Rol.Profesor) && grupo.ProfesorId != ProfesorActual())
        {
            throw new ForbiddenException("Solo puede consultar los estudiantes de sus grupos.", "RN-18");
        }

        var query = db.Matriculas.AsNoTracking().Where(m => m.GrupoId == grupoId && m.Estado == request.Estado);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(m =>
                m.Estudiante.Nombre.ToLower().Contains(termino)
                || m.Estudiante.Apellidos.ToLower().Contains(termino)
                || m.Estudiante.Cedula.ToLower().Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return await query
            .OrderBy(m => m.Estudiante.Apellidos)
            .ThenBy(m => m.Estudiante.Nombre)
            .ThenBy(m => m.Id)
            .Select(m => new EstudianteGrupoResponse(
                m.Id,
                m.EstudianteId,
                m.Estudiante.Cedula,
                m.Estudiante.Nombre,
                m.Estudiante.Apellidos,
                m.Estudiante.Usuario.Correo,
                m.FechaMatricula,
                m.Estado,
                m.FechaRetiro,
                m.MotivoRetiro))
            .ToPagedResultAsync(request, cancellationToken);
    }

    // Periodo más reciente primero, luego curso. Dentro de un mismo curso, la matrícula más nueva primero: tras un
    // cambio de grupo la activa queda antes que la retirada.
    private Task<PagedResult<MatriculaResponse>> ListarAsync(
        int estudianteId, ListarMatriculasRequest request, CancellationToken cancellationToken)
    {
        var query = db.Matriculas.AsNoTracking().Where(m => m.EstudianteId == estudianteId);

        if (request.PeriodoId is { } periodoId)
        {
            query = query.Where(m => m.Grupo.PeriodoId == periodoId);
        }

        if (request.Estado is { } estado)
        {
            query = query.Where(m => m.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(m =>
                m.Grupo.Curso.Nombre.ToLower().Contains(termino)
                || m.Grupo.Curso.Codigo.ToLower().Contains(termino)
                || m.Grupo.Nrc.ToLower().Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query
            .OrderByDescending(m => m.Grupo.Periodo.InicioClases)
            .ThenBy(m => m.Grupo.Curso.Nombre)
            .ThenByDescending(m => m.Id)
            .Select(MatriculaProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    // Orden de RF-51: RN-01, RN-02/RN-11, RN-04, RN-05, RN-06 y al final RN-03, para que un grupo lleno no oculte un
    // error más útil.
    private async Task<MatriculaResponse> MatricularAsync(
        int estudianteId, int grupoId, CancellationToken cancellationToken)
    {
        await ValidarEstudianteActivoAsync(estudianteId, cancellationToken);

        var grupo = await BuscarGrupoAsync(grupoId, cancellationToken);
        var esAdministrador = EsAdministrador();
        var hoy = fechaLocal.Hoy();

        grupo.ValidarMovimiento(esAdministrador);
        if (!esAdministrador && !grupo.Periodo.EnRangoMatricula(hoy))
        {
            throw new BusinessRuleException(
                "RN-04",
                $"La matrícula del periodo {grupo.Periodo.Nombre} es del {grupo.Periodo.InicioMatricula:dd/MM/yyyy} "
                + $"al {grupo.Periodo.FinMatricula:dd/MM/yyyy}.");
        }

        await db.ValidarCursoNoRepetidoAsync(estudianteId, grupo, ignorarMatriculaId: null, cancellationToken);
        await db.ValidarSinChoquesAsync(estudianteId, grupo, ignorarMatriculaId: null, cancellationToken);
        grupo.OcuparCupo();

        var matricula = new MatriculaEntidad(estudianteId, grupo.Id, hoy);
        db.Matriculas.Add(matricula);
        await GuardarAsync(cancellationToken);

        return await ObtenerAsync(matricula.Id, cancellationToken);
    }

    // RN-01. Un estudiante que no existe responde 404.
    private async Task ValidarEstudianteActivoAsync(int estudianteId, CancellationToken cancellationToken)
    {
        var estudianteActivo = await db.Estudiantes.AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => (bool?)e.Usuario.Activo)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"No existe el estudiante {estudianteId}.");

        if (!estudianteActivo)
        {
            throw new BusinessRuleException("RN-01", "El estudiante está inactivo y no se puede matricular.");
        }
    }

    // Con seguimiento: OcuparCupo y LiberarCupo cambian el grupo, y su xmin detecta otra operación simultánea.
    private async Task<Grupo> BuscarGrupoAsync(int grupoId, CancellationToken cancellationToken) =>
        await db.Grupos
            .Include(g => g.Horarios)
            .Include(g => g.Periodo)
            .SingleOrDefaultAsync(g => g.Id == grupoId, cancellationToken)
        ?? throw new NotFoundException($"No existe el grupo {grupoId}.");

    // Supuesto 6 de la spec: el conflicto de xmin se explica en términos del grupo.
    private async Task GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleException("CONCURRENCIA", MensajeConcurrencia);
        }
    }

    private bool EsAdministrador() => currentUser.Rol == nameof(Rol.Administrador);

    // Un token emitido antes de que existiera el claim no lo trae: basta con iniciar sesión de nuevo.
    private int ProfesorActual() =>
        currentUser.ProfesorId
        ?? throw new ForbiddenException("Su sesión no identifica a un profesor. Inicie sesión de nuevo.");

    private int EstudianteActual() =>
        currentUser.EstudianteId
        ?? throw new ForbiddenException("Su sesión no identifica a un estudiante. Inicie sesión de nuevo.");
}
