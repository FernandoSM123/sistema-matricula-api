namespace Matricula.Infrastructure.Tiempo;

public class InstitutoOptions
{
    public const string SectionName = "Instituto";

    /// <summary>Identificador IANA de la zona horaria, por ejemplo America/Costa_Rica.</summary>
    public string ZonaHoraria { get; init; } = string.Empty;

    public bool TieneZonaValida() =>
        !string.IsNullOrWhiteSpace(ZonaHoraria) && TimeZoneInfo.TryFindSystemTimeZoneById(ZonaHoraria, out _);
}
