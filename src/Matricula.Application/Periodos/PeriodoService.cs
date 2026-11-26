using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Pagination;
using Matricula.Domain.Entities;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Periodos;

public class PeriodoService(IAppDbContext db, ILocalClock localClock)
{
    /// <summary>
    /// RF-35: registra un periodo. Las fechas ya las exige el validador, y los periodos pueden traslaparse.
    /// </summary>
    public async Task<PeriodoResponse> CreateAsync(CreatePeriodoRequest request, CancellationToken cancellationToken)
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
    public Task<PagedResult<PeriodoResponse>> ListAsync(ListPeriodosRequest request, CancellationToken cancellationToken)
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
            .Select(PeriodoProjections.ToResponseExpression)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PeriodoResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        await db.Periodos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PeriodoProjections.ToResponseExpression)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el periodo {id}.");

    /// <summary>
    /// RF-38: vigente es el periodo con InicioMatricula ≤ hoy ≤ FinClases. Los periodos pueden traslaparse: gana el de
    /// matrícula más reciente y, si coinciden, el de mayor id.
    /// </summary>
    public async Task<PeriodoResponse> GetVigenteAsync(CancellationToken cancellationToken)
    {
        var id = await GetVigenteIdAsync(cancellationToken)
            ?? throw new NotFoundException("No hay un periodo vigente.");

        return await db.Periodos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(PeriodoProjections.ToResponseExpression)
            .SingleAsync(cancellationToken);
    }

    /// <summary>
    /// Id del periodo vigente, o null si no hay. Para las consultas de otros módulos que lo usan por defecto (la oferta
    /// académica, el horario semanal y la ocupación de un aula).
    /// </summary>
    public async Task<int?> GetVigenteIdAsync(CancellationToken cancellationToken)
    {
        var hoy = localClock.Today();

        return await db.Periodos.AsNoTracking()
            .Where(p => p.InicioMatricula <= hoy && hoy <= p.FinClases)
            .OrderByDescending(p => p.InicioMatricula)
            .ThenByDescending(p => p.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// RF-36: también se editan periodos pasados. Las fechas ya las exige el validador.
    /// </summary>
    public async Task<PeriodoResponse> UpdateAsync(int id, UpdatePeriodoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var periodo = await db.Periodos.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No existe el periodo {id}.");

        var nombre = Periodo.NormalizeNombre(request.Nombre);
        if (nombre != periodo.Nombre
            && await db.Periodos.AnyAsync(p => p.Nombre == nombre && p.Id != id, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un periodo con el nombre {nombre}.");
        }

        periodo.Update(
            request.Nombre,
            request.InicioClases!.Value,
            request.FinClases!.Value,
            request.InicioMatricula!.Value,
            request.FinMatricula!.Value,
            request.FechaLimiteRetiro!.Value);
        await db.SaveChangesAsync(cancellationToken);

        return periodo.ToResponse();
    }

    /// <summary>
    /// RF-37 y RN-34: solo un periodo sin grupos, ni siquiera cancelados. Los periodos no se desactivan: uno con grupos
    /// se corrige o se conserva como historial.
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var periodo = await db.Periodos.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No existe el periodo {id}.");

        if (await db.Grupos.AnyAsync(g => g.PeriodoId == id, cancellationToken))
        {
            throw new BusinessRuleException(
                "RN-34",
                $"El periodo {periodo.Nombre} tiene grupos y no se puede eliminar. Corrija sus datos o consérvelo como historial.");
        }

        db.Periodos.Remove(periodo);
        await db.SaveChangesAsync(cancellationToken);
    }
}
