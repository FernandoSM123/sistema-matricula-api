using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Matricula.Api.Controllers;

public class AuthController(AuthService authService) : ApiControllerBase
{
    /// <summary>
    /// Inicia sesión con la contraseña definitiva o con una temporal (RF-06, RF-07). Con una temporal,
    /// el token solo sirve para el cambio obligatorio y el logout.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    /// <summary>
    /// Renueva la sesión (RF-60): entrega un token de acceso nuevo y reemplaza el refresh token (RN-51). Es anónimo
    /// porque el token de acceso puede estar vencido. La sesión no se extiende (RN-50). Todos los rechazos dan el
    /// mismo 401 con code RF-60; reusar un refresh token ya reemplazado termina la sesión.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.RefreshAsync(request, cancellationToken));

    /// <summary>
    /// Olvido de contraseña (RF-10). Si el correo es de una cuenta activa, envía una temporal que vence en 1 hora.
    /// Responde siempre lo mismo, exista o no el correo (RN-28).
    /// </summary>
    [HttpPost("olvido-contrasena")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ForgotPassword)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.ForgotPasswordAsync(request, cancellationToken));

    /// <summary>
    /// Cambio obligatorio tras ingresar con una temporal (RF-07): nueva contraseña y confirmación.
    /// Devuelve un token nuevo, sin cambio pendiente; el anterior deja de funcionar.
    /// </summary>
    [HttpPost("cambio-obligatorio")]
    [Authorize(Policy = Policies.PasswordChangePending)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> RequiredPasswordChange(RequiredPasswordChangeRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.RequiredPasswordChangeAsync(request, cancellationToken));

    /// <summary>
    /// Cambio de la contraseña propia (RF-09): actual, nueva y confirmación. Envía un aviso por correo y
    /// devuelve un token nuevo; los anteriores dejan de funcionar.
    /// </summary>
    [HttpPost("cambiar-contrasena")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.ChangePasswordAsync(request, cancellationToken));

    /// <summary>
    /// Cierra sesión (RF-08). Invalida todos los tokens del usuario. Se permite también con cambio pendiente.
    /// </summary>
    [HttpPost("logout")]
    [Authorize(Policy = Policies.PasswordChangePending)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(cancellationToken);
        return NoContent();
    }
}