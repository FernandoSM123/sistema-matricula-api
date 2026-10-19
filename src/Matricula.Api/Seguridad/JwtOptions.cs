namespace Matricula.Api.Seguridad;

/// <summary>
/// Configuración del token. <see cref="Clave"/> va solo en user-secrets o en variables de entorno.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int LongitudMinimaClave = 32;

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Clave { get; init; } = string.Empty;
    public int MinutosVigencia { get; init; } = 60;
}