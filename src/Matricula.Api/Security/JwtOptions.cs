namespace Matricula.Api.Security;

/// <summary>
/// Configuración del token. <see cref="Clave"/> va solo en user-secrets o en variables de entorno.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32;

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 60;
}