using Matricula.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Matricula.Api.Seguridad;

/// <summary>
/// Políticas de autorización. Para restringir por rol se usan <see cref="Administrador"/> y <see cref="Estudiante"/>, no
/// [Authorize(Roles = ...)]: el atributo con Roles reemplaza la política por defecto y dejaría pasar
/// a un usuario con cambio de contraseña pendiente (RN-24).
/// </summary>
public static class Politicas
{
    /// <summary>Solo exige sesión. La usan únicamente el cambio obligatorio y el logout.</summary>
    public const string PasswordChangePending = nameof(PasswordChangePending);

    public const string Administrador = nameof(Administrador);

    public const string Estudiante = nameof(Estudiante);

    public static IServiceCollection AddPoliticas(this IServiceCollection services)
    {
        var conSesionSinCambioPendiente = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new SinCambioPendienteRequirement())
            .Build();

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(conSesionSinCambioPendiente)
            // Todo endpoint sin [Authorize] ni [AllowAnonymous] exige sesión (sección 7 de los requerimientos).
            .SetFallbackPolicy(conSesionSinCambioPendiente)
            .AddPolicy(PasswordChangePending, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Administrador, policy => policy
                .Combine(conSesionSinCambioPendiente)
                .RequireRole(nameof(Rol.Administrador)))
            .AddPolicy(Estudiante, policy => policy
                .Combine(conSesionSinCambioPendiente)
                .RequireRole(nameof(Rol.Estudiante)));

        return services;
    }
}