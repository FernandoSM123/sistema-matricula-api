using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Grupos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Las políticas van por acción: un atributo de clase se sumaría al de cada una y dejaría las lecturas solo para
// administradores.
public class GruposController(GrupoService grupoService) : ApiControllerBase
{
    /// <summary>
    /// Crea un grupo Planificado y sin horarios (RF-42). Responde RN-17 si el curso, profesor o aula están inactivos,
    /// RN-09 si el cupo supera la capacidad del aula y RN-13 si el NRC ya existe.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<GrupoDetalleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GrupoDetalleResponse>> Crear(
        CrearGrupoRequest request, CancellationToken cancellationToken)
    {
        var grupo = await grupoService.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = grupo.Id }, grupo);
    }

    /// <summary>
    /// Detalle de un grupo con sus horarios y cupos (RF-44).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<GrupoDetalleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GrupoDetalleResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await grupoService.ObtenerAsync(id, cancellationToken));
}
