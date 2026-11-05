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
    /// Cambia el estado de un grupo (RF-47): Planificado → Abierto → Cerrado → EnCurso → Finalizado, o Cancelado
    /// mientras no haya finalizado. Para abrir hace falta al menos un bloque de horario. Otra transición responde
    /// RF-47; repetir el estado actual responde igual.
    /// </summary>
    [HttpPost("{id:int}/estado")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(
        int id, CambiarEstadoRequest request, CancellationToken cancellationToken)
    {
        await grupoService.CambiarEstadoAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Agrega un bloque de horario (RF-48) y devuelve el grupo. Responde RN-14 si se traslapa con otro bloque del
    /// grupo, RN-07 o RN-08 si choca con otro grupo del mismo profesor o aula en el periodo, y RF-48 si el grupo está
    /// finalizado o cancelado.
    /// </summary>
    [HttpPost("{id:int}/horarios")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<GrupoDetalleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GrupoDetalleResponse>> AgregarHorario(
        int id, AgregarHorarioRequest request, CancellationToken cancellationToken)
    {
        var grupo = await grupoService.AgregarHorarioAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id }, grupo);
    }

    /// <summary>
    /// Quita un bloque de horario (RF-48). Solo mientras el grupo está Planificado; si no, RF-48.
    /// </summary>
    [HttpDelete("{id:int}/horarios/{horarioId:int}")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> QuitarHorario(int id, int horarioId, CancellationToken cancellationToken)
    {
        await grupoService.QuitarHorarioAsync(id, horarioId, cancellationToken);
        return NoContent();
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
