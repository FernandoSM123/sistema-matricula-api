using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

public class AuthController(AuthService authService) : ApiControllerBase
{
    /// <summary>
    /// Inicia sesión con la contraseña definitiva o con una temporal (RF-06, RF-07). Con una temporal,
    /// el token solo sirve para el cambio obligatorio y el logout.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    /// <summary>
    /// Cambio obligatorio tras ingresar con una temporal (RF-07): nueva contraseña y confirmación.
    /// Devuelve un token nuevo, sin cambio pendiente; el anterior deja de funcionar.
    /// </summary>
    [HttpPost("cambio-obligatorio")]
    [Authorize(Policy = Politicas.PasswordChangePending)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> CambioObligatorio(CambioObligatorioRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.CambioObligatorioAsync(request, cancellationToken));
}