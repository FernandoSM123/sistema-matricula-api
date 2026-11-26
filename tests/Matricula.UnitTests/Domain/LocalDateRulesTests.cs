using Matricula.Domain.Rules;

namespace Matricula.UnitTests.Domain;

public class LocalDateRulesTests
{
    private static readonly TimeZoneInfo CostaRica = TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");

    [Fact(DisplayName = "Antes de la medianoche local la fecha es el día anterior al UTC")]
    public void ToLocalDate_BeforeLocalMidnight_ReturnsDayBeforeUtc()
    {
        var instante = new DateTimeOffset(2027, 3, 10, 5, 59, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2027, 3, 9), LocalDateRules.ToLocalDate(instante, CostaRica));
    }

    [Fact(DisplayName = "En la medianoche local la fecha es el mismo día que en UTC")]
    public void ToLocalDate_AtLocalMidnight_ReturnsSameDayAsUtc()
    {
        var instante = new DateTimeOffset(2027, 3, 10, 6, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2027, 3, 10), LocalDateRules.ToLocalDate(instante, CostaRica));
    }

    [Fact(DisplayName = "Un instante con otro desplazamiento se convierte a la zona del instituto")]
    public void ToLocalDate_InstantWithOtherOffset_ConvertsToZone()
    {
        var instante = new DateTimeOffset(2027, 3, 10, 1, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal(new DateOnly(2027, 3, 9), LocalDateRules.ToLocalDate(instante, CostaRica));
    }

    [Fact(DisplayName = "Un solo día va de las 06:00 UTC a las 06:00 UTC del día siguiente")]
    public void ToUtcRange_SingleDay_GoesFromSixUtcToSixUtcNextDay()
    {
        var dia = new DateOnly(2027, 3, 10);

        var (inicio, fin) = LocalDateRules.ToUtcRange(dia, dia, CostaRica);

        Assert.Equal(new DateTimeOffset(2027, 3, 10, 6, 0, 0, TimeSpan.Zero), inicio);
        Assert.Equal(new DateTimeOffset(2027, 3, 11, 6, 0, 0, TimeSpan.Zero), fin);
        Assert.Equal(TimeSpan.Zero, inicio!.Value.Offset);
    }

    [Theory(DisplayName = "Un instante cerca de la medianoche local queda dentro según la fecha local")]
    [InlineData(2027, 3, 10, 6, 0, true)]   // 00:00 locales del 10: entra
    [InlineData(2027, 3, 11, 5, 30, true)]  // 23:30 locales del 10: entra aunque en UTC ya es el 11
    [InlineData(2027, 3, 11, 6, 0, false)]  // 00:00 locales del 11: el fin es exclusivo
    [InlineData(2027, 3, 10, 5, 59, false)] // 23:59 locales del 9
    public void ToUtcRange_InstantNearLocalMidnight_IsInsideByLocalDate(
        int anio, int mes, int dia, int hora, int minuto, bool dentro)
    {
        var fecha = new DateOnly(2027, 3, 10);
        var instante = new DateTimeOffset(anio, mes, dia, hora, minuto, 0, TimeSpan.Zero);

        var (inicio, fin) = LocalDateRules.ToUtcRange(fecha, fecha, CostaRica);

        Assert.Equal(dentro, instante >= inicio && instante < fin);
    }

    [Fact(DisplayName = "Con solo la fecha desde, el fin queda abierto")]
    public void ToUtcRange_OnlyFrom_LeavesEndOpen()
    {
        var (inicio, fin) = LocalDateRules.ToUtcRange(new DateOnly(2027, 3, 10), null, CostaRica);

        Assert.Equal(new DateTimeOffset(2027, 3, 10, 6, 0, 0, TimeSpan.Zero), inicio);
        Assert.Null(fin);
    }

    [Fact(DisplayName = "Con solo la fecha hasta, el inicio queda abierto")]
    public void ToUtcRange_OnlyTo_LeavesStartOpen()
    {
        var (inicio, fin) = LocalDateRules.ToUtcRange(null, new DateOnly(2027, 3, 10), CostaRica);

        Assert.Null(inicio);
        Assert.Equal(new DateTimeOffset(2027, 3, 11, 6, 0, 0, TimeSpan.Zero), fin);
    }

    [Fact(DisplayName = "Convertir un rango con una zona nula lanza ArgumentNullException")]
    public void ToUtcRange_NullZone_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LocalDateRules.ToUtcRange(null, null, null!));
    }

    [Fact(DisplayName = "Convertir una fecha con una zona nula lanza ArgumentNullException")]
    public void ToLocalDate_NullZone_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LocalDateRules.ToLocalDate(DateTimeOffset.UnixEpoch, null!));
    }
}
