namespace Matricula.Application.Abstractions;

/// <summary>
/// Emite el token de acceso y los refresh tokens. Lo implementa Api, que conoce el formato JWT y la configuración.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>Duración máxima de una sesión desde el ingreso (RN-50).</summary>
    TimeSpan RefreshTokenLifetime { get; }

    GeneratedToken Generate(TokenData datos);

    GeneratedRefreshToken GenerateRefreshToken();

    string HashRefreshToken(string token);
}
