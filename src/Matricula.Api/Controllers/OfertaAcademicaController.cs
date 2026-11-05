using Matricula.Api.Common;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Grupos.Consultas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

public class OfertaAcademicaController(OfertaAcademicaService ofertaService) : ApiControllerBase
{
    /// <summary>
    /// Oferta académica pública (RF-50): los grupos Abierto del periodo vigente con sus horarios y cupos disponibles,
    /// incluidos los llenos. Filtros por curso y modalidad; search busca en el curso y el NRC. Sin periodo vigente
    /// responde la lista vacía.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<OfertaAcademicaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<OfertaAcademicaResponse>>> Listar(
        [FromQuery] ListarOfertaRequest request, CancellationToken cancellationToken) =>
        Ok(await ofertaService.ListarAsync(request, cancellationToken));
}
