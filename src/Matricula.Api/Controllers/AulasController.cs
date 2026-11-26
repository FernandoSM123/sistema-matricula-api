using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Application.Aulas;
using Matricula.Application.Common.Pagination;
using Matricula.Application.Grupos.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Las políticas van por acción: un atributo de clase se sumaría al de cada una y dejaría las lecturas solo para
// administradores.
public class AulasController(AulaService aulaService, HorarioSemanalService horarioService) : ApiControllerBase
{
    /// <summary>
    /// Registra un aula (RF-39). El código de un aula activa responde RN-13 y el de una inactiva RN-39 con aulaId.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AulaResponse>> Create(
        RegistrarAulaRequest request, CancellationToken cancellationToken)
    {
        var aula = await aulaService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = aula.Id }, aula);
    }

    /// <summary>
    /// Lista las aulas (RF-40). Por defecto solo las activas; search busca en código y ubicación.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdministradorOrProfesor)]
    [ProducesResponseType<PagedResult<AulaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AulaResponse>>> List(
        [FromQuery] ListarAulasRequest request, CancellationToken cancellationToken) =>
        Ok(await aulaService.ListAsync(request, cancellationToken));

    /// <summary>
    /// Modifica los datos de un aula (RF-40), incluso si está inactiva.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AulaResponse>> Update(
        int id, ActualizarAulaRequest request, CancellationToken cancellationToken) =>
        Ok(await aulaService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Desactiva un aula (RF-40). Repetir la operación responde igual.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await aulaService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactiva un aula (RF-40, RN-39). Repetir la operación responde igual.
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        await aulaService.ReactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Elimina un aula que nunca tuvo grupos (RF-40). Si tuvo alguno, incluso cancelado, responde 409 RN-34: se
    /// desactiva en su lugar.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await aulaService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Detalle de un aula (RF-40).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.AdministradorOrProfesor)]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AulaResponse>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await aulaService.GetAsync(id, cancellationToken));

    /// <summary>
    /// Ocupación de un aula (RF-41): los bloques de sus grupos en el periodo, sin cancelados, por día y hora. Sin
    /// periodoId usa el vigente; si no hay vigente responde 404 pidiendo el periodoId.
    /// </summary>
    [HttpGet("{id:int}/ocupacion")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<IReadOnlyList<BloqueHorarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BloqueHorarioResponse>>> GetOcupacion(
        int id, [FromQuery] int? periodoId, CancellationToken cancellationToken) =>
        Ok(await horarioService.GetAulaOcupacionAsync(id, periodoId, cancellationToken));
}
