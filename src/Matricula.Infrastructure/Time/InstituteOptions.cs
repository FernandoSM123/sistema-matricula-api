namespace Matricula.Infrastructure.Time;

public class InstituteOptions
{
    public const string SectionName = "Instituto";

    /// <summary>Identificador IANA de la zona horaria, por ejemplo America/Costa_Rica.</summary>
    public string ZonaHoraria { get; init; } = string.Empty;

    public bool HasValidTimeZone() =>
        !string.IsNullOrWhiteSpace(ZonaHoraria) && TimeZoneInfo.TryFindSystemTimeZoneById(ZonaHoraria, out _);
}
