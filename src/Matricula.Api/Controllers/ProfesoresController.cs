using Matricula.Api.Common;
using Matricula.Api.Seguridad;
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
    /// Detalle de un profesor (RF-24).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProfesorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfesorResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await profesorService.ObtenerAsync(id, cancellationToken));
}
