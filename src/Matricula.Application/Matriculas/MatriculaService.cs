using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
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

    // Orden de RF-51: RN-01, RN-02/RN-11, RN-04, RN-05, RN-06 y al final RN-03, para que un grupo lleno no oculte un
    // error más útil.
    private async Task<MatriculaResponse> MatricularAsync(
        int estudianteId, int grupoId, CancellationToken cancellationToken)
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
    private int EstudianteActual() =>
        currentUser.EstudianteId
        ?? throw new ForbiddenException("Su sesión no identifica a un estudiante. Inicie sesión de nuevo.");
}
