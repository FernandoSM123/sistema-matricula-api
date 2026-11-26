namespace Matricula.Application.Common.Pagination;

/// <summary>
/// Parámetros comunes de paginación y búsqueda. Los listados heredan de esta clase y agregan sus propios filtros.
/// </summary>
public class PageRequest
{
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public string? Search { get; set; }
}
