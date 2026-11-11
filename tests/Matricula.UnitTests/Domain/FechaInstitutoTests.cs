using Matricula.Domain.Reglas;

namespace Matricula.UnitTests.Domain;

public class FechaInstitutoTests
{
    private static readonly TimeZoneInfo CostaRica = TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");

    [Fact]
    public void Calcular_AntesDeMedianocheLocal_DevuelveElDiaAnteriorAlUtc()
    {
        var instante = new DateTimeOffset(2027, 3, 10, 5, 59, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2027, 3, 9), FechaInstituto.Calcular(instante, CostaRica));
    }

    [Fact]
    public void Calcular_EnLaMedianocheLocal_DevuelveElMismoDiaQueUtc()
    {
        var instante = new DateTimeOffset(2027, 3, 10, 6, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2027, 3, 10), FechaInstituto.Calcular(instante, CostaRica));
    }

    [Fact]
    public void Calcular_InstanteConOtroDesplazamiento_ConvierteALaZona()
    {
        var instante = new DateTimeOffset(2027, 3, 10, 1, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal(new DateOnly(2027, 3, 9), FechaInstituto.Calcular(instante, CostaRica));
    }

    [Fact]
    public void RangoUtc_UnSoloDia_VaDeLasSeisUtcALasSeisUtcDelDiaSiguiente()
    {
        var dia = new DateOnly(2027, 3, 10);

        var (inicio, fin) = FechaInstituto.RangoUtc(dia, dia, CostaRica);

        Assert.Equal(new DateTimeOffset(2027, 3, 10, 6, 0, 0, TimeSpan.Zero), inicio);
        Assert.Equal(new DateTimeOffset(2027, 3, 11, 6, 0, 0, TimeSpan.Zero), fin);
        Assert.Equal(TimeSpan.Zero, inicio!.Value.Offset);
    }

    [Theory]
    [InlineData(2027, 3, 10, 6, 0, true)]   // 00:00 locales del 10: entra
    [InlineData(2027, 3, 11, 5, 30, true)]  // 23:30 locales del 10: entra aunque en UTC ya es el 11
    [InlineData(2027, 3, 11, 6, 0, false)]  // 00:00 locales del 11: el fin es exclusivo
    [InlineData(2027, 3, 10, 5, 59, false)] // 23:59 locales del 9
    public void RangoUtc_InstanteCercaDeLaMedianocheLocal_QuedaDentroSegunLaFechaLocal(
        int anio, int mes, int dia, int hora, int minuto, bool dentro)
    {
        var fecha = new DateOnly(2027, 3, 10);
        var instante = new DateTimeOffset(anio, mes, dia, hora, minuto, 0, TimeSpan.Zero);

        var (inicio, fin) = FechaInstituto.RangoUtc(fecha, fecha, CostaRica);

        Assert.Equal(dentro, instante >= inicio && instante < fin);
    }

    [Fact]
    public void RangoUtc_SoloDesde_DejaElFinAbierto()
    {
        var (inicio, fin) = FechaInstituto.RangoUtc(new DateOnly(2027, 3, 10), null, CostaRica);

        Assert.Equal(new DateTimeOffset(2027, 3, 10, 6, 0, 0, TimeSpan.Zero), inicio);
        Assert.Null(fin);
    }

    [Fact]
    public void RangoUtc_SoloHasta_DejaElInicioAbierto()
    {
        var (inicio, fin) = FechaInstituto.RangoUtc(null, new DateOnly(2027, 3, 10), CostaRica);

        Assert.Null(inicio);
        Assert.Equal(new DateTimeOffset(2027, 3, 11, 6, 0, 0, TimeSpan.Zero), fin);
    }

    [Fact]
    public void RangoUtc_ZonaNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FechaInstituto.RangoUtc(null, null, null!));
    }

    [Fact]
    public void Calcular_ZonaNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FechaInstituto.Calcular(DateTimeOffset.UnixEpoch, null!));
    }
}
