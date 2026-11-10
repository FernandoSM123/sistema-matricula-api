using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Matriculas;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Matricula.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Grupos;

public class GrupoService(IAppDbContext db, ICurrentUser currentUser, IFechaLocal fechaLocal)
{
    /// <summary>
    /// RF-43 con la visibilidad de RN-18: el profesor solo ve sus grupos y el estudiante solo los Abierto (supuesto 7),
    /// aunque envíen otro filtro. Ordena por inicio de clases (más reciente primero), curso y NRC.
    /// </summary>
    public Task<PagedResult<GrupoResponse>> ListarAsync(ListarGruposRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profesorId = request.ProfesorId;
        var estado = request.Estado;
        switch (currentUser.Rol)
        {
            case nameof(Rol.Profesor):
                profesorId = ProfesorActual();
                break;
            case nameof(Rol.Estudiante):
                estado = EstadoGrupo.Abierto;
                break;
        }

        return ConsultarAsync(
            request, request.CursoId, profesorId, request.PeriodoId, estado, request.Modalidad, cancellationToken);
    }

    /// <summary>
    /// RF-34 (consulta 4): los grupos de un curso. El administrador filtra por cualquier estado; el resto solo ve los
    /// Abierto, también el profesor, que aquí no se limita a los suyos (supuesto 7).
    /// </summary>
    public async Task<PagedResult<GrupoResponse>> ListarPorCursoAsync(
        int cursoId, ListarGruposCursoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Cursos.AnyAsync(c => c.Id == cursoId, cancellationToken))
        {
            throw new NotFoundException($"No existe el curso {cursoId}.");
        }

        var estado = currentUser.Rol == nameof(Rol.Administrador) ? request.Estado : EstadoGrupo.Abierto;

        return await ConsultarAsync(
            request, cursoId, profesorId: null, request.PeriodoId, estado, modalidad: null, cancellationToken);
    }

    /// <summary>
    /// RF-27 (consulta 5) para el administrador: los grupos de un profesor, en todos los estados.
    /// </summary>
    public async Task<PagedResult<GrupoResponse>> ListarDeProfesorAsync(
        int profesorId, ListarGruposProfesorRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Profesores.AnyAsync(p => p.Id == profesorId, cancellationToken))
        {
            throw new NotFoundException($"No existe el profesor {profesorId}.");
        }

        return await ConsultarAsync(
            request, cursoId: null, profesorId, request.PeriodoId, estado: null, modalidad: null, cancellationToken);
    }

    /// <summary>
    /// RF-27 (consulta 5) para el profesor de la sesión.
    /// </summary>
    public Task<PagedResult<GrupoResponse>> ListarMisGruposAsync(
        ListarGruposProfesorRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ConsultarAsync(
            request, cursoId: null, ProfesorActual(), request.PeriodoId, estado: null, modalidad: null, cancellationToken);
    }

    // Filtros comunes de los listados; la visibilidad por rol ya viene aplicada en los parámetros.
    private Task<PagedResult<GrupoResponse>> ConsultarAsync(
        PaginacionRequest request,
        int? cursoId,
        int? profesorId,
        int? periodoId,
        EstadoGrupo? estado,
        Modalidad? modalidad,
        CancellationToken cancellationToken)
    {
        var query = db.Grupos.AsNoTracking();

        if (cursoId is { } c)
        {
            query = query.Where(g => g.CursoId == c);
        }

        if (profesorId is { } p)
        {
            query = query.Where(g => g.ProfesorId == p);
        }

        if (periodoId is { } pe)
        {
            query = query.Where(g => g.PeriodoId == pe);
        }

        if (estado is { } e)
        {
            query = query.Where(g => g.Estado == e);
        }

        if (modalidad is { } m)
        {
            query = query.Where(g => g.Modalidad == m);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // El NRC se guarda en mayúsculas (Grupo.NormalizarNrc).
            var termino = request.Search.Trim().ToUpperInvariant();
            query = query.Where(g => g.Nrc.Contains(termino));
        }

        return query
            .OrderByDescending(g => g.Periodo.InicioClases)
            .ThenBy(g => g.Curso.Nombre)
            .ThenBy(g => g.Nrc)
            .ThenBy(g => g.Id)
            .Select(GrupoProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// RF-42: crea el grupo Planificado y sin horarios. Curso, profesor y aula deben estar activos (RN-17), el cupo no
    /// supera la capacidad del aula (RN-09) y el NRC es único (RN-13).
    /// </summary>
    public async Task<GrupoDetalleResponse> CrearAsync(CrearGrupoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // El validador ya exige estos datos.
        var cursoId = request.CursoId!.Value;
        var profesorId = request.ProfesorId!.Value;
        var periodoId = request.PeriodoId!.Value;
        var cupo = request.Cupo!.Value;

        await ValidarReferenciasAsync(cursoId, profesorId, periodoId, request.AulaId, cupo, actual: null, cancellationToken);

        var grupo = new Grupo(request.Nrc, cursoId, profesorId, periodoId, request.AulaId, cupo, request.Modalidad!.Value);
        await ValidarNrcDisponibleAsync(grupo.Nrc, grupoId: null, cancellationToken);

        db.Grupos.Add(grupo);
        await db.SaveChangesAsync(cancellationToken);

        return await ObtenerAsync(grupo.Id, cancellationToken);
    }

    /// <summary>
    /// RF-45: lo que se puede cambiar depende del estado (lo decide el dominio). RN-17 solo con las referencias nuevas,
    /// RN-09 siempre, RN-13 contra los otros grupos, y RN-07/RN-08 con todos los bloques del grupo y el profesor y aula
    /// que quedan.
    /// </summary>
    public async Task<GrupoDetalleResponse> ActualizarAsync(
        int id, ActualizarGrupoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await BuscarConHorariosAsync(id, cancellationToken);

        // El validador ya exige estos datos.
        var cursoId = request.CursoId!.Value;
        var profesorId = request.ProfesorId!.Value;
        var periodoId = request.PeriodoId!.Value;
        var cupo = request.Cupo!.Value;

        // Antes de modificar el grupo: RN-17 compara con las referencias que tenía.
        await ValidarReferenciasAsync(cursoId, profesorId, periodoId, request.AulaId, cupo, grupo, cancellationToken);

        grupo.ActualizarDatos(request.Nrc, cursoId, profesorId, periodoId, request.AulaId, cupo, request.Modalidad!.Value);
        await ValidarNrcDisponibleAsync(grupo.Nrc, grupo.Id, cancellationToken);
        await ValidarTraslapesAsync(grupo, grupo.Horarios, grupo.ProfesorId, grupo.AulaId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return await ObtenerAsync(id, cancellationToken);
    }

    /// <summary>
    /// RF-44: incluye los horarios, el cupo, los ocupados y los disponibles. RN-18: un profesor solo ve el detalle de
    /// sus grupos y un estudiante el de los Abierto y el de los grupos en que tiene o tuvo una matrícula; otro grupo
    /// existente responde 403.
    /// </summary>
    public async Task<GrupoDetalleResponse> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var detalle = await db.Grupos.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(GrupoProyecciones.ADetalle)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"No existe el grupo {id}.");

        var visible = currentUser.Rol switch
        {
            nameof(Rol.Profesor) => detalle.ProfesorId == ProfesorActual(),
            nameof(Rol.Estudiante) => detalle.Estado == EstadoGrupo.Abierto
                || await TieneMatriculaAsync(id, cancellationToken),
            _ => true,
        };

        if (!visible)
        {
            throw new ForbiddenException("No tiene acceso a este grupo.", "RN-18");
        }

        return detalle.OrdenarHorarios();
    }

    // Activa o retirada: el estudiante puede revisar el grupo de su historial.
    private Task<bool> TieneMatriculaAsync(int grupoId, CancellationToken cancellationToken)
    {
        var estudianteId = currentUser.EstudianteId
            ?? throw new ForbiddenException("Su sesión no identifica a un estudiante. Inicie sesión de nuevo.");

        return db.Matriculas.AnyAsync(m => m.GrupoId == grupoId && m.EstudianteId == estudianteId, cancellationToken);
    }

    // Un token emitido antes de que existiera el claim no lo trae: basta con iniciar sesión de nuevo.
    private int ProfesorActual() =>
        currentUser.ProfesorId
        ?? throw new ForbiddenException("Su sesión no identifica a un profesor. Inicie sesión de nuevo.");

    /// <summary>
    /// RF-48: agrega un bloque. Rechaza el traslape con otro bloque del grupo (RN-14), con los grupos del mismo
    /// profesor (RN-07) o la misma aula (RN-08) en el periodo, y que deje con choque a un estudiante matriculado (RN-06,
    /// supuesto 13).
    /// </summary>
    public async Task<GrupoDetalleResponse> AgregarHorarioAsync(
        int grupoId, AgregarHorarioRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var grupo = await BuscarConHorariosAsync(grupoId, cancellationToken);
        var horario = grupo.AgregarHorario(request.Dia!.Value, request.HoraInicio!.Value, request.HoraFin!.Value);
        await ValidarTraslapesAsync(grupo, [horario], grupo.ProfesorId, grupo.AulaId, cancellationToken);
        await db.ValidarBloquesSinChoquesAsync(grupo, [horario], cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return await ObtenerAsync(grupoId, cancellationToken);
    }

    /// <summary>
    /// RF-48: solo mientras el grupo está Planificado. Un bloque de otro grupo responde 404.
    /// </summary>
    public async Task QuitarHorarioAsync(int grupoId, int horarioId, CancellationToken cancellationToken)
    {
        var grupo = await BuscarConHorariosAsync(grupoId, cancellationToken);
        var horario = grupo.QuitarHorario(horarioId)
            ?? throw new NotFoundException($"El grupo {grupoId} no tiene el bloque de horario {horarioId}.");

        // Con FK Restrict, EF no elimina el bloque solo por sacarlo de la colección.
        db.Horarios.Remove(horario);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// RF-47: avanza un paso del flujo normal o cancela. Pedir el estado actual no cambia nada. Al cancelar se
    /// retiran las matrículas activas y se liberan sus cupos (RN-12).
    /// </summary>
    public async Task CambiarEstadoAsync(int grupoId, CambiarEstadoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Los horarios se cargan porque abrir un grupo exige al menos un bloque.
        var grupo = await BuscarConHorariosAsync(grupoId, cancellationToken);
        var estadoAnterior = grupo.Estado;
        grupo.CambiarEstado(request.Estado!.Value);

        if (grupo.Estado == EstadoGrupo.Cancelado && estadoAnterior != EstadoGrupo.Cancelado)
        {
            await RetirarPorCancelacionAsync(grupo, cancellationToken);
        }

        // Un solo guardado: estado, retiros y cupos. Si alguien se matricula a la vez, el token de concurrencia del
        // grupo hace fallar a uno de los dos (409 CONCURRENCIA).
        await db.SaveChangesAsync(cancellationToken);
    }

    // RN-12: la cancelación retira todas las matrículas activas, sin revisar la fecha límite de retiro (supuesto 10).
    private async Task RetirarPorCancelacionAsync(Grupo grupo, CancellationToken cancellationToken)
    {
        var activas = await db.Matriculas
            .Where(m => m.GrupoId == grupo.Id && m.Estado == EstadoMatricula.Activa)
            .ToListAsync(cancellationToken);

        var hoy = fechaLocal.Hoy();
        foreach (var matricula in activas)
        {
            matricula.Retirar(hoy, MotivoRetiro.CancelacionGrupo);
            grupo.LiberarCupo();
        }
    }

    private async Task<Grupo> BuscarConHorariosAsync(int id, CancellationToken cancellationToken) =>
        await db.Grupos
            .Include(g => g.Horarios)
            .SingleOrDefaultAsync(g => g.Id == id, cancellationToken)
        ?? throw new NotFoundException($"No existe el grupo {id}.");

    // RN-07 y RN-08: compara los bloques con los de los otros grupos del mismo periodo que comparten el profesor o el
    // aula. Los cancelados no cuentan (supuesto 3). El traslape se decide con ReglasHorario, la misma regla de RN-14.
    private async Task ValidarTraslapesAsync(
        Grupo grupo,
        IReadOnlyCollection<Horario> bloques,
        int profesorId,
        int? aulaId,
        CancellationToken cancellationToken)
    {
        if (bloques.Count == 0)
        {
            return;
        }

        var dias = bloques.Select(b => b.Dia).Distinct().ToList();
        var otros = await db.Grupos.AsNoTracking()
            .Where(g => g.PeriodoId == grupo.PeriodoId
                && g.Id != grupo.Id
                && g.Estado != EstadoGrupo.Cancelado
                && (g.ProfesorId == profesorId || (aulaId != null && g.AulaId == aulaId)))
            .SelectMany(g => g.Horarios
                .Where(h => dias.Contains(h.Dia))
                .Select(h => new { g.Nrc, g.ProfesorId, g.AulaId, h.Dia, h.HoraInicio, h.HoraFin }))
            .ToListAsync(cancellationToken);

        var choque = otros.FirstOrDefault(o => bloques.Any(b =>
            ReglasHorario.SeTraslapan(b.Dia, b.HoraInicio, b.HoraFin, o.Dia, o.HoraInicio, o.HoraFin)));

        if (choque is null)
        {
            return;
        }

        var cuando = $"el {choque.Dia} de {choque.HoraInicio:HH\\:mm} a {choque.HoraFin:HH\\:mm}";
        if (choque.ProfesorId == profesorId)
        {
            throw new BusinessRuleException(
                "RN-07", $"El profesor ya imparte el grupo {choque.Nrc} {cuando} en el mismo periodo.");
        }

        throw new BusinessRuleException(
            "RN-08", $"El aula ya está ocupada por el grupo {choque.Nrc} {cuando} en el mismo periodo.");
    }

    // RN-17 solo aplica a lo que el grupo empieza a usar: un curso, profesor o aula que ya tenía no se revisa (RN-37
    // impide desactivarlos mientras el grupo esté activo). RN-09 siempre se revisa.
    private async Task ValidarReferenciasAsync(
        int cursoId,
        int profesorId,
        int periodoId,
        int? aulaId,
        int cupo,
        Grupo? actual,
        CancellationToken cancellationToken)
    {
        var cursoActivo = await db.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => (bool?)c.Activo)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"No existe el curso {cursoId}.");

        var profesorActivo = await db.Profesores.AsNoTracking()
            .Where(p => p.Id == profesorId)
            .Select(p => (bool?)p.Usuario.Activo)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"No existe el profesor {profesorId}.");

        if (!await db.Periodos.AnyAsync(p => p.Id == periodoId, cancellationToken))
        {
            throw new NotFoundException($"No existe el periodo {periodoId}.");
        }

        var aula = aulaId is null
            ? null
            : await db.Aulas.AsNoTracking()
                .Where(a => a.Id == aulaId)
                .Select(a => new { a.Codigo, a.Activo, a.Capacidad })
                .SingleOrDefaultAsync(cancellationToken)
              ?? throw new NotFoundException($"No existe el aula {aulaId}.");

        if (!cursoActivo && actual?.CursoId != cursoId)
        {
            throw new BusinessRuleException("RN-17", "El curso está inactivo y no se puede usar en grupos nuevos.");
        }

        if (!profesorActivo && actual?.ProfesorId != profesorId)
        {
            throw new BusinessRuleException("RN-17", "El profesor está inactivo y no se puede asignar a grupos.");
        }

        if (aula is not null && !aula.Activo && actual?.AulaId != aulaId)
        {
            throw new BusinessRuleException("RN-17", $"El aula {aula.Codigo} está inactiva y no se puede asignar a grupos.");
        }

        if (aula is not null && cupo > aula.Capacidad)
        {
            throw new BusinessRuleException(
                "RN-09",
                $"El cupo ({cupo}) supera la capacidad del aula {aula.Codigo} ({aula.Capacidad}).");
        }
    }

    // RN-13. En la edición se excluye el propio grupo.
    private async Task ValidarNrcDisponibleAsync(string nrc, int? grupoId, CancellationToken cancellationToken)
    {
        if (await db.Grupos.AnyAsync(g => g.Nrc == nrc && g.Id != grupoId, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un grupo con el NRC {nrc}.");
        }
    }
}
