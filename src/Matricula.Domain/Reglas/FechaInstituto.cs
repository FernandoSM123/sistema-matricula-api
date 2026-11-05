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
}
