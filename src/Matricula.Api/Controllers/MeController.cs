using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Auth;
using Matricula.Application.Estudiantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

/// <summary>
/// Recursos del usuario autenticado ("lo mío").
/// </summary>
public class MeController(AuthService authService, EstudianteService estudianteService) : ApiControllerBase
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

    /// <summary>
    /// El estudiante edita su teléfono y su dirección. El correo y los demás datos los cambia un administrador.
    /// Devuelve el perfil actualizado.
    /// </summary>
    [HttpPut("datos-contacto")]
    [Authorize(Policy = Politicas.Estudiante)]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PerfilResponse>> ActualizarDatosContacto(
        ActualizarContactoRequest request, CancellationToken cancellationToken)
    {
        await estudianteService.ActualizarContactoAsync(request, cancellationToken);
        return Ok(await authService.ObtenerPerfilAsync(cancellationToken));
    }
}
