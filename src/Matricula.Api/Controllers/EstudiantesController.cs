using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Estudiantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

[Authorize(Policy = Politicas.Administrador)]
public class EstudiantesController(EstudianteService estudianteService) : ApiControllerBase
{
    /// <summary>
    /// Registra un estudiante por un administrador (RF-02). La temporal se envía a su correo y nunca aparece en
    /// la respuesta; con correoEnviado en false se puede reenviar.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<EstudianteRegistradoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstudianteRegistradoResponse>> Registrar(
        RegistrarEstudianteRequest request, CancellationToken cancellationToken)
    {
        var registrado = await estudianteService.RegistrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = registrado.Estudiante.Id }, registrado);
    }

    /// <summary>
    /// Lista los estudiantes (RF-16). Por defecto solo los activos; search busca en nombre, apellidos, cédula
    /// y correo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<EstudianteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EstudianteResponse>>> Listar(
        [FromQuery] ListarEstudiantesRequest request, CancellationToken cancellationToken) =>
        Ok(await estudianteService.ListarAsync(request, cancellationToken));

    /// <summary>
    /// Modifica los datos personales y el correo de un estudiante (RF-18). La fecha de registro no cambia.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<EstudianteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstudianteResponse>> Actualizar(
        int id, ActualizarEstudianteRequest request, CancellationToken cancellationToken) =>
        Ok(await estudianteService.ActualizarAsync(id, request, cancellationToken));

    /// <summary>
    /// Desactiva al estudiante y su cuenta (RF-19, RN-38): no puede ingresar y sus sesiones se cierran.
    /// </summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken)
    {
        await estudianteService.DesactivarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactiva al estudiante y su cuenta (RF-19, RN-38, RN-39).
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivar(int id, CancellationToken cancellationToken)
    {
        await estudianteService.ReactivarAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Detalle de un estudiante (RF-17).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EstudianteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstudianteResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await estudianteService.ObtenerAsync(id, cancellationToken));
}
