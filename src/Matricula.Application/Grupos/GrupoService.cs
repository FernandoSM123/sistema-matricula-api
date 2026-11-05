using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Domain.Entities;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Grupos;

public class GrupoService(IAppDbContext db)
{
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
    /// RF-44: incluye los horarios, el cupo, los ocupados y los disponibles.
    /// </summary>
    public async Task<GrupoDetalleResponse> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var detalle = await db.Grupos.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(GrupoProyecciones.ADetalle)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"No existe el grupo {id}.");

        return detalle.OrdenarHorarios();
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
