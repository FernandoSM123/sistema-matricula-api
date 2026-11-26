using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Application.Common.Pagination;
using Matricula.Application.Cursos;
using Matricula.Application.Grupos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Las políticas van por acción: un atributo de clase se sumaría al de cada una y dejaría las lecturas solo para
// administradores.
public class CursosController(CursoService cursoService, GrupoService grupoService) : ApiControllerBase
{
    /// <summary>
    /// Registra un curso (RF-29). El código de un curso activo responde RN-13 y el de uno inactivo RN-39 con cursoId.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<CursoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CursoResponse>> Create(
        RegistrarCursoRequest request, CancellationToken cancellationToken)
    {
        var curso = await cursoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = curso.Id }, curso);
    }

    /// <summary>
    /// Lista los cursos (RF-30). Por defecto solo los activos; search busca en nombre y código, y nivel filtra.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<PagedResult<CursoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<CursoResponse>>> List(
        [FromQuery] ListarCursosRequest request, CancellationToken cancellationToken) =>
        Ok(await cursoService.ListAsync(request, cancellationToken));

    /// <summary>
    /// Modifica los datos de un curso (RF-32), incluso si está inactivo.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<CursoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CursoResponse>> Update(
        int id, ActualizarCursoRequest request, CancellationToken cancellationToken) =>
        Ok(await cursoService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Desactiva un curso (RF-33). Repetir la operación responde igual.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await cursoService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactiva un curso (RF-33, RN-39). Repetir la operación responde igual.
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        await cursoService.ReactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Elimina un curso que nunca tuvo grupos (RF-33). Si tuvo alguno, incluso cancelado, responde 409 RN-34: se
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
        await cursoService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Detalle de un curso (RF-31).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType<CursoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CursoResponse>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await cursoService.GetAsync(id, cancellationToken));

    /// <summary>
    /// Grupos de un curso (RF-34), con filtros por periodo y estado; search busca en el NRC. El administrador ve
    /// cualquier estado; profesores y estudiantes solo los Abierto. Curso inexistente → 404.
    /// </summary>
    [HttpGet("{id:int}/grupos")]
    [Authorize]
    [ProducesResponseType<PagedResult<GrupoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<GrupoResponse>>> ListGrupos(
        int id, [FromQuery] ListarGruposCursoRequest request, CancellationToken cancellationToken) =>
        Ok(await grupoService.ListByCursoAsync(id, request, cancellationToken));
}
