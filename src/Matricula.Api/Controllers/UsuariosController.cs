using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Cuentas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

/// <summary>
/// Acciones sobre cuentas de cualquier rol. El id es el usuarioId que exponen administradores, estudiantes y profesores.
/// </summary>
[Authorize(Policy = Politicas.Administrador)]
public class UsuariosController(UsuarioService usuarioService) : ApiControllerBase
{
    /// <summary>
    /// Genera y envía una nueva contraseña temporal (RF-12). La anterior deja de funcionar.
    /// </summary>
    [HttpPost("{id:int}/reenviar-temporal")]
    [ProducesResponseType<ReenvioTemporalResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReenvioTemporalResponse>> ReenviarTemporal(int id, CancellationToken cancellationToken) =>
        Ok(await usuarioService.ReenviarTemporalAsync(id, cancellationToken));

    /// <summary>
    /// Desactiva la cuenta (RF-14): no puede ingresar y sus sesiones se cierran. No se permite con el último administrador activo.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken)
    {
        await usuarioService.DesactivarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactiva la cuenta (RF-14).
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivar(int id, CancellationToken cancellationToken)
    {
        await usuarioService.ReactivarAsync(id, cancellationToken);
        return NoContent();
    }
}