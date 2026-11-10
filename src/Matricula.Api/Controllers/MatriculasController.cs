using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Matriculas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Las políticas van por acción: matricular a otro es solo del administrador; el detalle, el retiro y el cambio de
// grupo también los usa el estudiante sobre sus propias matrículas (RN-18, en el servicio).
public class MatriculasController(MatriculaService matriculaService) : ApiControllerBase
{
    /// <summary>
    /// Matricula a un estudiante en un grupo (RF-51). Responde RN-01 (estudiante inactivo), RN-03 (sin cupo), RN-05
    /// (curso repetido en el periodo), RN-06 (choque de horario), RN-11 (grupo Finalizado o Cancelado) o CONCURRENCIA.
    /// Al administrador no se le aplican el estado Abierto ni las fechas de matrícula.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<MatriculaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MatriculaResponse>> Matricular(
        MatricularRequest request, CancellationToken cancellationToken)
    {
        var matricula = await matriculaService.MatricularAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = matricula.Id }, matricula);
    }

    /// <summary>
    /// Detalle de una matrícula. Un estudiante solo ve las suyas (RN-18).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Politicas.AdministradorOEstudiante)]
    [ProducesResponseType<MatriculaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatriculaResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await matriculaService.ObtenerAsync(id, cancellationToken));

    /// <summary>
    /// Retira una matrícula (RF-52): queda Retirada con la fecha de hoy y el cupo se libera. Un estudiante solo retira
    /// las suyas (RN-18), con el grupo Abierto (RN-11) y hasta la fecha límite de retiro (RN-10); al administrador no se
    /// le aplican el estado ni la fecha. Una matrícula ya retirada responde RF-52.
    /// </summary>
    [HttpPost("{id:int}/retiro")]
    [Authorize(Policy = Politicas.AdministradorOEstudiante)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retirar(int id, CancellationToken cancellationToken)
    {
        await matriculaService.RetirarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cambia al estudiante a otro grupo del mismo curso y periodo (RF-53). La matrícula original queda Retirada con
    /// motivo CambioGrupo y se crea una nueva en el destino; si alguna regla falla no cambia nada. Responde RF-53 (otro
    /// curso, periodo o el mismo grupo), RN-10 y RN-11 sobre el origen, y RN-02, RN-03, RN-06, RN-11 o CONCURRENCIA
    /// sobre el destino. El Location apunta a la matrícula nueva.
    /// </summary>
    [HttpPost("{id:int}/cambio-grupo")]
    [Authorize(Policy = Politicas.AdministradorOEstudiante)]
    [ProducesResponseType<MatriculaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MatriculaResponse>> CambiarGrupo(
        int id, CambioGrupoRequest request, CancellationToken cancellationToken)
    {
        var nueva = await matriculaService.CambiarGrupoAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = nueva.Id }, nueva);
    }
}
