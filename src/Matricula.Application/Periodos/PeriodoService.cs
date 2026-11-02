using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Entities;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Periodos;

public class PeriodoService(IAppDbContext db)
{
    /// <summary>
    /// RF-35: registra un periodo. Las fechas ya las exige el validador, y los periodos pueden traslaparse.
    /// </summary>
    public async Task<PeriodoResponse> RegistrarAsync(RegistrarPeriodoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var periodo = new Periodo(
            request.Nombre,
            request.InicioClases!.Value,
            request.FinClases!.Value,
            request.InicioMatricula!.Value,
            request.FinMatricula!.Value,
            request.FechaLimiteRetiro!.Value);

        // RN-13. Los periodos no se desactivan, así que aquí no aplica RN-39.
        if (await db.Periodos.AnyAsync(p => p.Nombre == periodo.Nombre, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un periodo con el nombre {periodo.Nombre}.");
        }

        db.Periodos.Add(periodo);
        await db.SaveChangesAsync(cancellationToken);
        return periodo.ToResponse();
    }

    /// <summary>
    /// RF-36: listado paginado, con los más recientes primero.
    /// </summary>
    public Task<PagedResult<PeriodoResponse>> ListarAsync(ListarPeriodosRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Periodos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query
            .OrderByDescending(p => p.InicioClases)
            .ThenBy(p => p.Id)
            .Select(PeriodoProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PeriodoResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await db.Periodos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PeriodoProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el periodo {id}.");

    /// <summary>
    /// RF-36: también se editan periodos pasados. Las fechas ya las exige el validador.
    /// </summary>
    public async Task<PeriodoResponse> ActualizarAsync(int id, ActualizarPeriodoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var periodo = await db.Periodos.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No existe el periodo {id}.");

        var nombre = Periodo.NormalizarNombre(request.Nombre);
        if (nombre != periodo.Nombre
            && await db.Periodos.AnyAsync(p => p.Nombre == nombre && p.Id != id, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un periodo con el nombre {nombre}.");
        }

        periodo.ActualizarDatos(
            request.Nombre,
            request.InicioClases!.Value,
            request.FinClases!.Value,
            request.InicioMatricula!.Value,
            request.FinMatricula!.Value,
            request.FechaLimiteRetiro!.Value);
        await db.SaveChangesAsync(cancellationToken);

        return periodo.ToResponse();
    }
}
