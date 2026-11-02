using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Cursos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Las políticas van por acción: un atributo de clase se sumaría al de cada una y dejaría las lecturas solo para
// administradores.
public class CursosController(CursoService cursoService) : ApiControllerBase
{
    /// <summary>
    /// Registra un curso (RF-29). El código de un curso activo responde RN-13 y el de uno inactivo RN-39 con cursoId.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Administrador)]
    [ProducesResponseType<CursoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CursoResponse>> Registrar(
        RegistrarCursoRequest request, CancellationToken cancellationToken)
    {
        var curso = await cursoService.RegistrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = curso.Id }, curso);
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
    public async Task<ActionResult<CursoResponse>> Obtener(int id, CancellationToken cancellationToken) =>
        Ok(await cursoService.ObtenerAsync(id, cancellationToken));
}
