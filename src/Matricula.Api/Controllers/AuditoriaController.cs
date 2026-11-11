using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Application.Auditoria;
using Matricula.Application.Common.Paginacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

// Solo lectura: no hay endpoints para crear, modificar ni eliminar registros de auditoría (RN-43).
[Authorize(Policy = Politicas.Administrador)]
public class AuditoriaController(AuditoriaService auditoriaService) : ApiControllerBase
{
    /// <summary>
    /// Cambios de datos registrados (RF-57), del más reciente al más antiguo. Filtros combinables: entidad (por
    /// ejemplo, Matricula), entidadId (exige entidad), usuarioId, accion, desde y hasta (fechas locales del instituto,
    /// ambas incluidas) y operacionId; search busca en el correo del autor. Un autor null es el Sistema.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<RegistroAuditoriaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<RegistroAuditoriaResponse>>> Listar(
        [FromQuery] ListarAuditoriaRequest request, CancellationToken cancellationToken) =>
        Ok(await auditoriaService.ListarAsync(request, cancellationToken));
}