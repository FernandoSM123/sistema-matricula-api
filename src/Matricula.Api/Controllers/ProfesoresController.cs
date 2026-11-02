using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Profesores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

[Authorize(Policy = Politicas.Administrador)]
public class ProfesoresController(ProfesorService profesorService) : ApiControllerBase
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
    /// Detalle de un profesor (RF-24).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProfesorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfesorResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await profesorService.ObtenerAsync(id, cancellationToken));
}
