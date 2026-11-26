using Microsoft.AspNetCore.Authorization;

namespace Matricula.Api.Security;

/// <summary>
/// RN-24: quien ingresó con una temporal solo puede hacer el cambio obligatorio o cerrar sesión.
/// Es su propio handler, así no hay que registrarlo aparte.
/// </summary>
public class NoPasswordChangePendingRequirement : AuthorizationHandler<NoPasswordChangePendingRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NoPasswordChangePendingRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.User.HasClaim(c => c.Type == TokenClaims.DebeCambiarContrasena))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}