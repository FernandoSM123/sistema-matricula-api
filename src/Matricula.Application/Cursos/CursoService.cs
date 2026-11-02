using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Domain.Entities;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Cursos;

public class CursoService(IAppDbContext db)
{
    /// <summary>
    /// RF-29: registra un curso activo. Costo y Nivel ya los exige el validador.
    /// </summary>
    public async Task<CursoResponse> RegistrarAsync(RegistrarCursoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var curso = new Curso(
            request.Codigo,
            request.Nombre,
            request.Descripcion,
            request.DuracionHoras,
            request.Costo!.Value,
            request.Nivel!.Value);
        await ValidarCodigoDisponibleAsync(curso.Codigo, cancellationToken);

        db.Cursos.Add(curso);
        await db.SaveChangesAsync(cancellationToken);
        return curso.ToResponse();
    }

    /// <summary>
    /// RF-31: un curso inactivo también se consulta, porque el historial lo sigue mostrando.
    /// </summary>
    public async Task<CursoResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await db.Cursos.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(CursoProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el curso {id}.");

    // RN-13 (código único) y RN-39 (un inactivo se reactiva, no se registra de nuevo).
    private async Task ValidarCodigoDisponibleAsync(string codigo, CancellationToken cancellationToken)
    {
        var existente = await db.Cursos.AsNoTracking()
            .Where(c => c.Codigo == codigo)
            .Select(c => new { c.Id, c.Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (existente is null)
        {
            return;
        }

        if (existente.Activo)
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un curso con el código {codigo}.");
        }

        throw new BusinessRuleException(
            "RN-39",
            $"El código {codigo} pertenece a un curso inactivo. Reactívelo en lugar de registrarlo de nuevo.",
            new Dictionary<string, object?> { ["cursoId"] = existente.Id });
    }
}
