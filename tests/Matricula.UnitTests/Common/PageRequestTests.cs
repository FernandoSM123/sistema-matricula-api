using Matricula.Application.Common.Pagination;

namespace Matricula.UnitTests.Common;

public class PageRequestTests
{
    [Fact(DisplayName = "Sin parámetros pide la página 1 con 20 elementos")]
    public void Defaults_Page1And20Items()
    {
        var request = new PageRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
    }

    [Theory(DisplayName = "Una página menor que 1 se ajusta a 1")]
    [InlineData(0)]
    [InlineData(-5)]
    public void Page_LessThanOne_ClampsTo1(int page)
    {
        var request = new PageRequest { Page = page };

        Assert.Equal(1, request.Page);
    }

    [Theory(DisplayName = "Un tamaño de página menor que 1 se ajusta a 1")]
    [InlineData(0)]
    [InlineData(-10)]
    public void PageSize_LessThanOne_ClampsTo1(int pageSize)
    {
        var request = new PageRequest { PageSize = pageSize };

        Assert.Equal(1, request.PageSize);
    }

    [Fact(DisplayName = "Un tamaño de página mayor que el máximo se ajusta a 100")]
    public void PageSize_AboveMaximum_ClampsTo100()
    {
        var request = new PageRequest { PageSize = 500 };

        Assert.Equal(PageRequest.MaxPageSize, request.PageSize);
    }

    [Theory(DisplayName = "Un tamaño de página dentro del rango se conserva")]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void PageSize_WithinRange_IsKept(int pageSize)
    {
        var request = new PageRequest { PageSize = pageSize };

        Assert.Equal(pageSize, request.PageSize);
    }
}
