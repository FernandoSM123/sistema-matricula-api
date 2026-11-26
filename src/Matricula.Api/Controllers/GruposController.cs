using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Application.Common.Pagination;
using Matricula.Application.Grupos;
using Matricula.Application.Matriculas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Las políticas van por acción: un atributo de clase se sumaría al de cada una y dejaría las lecturas solo para
// administradores.
public class GruposController(GrupoService grupoService, MatriculaService matriculaService) : ApiControllerBase
{
    /// <summary>
    /// Crea un grupo Planificado y sin horarios (RF-42). Responde RN-17 si el curso, profesor o aula están inactivos,
    /// RN-09 si el cupo supera la capacidad del aula y RN-13 si el NRC ya existe.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<GrupoDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GrupoDetailResponse>> Create(
        CreateGrupoRequest request, CancellationToken cancellationToken)
    {
        var grupo = await grupoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = grupo.Id }, grupo);
    }

    /// <summary>
    /// Edita un grupo (RF-45) y devuelve el detalle. En Planificado se cambia todo; en Abierto, Cerrado o EnCurso solo
    /// profesor, aula, modalidad y cupo; en Finalizado o Cancelado nada (RF-45). Responde RN-17 si un curso, profesor o
    /// aula nuevos están inactivos, RN-09, RN-13 y RN-07/RN-08 si el profesor o aula nuevos chocan con otro grupo.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<GrupoDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GrupoDetailResponse>> Update(
        int id, UpdateGrupoRequest request, CancellationToken cancellationToken) =>
        Ok(await grupoService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Cambia el estado de un grupo (RF-47): Planificado → Abierto → Cerrado → EnCurso → Finalizado, o Cancelado
    /// mientras no haya finalizado. Para abrir hace falta al menos un bloque de horario. Otra transición responde
    /// RF-47; repetir el estado actual responde igual. Cancelar retira las matrículas activas con motivo
    /// CancelacionGrupo y libera sus cupos (RN-12), aunque haya pasado la fecha límite de retiro.
    /// </summary>
    [HttpPost("{id:int}/estado")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEstado(
        int id, ChangeEstadoRequest request, CancellationToken cancellationToken)
    {
        await grupoService.ChangeEstadoAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Agrega un bloque de horario (RF-48) y devuelve el grupo. Responde RN-14 si se traslapa con otro bloque del
    /// grupo, RN-07 o RN-08 si choca con otro grupo del mismo profesor o aula en el periodo, RN-06 si dejaría a algún
    /// estudiante matriculado con choque de horario (da cuántos, sin nombrarlos) y RF-48 si el grupo está finalizado o
    /// cancelado.
    /// </summary>
    [HttpPost("{id:int}/horarios")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType<GrupoDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GrupoDetailResponse>> AddHorario(
        int id, AddHorarioRequest request, CancellationToken cancellationToken)
    {
        var grupo = await grupoService.AddHorarioAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, grupo);
    }

    /// <summary>
    /// Quita un bloque de horario (RF-48). Solo mientras el grupo está Planificado; si no, RF-48.
    /// </summary>
    [HttpDelete("{id:int}/horarios/{horarioId:int}")]
    [Authorize(Policy = Policies.Administrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveHorario(int id, int horarioId, CancellationToken cancellationToken)
    {
        await grupoService.RemoveHorarioAsync(id, horarioId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lista los grupos (RF-43) con filtros por curso, profesor, periodo, estado y modalidad; search busca en el NRC.
    /// Un profesor solo ve los suyos y un estudiante solo los Abierto, aunque envíen otro filtro (RN-18).
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType<PagedResult<GrupoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<GrupoResponse>>> List(
        [FromQuery] ListGruposRequest request, CancellationToken cancellationToken) =>
        Ok(await grupoService.ListAsync(request, cancellationToken));

    /// <summary>
    /// Detalle de un grupo con sus horarios y cupos (RF-44). Un profesor solo ve los suyos y un estudiante solo los
    /// Abierto; otro grupo responde 403 RN-18.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType<GrupoDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GrupoDetailResponse>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await grupoService.GetAsync(id, cancellationToken));

    /// <summary>
    /// Elimina un grupo que nunca tuvo matrículas (RF-46), junto con sus bloques de horario, en cualquier estado. Si
    /// tuvo alguna, aunque esté retirada, responde 409 RN-34: se cancela en su lugar.
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
        await grupoService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Estudiantes de un grupo (RF-49), para el administrador o el profesor del grupo; otro profesor recibe 403 RN-18.
    /// Por defecto los de matrícula activa (estado=Retirada para los retirados); search busca en nombre, apellidos y
    /// cédula. No incluye teléfono ni dirección.
    /// </summary>
    [HttpGet("{id:int}/estudiantes")]
    [Authorize(Policy = Policies.AdministradorOrProfesor)]
    [ProducesResponseType<PagedResult<EstudianteGrupoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<EstudianteGrupoResponse>>> ListEstudiantes(
        int id, [FromQuery] ListEstudiantesGrupoRequest request, CancellationToken cancellationToken) =>
        Ok(await matriculaService.ListEstudiantesAsync(id, request, cancellationToken));
}
