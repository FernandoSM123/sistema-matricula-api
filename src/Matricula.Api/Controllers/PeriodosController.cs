using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Application.Common.Pagination;
using Matricula.Application.Periodos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Las políticas van por acción: un atributo de clase se sumaría al de cada una y dejaría las lecturas solo para
// administradores.
public class PeriodosController(PeriodoService periodoService) : ApiControllerBase
{
    /// <summary>
    /// Registra un periodo (RF-35). Unas fechas incoherentes (RN-16) responden 400 en el campo correspondiente.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<PeriodoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PeriodoResponse>> Create(
        RegistrarPeriodoRequest request, CancellationToken cancellationToken)
    {
        var periodo = await periodoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = periodo.Id }, periodo);
    }

    /// <summary>
    /// Lista los periodos (RF-36), los más recientes primero; search busca en el nombre.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<PagedResult<PeriodoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PeriodoResponse>>> List(
        [FromQuery] ListarPeriodosRequest request, CancellationToken cancellationToken) =>
        Ok(await periodoService.ListAsync(request, cancellationToken));

    /// <summary>
    /// Periodo vigente según la fecha del instituto (RF-38): desde que abre su matrícula hasta el fin de clases. Si no
    /// hay ninguno responde 404. Es público, como la oferta académica, para que el visitante sepa de qué periodo es y
    /// hasta cuándo puede matricular (COR-01, B-01).
    /// </summary>
    [HttpGet("vigente")]
    [AllowAnonymous]
    [ProducesResponseType<PeriodoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PeriodoResponse>> GetVigente(CancellationToken cancellationToken) =>
        Ok(await periodoService.GetVigenteAsync(cancellationToken));

    /// <summary>
    /// Detalle de un periodo (RF-36).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType<PeriodoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PeriodoResponse>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await periodoService.GetAsync(id, cancellationToken));

    /// <summary>
    /// Modifica los datos de un periodo (RF-36), incluso si ya pasó.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<PeriodoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PeriodoResponse>> Update(
        int id, ActualizarPeriodoRequest request, CancellationToken cancellationToken) =>
        Ok(await periodoService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Elimina un periodo sin grupos (RF-37). Si tiene alguno, incluso cancelado, responde 409 RN-34.
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
        await periodoService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
