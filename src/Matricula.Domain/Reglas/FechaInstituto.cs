namespace Matricula.Domain.Reglas;

/// <summary>
/// Calcula la fecha del calendario en una zona horaria. Las fechas sin hora del negocio (periodo vigente, RN-04,
/// RN-10) se comparan contra esta fecha, no contra la UTC: en Costa Rica, desde las 18:00 UTC ya va un día adelante.
/// </summary>
public static class FechaInstituto
{
    public static DateOnly Calcular(DateTimeOffset instante, TimeZoneInfo zona)
    {
        ArgumentNullException.ThrowIfNull(zona);

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instante, zona).DateTime);
    }

    /// <summary>
    /// Convierte un rango de fechas locales, con los dos extremos incluidos, al rango UTC equivalente: desde las 00:00
    /// locales de <paramref name="desde"/> hasta las 00:00 locales del día siguiente a <paramref name="hasta"/>, este
    /// último excluido. Un extremo nulo queda abierto (supuesto 8 de la spec de auditoria).
    /// </summary>
    public static (DateTimeOffset? Inicio, DateTimeOffset? Fin) RangoUtc(
        DateOnly? desde, DateOnly? hasta, TimeZoneInfo zona)
    {
        ArgumentNullException.ThrowIfNull(zona);

        return (
            desde is { } inicio ? InicioDelDiaUtc(inicio, zona) : null,
            hasta is { } fin ? InicioDelDiaUtc(fin.AddDays(1), zona) : null);
    }

    private static DateTimeOffset InicioDelDiaUtc(DateOnly fecha, TimeZoneInfo zona)
    {
        var medianoche = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(medianoche, zona.GetUtcOffset(medianoche)).ToUniversalTime();
    }
}
