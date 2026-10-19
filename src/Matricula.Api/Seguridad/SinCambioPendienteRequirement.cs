using Microsoft.AspNetCore.Authorization;

namespace Matricula.Api.Seguridad;

/// <summary>
/// RN-24: quien ingresó con una temporal solo puede hacer el cambio obligatorio o cerrar sesión.
/// Es su propio handler, así no hay que registrarlo aparte.
/// </summary>
public class SinCambioPendienteRequirement : AuthorizationHandler<SinCambioPendienteRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SinCambioPendienteRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.User.HasClaim(c => c.Type == ClaimsToken.DebeCambiarContrasena))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}