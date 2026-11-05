using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Grupos;
using Matricula.Domain.Entities;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Aulas;

public class AulaService(IAppDbContext db)
{
    /// <summary>
    /// RF-39: registra un aula activa.
    /// </summary>
    public async Task<AulaResponse> RegistrarAsync(RegistrarAulaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aula = new Aula(request.Codigo, request.Ubicacion, request.Capacidad);
        await ValidarCodigoDisponibleAsync(aula.Codigo, cancellationToken);

        db.Aulas.Add(aula);
        await db.SaveChangesAsync(cancellationToken);
        return aula.ToResponse();
    }

    /// <summary>
    /// RF-40: listado paginado, solo activas salvo que se pidan las inactivas (RN-41).
    /// </summary>
    public Task<PagedResult<AulaResponse>> ListarAsync(ListarAulasRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Aulas.AsNoTracking();

        if (!request.IncluirInactivos)
        {
            query = query.Where(a => a.Activo);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(a =>
                a.Codigo.ToLower().Contains(termino)
                || (a.Ubicacion != null && a.Ubicacion.ToLower().Contains(termino)));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query
            .OrderBy(a => a.Codigo)
            .ThenBy(a => a.Id)
            .Select(AulaProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// RF-40: también se editan aulas inactivas. El módulo grupos impide reducir la capacidad por debajo del cupo de
    /// sus grupos (RN-09).
    /// </summary>
    public async Task<AulaResponse> ActualizarAsync(int id, ActualizarAulaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aula = await BuscarAsync(id, cancellationToken);

        // RN-13: aquí un código de una inactiva tampoco se puede usar, y reactivarla no resolvería el conflicto.
        var codigo = Aula.NormalizarCodigo(request.Codigo);
        if (codigo != aula.Codigo
            && await db.Aulas.AnyAsync(a => a.Codigo == codigo && a.Id != id, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un aula con el código {codigo}.");
        }

        // RN-09: la capacidad no baja del cupo de un grupo que todavía usa el aula.
        if (request.Capacidad < aula.Capacidad)
        {
            var mayor = await db.Grupos.AsNoTracking()
                .Where(GuardasGrupos.Activo)
                .Where(g => g.AulaId == id && g.Cupo > request.Capacidad)
                .OrderByDescending(g => g.Cupo)
                .Select(g => new { g.Nrc, g.Cupo })
                .FirstOrDefaultAsync(cancellationToken);

            if (mayor is not null)
            {
                throw new BusinessRuleException(
                    "RN-09",
                    $"La capacidad ({request.Capacidad}) quedaría por debajo del cupo del grupo {mayor.Nrc} ({mayor.Cupo}).");
            }
        }

        aula.ActualizarDatos(request.Codigo, request.Ubicacion, request.Capacidad);
        await db.SaveChangesAsync(cancellationToken);

        return aula.ToResponse();
    }

    /// <summary>
    /// RF-40: un aula inactiva también se consulta, porque el historial la sigue mostrando.
    /// </summary>
    public async Task<AulaResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await db.Aulas.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(AulaProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el aula {id}.");

    /// <summary>
    /// RF-40: repetir la operación no cambia nada. El módulo grupos añade RN-37.
    /// </summary>
    public async Task DesactivarAsync(int id, CancellationToken cancellationToken)
    {
        var aula = await BuscarAsync(id, cancellationToken);
        if (aula.Activo)
        {
            await db.ValidarSinGruposActivosAsync(g => g.AulaId == id, $"el aula {aula.Codigo}", cancellationToken);
        }

        aula.Desactivar();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// RF-40 y RN-39: un aula que vuelve a usarse se reactiva, no se registra de nuevo.
    /// </summary>
    public async Task ReactivarAsync(int id, CancellationToken cancellationToken)
    {
        var aula = await BuscarAsync(id, cancellationToken);
        aula.Reactivar();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Aula> BuscarAsync(int id, CancellationToken cancellationToken) =>
        await db.Aulas.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
        ?? throw new NotFoundException($"No existe el aula {id}.");

    // RN-13 (código único) y RN-39 (una inactiva se reactiva, no se registra de nuevo).
    private async Task ValidarCodigoDisponibleAsync(string codigo, CancellationToken cancellationToken)
    {
        var existente = await db.Aulas.AsNoTracking()
            .Where(a => a.Codigo == codigo)
            .Select(a => new { a.Id, a.Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (existente is null)
        {
            return;
        }

        if (existente.Activo)
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un aula con el código {codigo}.");
        }

        throw new BusinessRuleException(
            "RN-39",
            $"El código {codigo} pertenece a un aula inactiva. Reactívela en lugar de registrarla de nuevo.",
            new Dictionary<string, object?> { ["aulaId"] = existente.Id });
    }
}
