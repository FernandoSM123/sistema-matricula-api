using Matricula.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matricula.Api.Security;

public static class SecurityExtensions
{
    /// <summary>
    /// Autenticación JWT con validación del SecurityStamp en cada petición, y políticas de autorización.
    /// </summary>
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Configure 'Jwt:Issuer' y 'Jwt:Audience'.")
            .Validate(o => o.Clave.Length >= JwtOptions.LongitudMinimaClave,
                $"'Jwt:Clave' debe tener al menos {JwtOptions.LongitudMinimaClave} caracteres. Defínala en user-secrets o en la variable de entorno Jwt__Clave.")
            .Validate(o => o.MinutosVigencia > 0, "'Jwt:MinutosVigencia' debe ser mayor que cero.")
            .ValidateOnStart();

        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var opciones = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = opciones.Issuer,
                    ValidAudience = opciones.Audience,
                    IssuerSigningKey = JwtTokenGenerator.CreateSigningKey(opciones),
                    NameClaimType = TokenClaims.UsuarioId,
                    RoleClaimType = TokenClaims.Rol,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = SecurityStampValidation.OnTokenValidated };
            });

        services.AddPolicies();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultHandler>();

        return services;
    }
}