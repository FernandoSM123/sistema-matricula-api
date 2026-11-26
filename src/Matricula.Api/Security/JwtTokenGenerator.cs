using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Matricula.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Matricula.Api.Security;

public class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenGenerator
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    private const int RefreshTokenBytes = 32;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromMinutes(_options.RefreshTokenMinutes);

    /// <summary>
    /// 32 bytes aleatorios en Base64Url (43 caracteres) y su SHA-256, que es lo único que se guarda.
    /// </summary>
    public GeneratedRefreshToken GenerateRefreshToken()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        return new GeneratedRefreshToken(token, HashRefreshToken(token));
    }

    public string HashRefreshToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public static SymmetricSecurityKey CreateSigningKey(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
    }

    public GeneratedToken Generate(TokenData datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        var ahora = timeProvider.GetUtcNow();
        var expira = ahora.AddMinutes(_options.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            [TokenClaims.UsuarioId] = datos.UsuarioId.ToString(CultureInfo.InvariantCulture),
            [TokenClaims.Correo] = datos.Correo,
            [TokenClaims.Rol] = datos.Rol.ToString(),
            [TokenClaims.SecurityStamp] = datos.SecurityStamp
        };
        if (datos.DebeCambiarContrasena)
        {
            claims[TokenClaims.DebeCambiarContrasena] = "true";
        }
        if (datos.EstudianteId is { } estudianteId)
        {
            claims[TokenClaims.EstudianteId] = estudianteId.ToString(CultureInfo.InvariantCulture);
        }
        if (datos.ProfesorId is { } profesorId)
        {
            claims[TokenClaims.ProfesorId] = profesorId.ToString(CultureInfo.InvariantCulture);
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(CreateSigningKey(_options), SecurityAlgorithms.HmacSha256)
        };

        return new GeneratedToken(_handler.CreateToken(descriptor), expira);
    }
}