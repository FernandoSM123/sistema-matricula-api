using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Application.Common.Pagination;
using Matricula.Application.Grupos;
using Matricula.Application.Grupos.Queries;
using Matricula.Application.Profesores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

[Authorize(Policy = Policies.Administrador)]
public class ProfesoresController(
    ProfesorService profesorService, GrupoService grupoService, HorarioSemanalService horarioService) : ApiControllerBase
{
    /// <summary>
    /// Registra un profesor (RF-03, RF-22). La temporal se envía a su correo y nunca aparece en la respuesta; con
    /// correoEnviado en false se puede reenviar.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ProfesorCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfesorCreatedResponse>> Create(
        CreateProfesorRequest request, CancellationToken cancellationToken)
    {
        var registrado = await profesorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = registrado.Profesor.Id }, registrado);
    }

    /// <summary>
    /// Lista los profesores (RF-23). Por defecto solo los activos; search busca en nombre, apellidos, cédula,
    /// especialidad y correo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProfesorResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProfesorResponse>>> List(
        [FromQuery] ListProfesoresRequest request, CancellationToken cancellationToken) =>
        Ok(await profesorService.ListAsync(request, cancellationToken));

    /// <summary>
    /// Modifica los datos personales y el correo de un profesor (RF-25).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProfesorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfesorResponse>> Update(
        int id, UpdateProfesorRequest request, CancellationToken cancellationToken) =>
        Ok(await profesorService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Desactiva al profesor y su cuenta (RF-26, RN-38): no puede ingresar y sus sesiones se cierran.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await profesorService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactiva al profesor y su cuenta (RF-26, RN-38, RN-39).
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        await profesorService.ReactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Detalle de un profesor (RF-24).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProfesorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfesorResponse>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await profesorService.GetAsync(id, cancellationToken));

    /// <summary>
    /// Elimina un profesor que nunca tuvo grupos, junto con su cuenta (RF-26, RN-35). Si tuvo alguno, incluso
    /// cancelado, responde 409 RN-34: se desactiva en su lugar.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await profesorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Grupos de un profesor en todos los estados (RF-27), con filtro por periodo; search busca en el NRC.
    /// </summary>
    [HttpGet("{id:int}/grupos")]
    [ProducesResponseType<PagedResult<GrupoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<GrupoResponse>>> ListGrupos(
        int id, [FromQuery] ListGruposProfesorRequest request, CancellationToken cancellationToken) =>
        Ok(await grupoService.ListByProfesorAsync(id, request, cancellationToken));

    /// <summary>
    /// Horario semanal de un profesor (RF-28): sus bloques del periodo, sin grupos cancelados, por día y hora. Sin
    /// periodoId usa el vigente; si no hay vigente responde 404 pidiendo el periodoId.
    /// </summary>
    [HttpGet("{id:int}/horario")]
    [ProducesResponseType<IReadOnlyList<BloqueHorarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BloqueHorarioResponse>>> GetHorario(
        int id, [FromQuery] int? periodoId, CancellationToken cancellationToken) =>
        Ok(await horarioService.GetProfesorHorarioAsync(id, periodoId, cancellationToken));
}
