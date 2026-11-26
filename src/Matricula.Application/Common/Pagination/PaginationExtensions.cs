using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Common.Pagination;

public static class PaginationExtensions
{
    /// <summary>
    /// Pagina una consulta ya ordenada y proyectada. El orden debe aplicarse antes para que Skip/Take sea estable.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, request.Page, request.PageSize, total);
    }
}
