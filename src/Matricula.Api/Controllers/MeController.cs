using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Auth;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Estudiantes;
using Matricula.Application.Grupos;
using Matricula.Application.Grupos.Consultas;
using Matricula.Application.Matriculas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

/// <summary>
/// Recursos del usuario autenticado ("lo mío").
/// </summary>
public class MeController(
    AuthService authService,
    EstudianteService estudianteService,
    GrupoService grupoService,
    HorarioSemanalService horarioService,
    MatriculaService matriculaService) : ApiControllerBase
{
    /// <summary>
    /// El estudiante de la sesión se matricula en un grupo (RF-51). El grupo debe estar Abierto y dentro del rango de
    /// matrícula; responde RN-02, RN-03, RN-04, RN-05, RN-06, RN-11 o CONCURRENCIA. El Location apunta a
    /// /matriculas/{id}.
    /// </summary>
    [HttpPost("matriculas")]
    [Authorize(Policy = Politicas.Estudiante)]
    [ProducesResponseType<MatriculaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MatriculaResponse>> Matricularme(
        MatricularPropioRequest request, CancellationToken cancellationToken)
    {
        var matricula = await matriculaService.MatricularPropioAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(MatriculasController.Obtener), "Matriculas", new { id = matricula.Id }, matricula);
    }

    /// <summary>
    /// Grupos del profesor de la sesión en todos los estados (RF-27), con filtro por periodo; search busca en el NRC.
    /// </summary>
    [HttpGet("grupos")]
    [Authorize(Policy = Politicas.Profesor)]
    [ProducesResponseType<PagedResult<GrupoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<GrupoResponse>>> MisGrupos(
        [FromQuery] ListarGruposProfesorRequest request, CancellationToken cancellationToken) =>
        Ok(await grupoService.ListarMisGruposAsync(request, cancellationToken));

    /// <summary>
    /// Horario semanal del profesor de la sesión (RF-28). Sin periodoId usa el vigente; si no hay vigente responde 404
    /// pidiendo el periodoId. El módulo matriculas lo amplía a los estudiantes (RF-21).
    /// </summary>
    [HttpGet("horario")]
    [Authorize(Policy = Politicas.Profesor)]
    [ProducesResponseType<IReadOnlyList<BloqueHorarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BloqueHorarioResponse>>> MiHorario(
        [FromQuery] int? periodoId, CancellationToken cancellationToken) =>
        Ok(await horarioService.MiHorarioAsync(periodoId, cancellationToken));

    /// <summary>
    /// Perfil propio: datos de la cuenta, rol y datos personales (RF-13).
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PerfilResponse>> Perfil(CancellationToken cancellationToken) =>
        Ok(await authService.ObtenerPerfilAsync(cancellationToken));

    /// <summary>
    /// El estudiante edita su teléfono y su dirección. El correo y los demás datos los cambia un administrador.
    /// Devuelve el perfil actualizado.
    /// </summary>
    [HttpPut("datos-contacto")]
    [Authorize(Policy = Politicas.Estudiante)]
    [ProducesResponseType<PerfilResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PerfilResponse>> ActualizarDatosContacto(
        ActualizarContactoRequest request, CancellationToken cancellationToken)
    {
        await estudianteService.ActualizarContactoAsync(request, cancellationToken);
        return Ok(await authService.ObtenerPerfilAsync(cancellationToken));
    }
}
