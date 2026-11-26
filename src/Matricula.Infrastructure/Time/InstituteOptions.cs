namespace Matricula.Infrastructure.Time;

public class InstituteOptions
{
    public const string SectionName = "Institute";

    /// <summary>Identificador IANA de la zona horaria, por ejemplo America/Costa_Rica.</summary>
    public string TimeZone { get; init; } = string.Empty;

    public bool HasValidTimeZone() =>
        !string.IsNullOrWhiteSpace(TimeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(TimeZone, out _);
}
