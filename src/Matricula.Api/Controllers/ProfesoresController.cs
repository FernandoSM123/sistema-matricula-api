using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Grupos;
using Matricula.Application.Grupos.Consultas;
using Matricula.Application.Profesores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

[Authorize(Policy = Politicas.Administrador)]
public class ProfesoresController(
    ProfesorService profesorService, GrupoService grupoService, HorarioSemanalService horarioService) : ApiControllerBase
{
    /// <summary>
    /// Registra un profesor (RF-03, RF-22). La temporal se envía a su correo y nunca aparece en la respuesta; con
    /// correoEnviado en false se puede reenviar.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ProfesorRegistradoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfesorRegistradoResponse>> Registrar(
        RegistrarProfesorRequest request, CancellationToken cancellationToken)
    {
        var registrado = await profesorService.RegistrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = registrado.Profesor.Id }, registrado);
    }

    /// <summary>
    /// Lista los profesores (RF-23). Por defecto solo los activos; search busca en nombre, apellidos, cédula,
    /// especialidad y correo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProfesorResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProfesorResponse>>> Listar(
        [FromQuery] ListarProfesoresRequest request, CancellationToken cancellationToken) =>
        Ok(await profesorService.ListarAsync(request, cancellationToken));

    /// <summary>
    /// Modifica los datos personales y el correo de un profesor (RF-25).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProfesorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfesorResponse>> Actualizar(
        int id, ActualizarProfesorRequest request, CancellationToken cancellationToken) =>
        Ok(await profesorService.ActualizarAsync(id, request, cancellationToken));

    /// <summary>
    /// Desactiva al profesor y su cuenta (RF-26, RN-38): no puede ingresar y sus sesiones se cierran.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken)
    {
        await profesorService.DesactivarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactiva al profesor y su cuenta (RF-26, RN-38, RN-39).
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivar(int id, CancellationToken cancellationToken)
    {
        await profesorService.ReactivarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Detalle de un profesor (RF-24).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProfesorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfesorResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await profesorService.ObtenerAsync(id, cancellationToken));

    /// <summary>
    /// Grupos de un profesor en todos los estados (RF-27), con filtro por periodo; search busca en el NRC.
    /// </summary>
    [HttpGet("{id:int}/grupos")]
    [ProducesResponseType<PagedResult<GrupoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<GrupoResponse>>> ListarGrupos(
        int id, [FromQuery] ListarGruposProfesorRequest request, CancellationToken cancellationToken) =>
        Ok(await grupoService.ListarDeProfesorAsync(id, request, cancellationToken));

    /// <summary>
    /// Horario semanal de un profesor (RF-28): sus bloques del periodo, sin grupos cancelados, por día y hora. Sin
    /// periodoId usa el vigente; si no hay vigente responde 404 pidiendo el periodoId.
    /// </summary>
    [HttpGet("{id:int}/horario")]
    [ProducesResponseType<IReadOnlyList<BloqueHorarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BloqueHorarioResponse>>> Horario(
        int id, [FromQuery] int? periodoId, CancellationToken cancellationToken) =>
        Ok(await horarioService.HorarioProfesorAsync(id, periodoId, cancellationToken));
}
