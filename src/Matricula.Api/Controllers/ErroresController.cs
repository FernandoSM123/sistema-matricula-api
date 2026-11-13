using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Solo lectura: no hay endpoints para crear, modificar ni eliminar errores; se depuran solos al vencer (RN-49).
[Authorize(Policy = Politicas.Administrador)]
public class ErroresController(ErroresService erroresService) : ApiControllerBase
{
    /// <summary>
    /// Errores inesperados registrados (RF-59), del más reciente al más antiguo, sin el detalle técnico. Filtros
    /// combinables: desde y hasta (fechas locales del instituto, ambas incluidas), origen (Peticion o Correo) y traceId
    /// (el que recibe el usuario en el error, completo o solo sus 32 caracteres); search busca en el mensaje, el tipo
    /// de la excepción y la ruta. Un usuario null indica que no había sesión.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<RegistroErrorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<RegistroErrorResponse>>> Listar(
        [FromQuery] ListarErroresRequest request, CancellationToken cancellationToken) =>
        Ok(await erroresService.ListarAsync(request, cancellationToken));

    /// <summary>
    /// Un error con su detalle técnico (RF-59): la excepción completa, con las internas y el stack trace. Un error ya
    /// depurado responde 404.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<RegistroErrorDetalleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistroErrorDetalleResponse>> Obtener(long id, CancellationToken cancellationToken) =>
        Ok(await erroresService.ObtenerAsync(id, cancellationToken));
}
