using System.Globalization;
using System.Text;
using Matricula.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Matricula.Api.Seguridad;

public class GeneradorToken(IOptions<JwtOptions> options, TimeProvider timeProvider) : IGeneradorToken
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public static SymmetricSecurityKey CrearClave(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Clave));
    }

    public TokenGenerado Generar(DatosToken datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        var ahora = timeProvider.GetUtcNow();
        var expira = ahora.AddMinutes(_options.MinutosVigencia);

        var claims = new Dictionary<string, object>
        {
            [ClaimsToken.UsuarioId] = datos.UsuarioId.ToString(CultureInfo.InvariantCulture),
            [ClaimsToken.Correo] = datos.Correo,
            [ClaimsToken.Rol] = datos.Rol.ToString(),
            [ClaimsToken.SecurityStamp] = datos.SecurityStamp
        };
        if (datos.DebeCambiarContrasena)
        {
            claims[ClaimsToken.DebeCambiarContrasena] = "true";
        }
        if (datos.EstudianteId is { } estudianteId)
        {
            claims[ClaimsToken.EstudianteId] = estudianteId.ToString(CultureInfo.InvariantCulture);
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(CrearClave(_options), SecurityAlgorithms.HmacSha256)
        };

        return new TokenGenerado(_handler.CreateToken(descriptor), expira);
    }
}