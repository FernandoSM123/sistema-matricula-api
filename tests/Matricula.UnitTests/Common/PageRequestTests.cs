using Matricula.Application.Common.Pagination;

namespace Matricula.UnitTests.Common;

public class PageRequestTests
{
    [Fact]
    public void ValoresPorDefecto_Pagina1Y20Elementos()
    {
        var request = new PageRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Page_MenorQueUno_SeLimitaA1(int page)
    {
        var request = new PageRequest { Page = page };

        Assert.Equal(1, request.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void PageSize_MenorQueUno_SeLimitaA1(int pageSize)
    {
        var request = new PageRequest { PageSize = pageSize };

        Assert.Equal(1, request.PageSize);
    }

    [Fact]
    public void PageSize_MayorAlMaximo_SeLimitaA100()
    {
        var request = new PageRequest { PageSize = 500 };

        Assert.Equal(PageRequest.MaxPageSize, request.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void PageSize_DentroDelRango_SeConserva(int pageSize)
    {
        var request = new PageRequest { PageSize = pageSize };

        Assert.Equal(pageSize, request.PageSize);
    }
}
