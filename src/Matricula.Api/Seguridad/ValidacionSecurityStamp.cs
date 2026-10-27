using System.Security.Claims;
using Matricula.Application.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Matricula.Api.Seguridad;

/// <summary>
/// Rechaza el token si la cuenta se desactivó o su stamp cambió (logout, cambio de contraseña o desactivación, RN-29).
/// </summary>
public static class ValidacionSecurityStamp
{
    public static async Task OnTokenValidated(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var principal = context.Principal;
        var stamp = principal?.FindFirstValue(ClaimsToken.SecurityStamp);
        if (!int.TryParse(principal?.FindFirstValue(ClaimsToken.UsuarioId), out var usuarioId) || stamp is null)
        {
            context.Fail("El token no tiene los datos de la sesión.");
            return;
        }

        var authService = context.HttpContext.RequestServices.GetRequiredService<AuthService>();
        if (!await authService.SesionValidaAsync(usuarioId, stamp, context.HttpContext.RequestAborted))
        {
            context.Fail("La sesión ya no es válida.");
        }
    }
}