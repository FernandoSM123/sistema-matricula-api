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
    public void Calcular_ZonaNula_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FechaInstituto.Calcular(DateTimeOffset.UnixEpoch, null!));
    }
}
