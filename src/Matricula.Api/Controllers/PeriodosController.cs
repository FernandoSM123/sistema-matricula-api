using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Common.Paginacion;
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
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<PeriodoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PeriodoResponse>> Registrar(
        RegistrarPeriodoRequest request, CancellationToken cancellationToken)
    {
        var periodo = await periodoService.RegistrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = periodo.Id }, periodo);
    }

    /// <summary>
    /// Lista los periodos (RF-36), los más recientes primero; search busca en el nombre.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<PagedResult<PeriodoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PeriodoResponse>>> Listar(
        [FromQuery] ListarPeriodosRequest request, CancellationToken cancellationToken) =>
        Ok(await periodoService.ListarAsync(request, cancellationToken));

    /// <summary>
    /// Detalle de un periodo (RF-36).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType<PeriodoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PeriodoResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await periodoService.ObtenerAsync(id, cancellationToken));

    /// <summary>
    /// Modifica los datos de un periodo (RF-36), incluso si ya pasó.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<PeriodoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PeriodoResponse>> Actualizar(
        int id, ActualizarPeriodoRequest request, CancellationToken cancellationToken) =>
        Ok(await periodoService.ActualizarAsync(id, request, cancellationToken));
}
