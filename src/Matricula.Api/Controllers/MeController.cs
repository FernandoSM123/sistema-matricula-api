using Matricula.Api.Common;
using Matricula.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

/// <summary>
/// Recursos del usuario autenticado ("lo mío").
/// </summary>
public class MeController(AuthService authService) : ApiControllerBase
{
    /// <summary>
    /// Perfil propio: datos de la cuenta, rol y datos personales (RF-13).
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PerfilResponse>> Perfil(CancellationToken cancellationToken) =>
        Ok(await authService.ObtenerPerfilAsync(cancellationToken));
}