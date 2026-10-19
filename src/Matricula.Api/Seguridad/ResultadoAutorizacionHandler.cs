using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Seguridad;

/// <summary>
/// Responde 403 con code "RN-24" cuando el acceso se niega por un cambio de contraseña pendiente;
/// el resto de casos los resuelve el manejador por defecto.
/// </summary>
public class ResultadoAutorizacionHandler(IProblemDetailsService problemDetailsService) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _porDefecto = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        var porCambioPendiente = authorizeResult.Forbidden
            && authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<SinCambioPendienteRequirement>().Any() == true;

        if (!porCambioPendiente)
        {
            await _porDefecto.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Cambio de contraseña pendiente",
            Detail = "Ingresó con una contraseña temporal. Defina su nueva contraseña antes de continuar."
        };
        problem.Extensions["code"] = "RN-24";

        await problemDetailsService.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}