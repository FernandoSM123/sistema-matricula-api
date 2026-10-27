using Matricula.Api.Common;
using Matricula.Api.Seguridad;
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
    /// Detalle de un estudiante (RF-17).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EstudianteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstudianteResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await estudianteService.ObtenerAsync(id, cancellationToken));
}
