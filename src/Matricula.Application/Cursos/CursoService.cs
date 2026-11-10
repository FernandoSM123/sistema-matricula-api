using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Grupos;
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
    /// RF-30: listado paginado, solo activos salvo que se pidan los inactivos (RN-41).
    /// </summary>
    public Task<PagedResult<CursoResponse>> ListarAsync(ListarCursosRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Cursos.AsNoTracking();

        if (!request.IncluirInactivos)
        {
            query = query.Where(c => c.Activo);
        }

        if (request.Nivel is { } nivel)
        {
            query = query.Where(c => c.Nivel == nivel);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(c => c.Nombre.ToLower().Contains(termino) || c.Codigo.ToLower().Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Id)
            .Select(CursoProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// RF-32: también se editan cursos inactivos. Costo y Nivel ya los exige el validador.
    /// </summary>
    public async Task<CursoResponse> ActualizarAsync(int id, ActualizarCursoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var curso = await BuscarAsync(id, cancellationToken);

        // RN-13: aquí un código de un inactivo tampoco se puede usar, y reactivarlo no resolvería el conflicto.
        var codigo = Curso.NormalizarCodigo(request.Codigo);
        if (codigo != curso.Codigo
            && await db.Cursos.AnyAsync(c => c.Codigo == codigo && c.Id != id, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un curso con el código {codigo}.");
        }

        curso.ActualizarDatos(
            request.Codigo,
            request.Nombre,
            request.Descripcion,
            request.DuracionHoras,
            request.Costo!.Value,
            request.Nivel!.Value);
        await db.SaveChangesAsync(cancellationToken);

        return curso.ToResponse();
    }

    /// <summary>
    /// RF-33: repetir la operación no cambia nada. El módulo grupos añade RN-37.
    /// </summary>
    public async Task DesactivarAsync(int id, CancellationToken cancellationToken)
    {
        var curso = await BuscarAsync(id, cancellationToken);
        if (curso.Activo)
        {
            await db.ValidarSinGruposActivosAsync(g => g.CursoId == id, $"el curso {curso.Codigo}", cancellationToken);
        }

        curso.Desactivar();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// RF-33 y RN-39: un curso que vuelve a ofrecerse se reactiva, no se registra de nuevo.
    /// </summary>
    public async Task ReactivarAsync(int id, CancellationToken cancellationToken)
    {
        var curso = await BuscarAsync(id, cancellationToken);
        curso.Reactivar();
        await db.SaveChangesAsync(cancellationToken);
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

    /// <summary>
    /// RF-33 y RN-34: solo un curso que nunca tuvo grupos, ni siquiera cancelados. Si no, se desactiva.
    /// </summary>
    public async Task EliminarAsync(int id, CancellationToken cancellationToken)
    {
        var curso = await BuscarAsync(id, cancellationToken);

        if (await db.Grupos.AnyAsync(g => g.CursoId == id, cancellationToken))
        {
            throw new BusinessRuleException(
                "RN-34", $"El curso {curso.Codigo} tiene grupos y no se puede eliminar. Desactívelo en su lugar.");
        }

        db.Cursos.Remove(curso);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Curso> BuscarAsync(int id, CancellationToken cancellationToken) =>
        await db.Cursos.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
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
