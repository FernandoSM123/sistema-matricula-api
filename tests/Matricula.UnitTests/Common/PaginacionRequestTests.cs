using Matricula.Application.Common.Paginacion;

namespace Matricula.UnitTests.Common;

public class PaginacionRequestTests
{
    [Fact]
    public void ValoresPorDefecto_Pagina1Y20Elementos()
    {
        var request = new PaginacionRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Page_MenorQueUno_SeLimitaA1(int page)
    {
        var request = new PaginacionRequest { Page = page };

        Assert.Equal(1, request.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void PageSize_MenorQueUno_SeLimitaA1(int pageSize)
    {
        var request = new PaginacionRequest { PageSize = pageSize };

        Assert.Equal(1, request.PageSize);
    }

    [Fact]
    public void PageSize_MayorAlMaximo_SeLimitaA100()
    {
        var request = new PaginacionRequest { PageSize = 500 };

        Assert.Equal(PaginacionRequest.PageSizeMaximo, request.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void PageSize_DentroDelRango_SeConserva(int pageSize)
    {
        var request = new PaginacionRequest { PageSize = pageSize };

        Assert.Equal(pageSize, request.PageSize);
    }
}
