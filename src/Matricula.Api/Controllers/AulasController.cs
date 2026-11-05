using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Aulas;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Grupos.Consultas;
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
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AulaResponse>> Registrar(
        RegistrarAulaRequest request, CancellationToken cancellationToken)
    {
        var aula = await aulaService.RegistrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = aula.Id }, aula);
    }

    /// <summary>
    /// Lista las aulas (RF-40). Por defecto solo las activas; search busca en código y ubicación.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Politicas.AdministradorOProfesor)]
    [ProducesResponseType<PagedResult<AulaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AulaResponse>>> Listar(
        [FromQuery] ListarAulasRequest request, CancellationToken cancellationToken) =>
        Ok(await aulaService.ListarAsync(request, cancellationToken));

    /// <summary>
    /// Modifica los datos de un aula (RF-40), incluso si está inactiva.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AulaResponse>> Actualizar(
        int id, ActualizarAulaRequest request, CancellationToken cancellationToken) =>
        Ok(await aulaService.ActualizarAsync(id, request, cancellationToken));

    /// <summary>
    /// Desactiva un aula (RF-40). Repetir la operación responde igual.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken)
    {
        await aulaService.DesactivarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactiva un aula (RF-40, RN-39). Repetir la operación responde igual.
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivar(int id, CancellationToken cancellationToken)
    {
        await aulaService.ReactivarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Detalle de un aula (RF-40).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Politicas.AdministradorOProfesor)]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AulaResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await aulaService.ObtenerAsync(id, cancellationToken));

    /// <summary>
    /// Ocupación de un aula (RF-41): los bloques de sus grupos en el periodo, sin cancelados, por día y hora. Sin
    /// periodoId usa el vigente; si no hay vigente responde 404 pidiendo el periodoId.
    /// </summary>
    [HttpGet("{id:int}/ocupacion")]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<IReadOnlyList<BloqueHorarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BloqueHorarioResponse>>> Ocupacion(
        int id, [FromQuery] int? periodoId, CancellationToken cancellationToken) =>
        Ok(await horarioService.OcupacionAulaAsync(id, periodoId, cancellationToken));
}
