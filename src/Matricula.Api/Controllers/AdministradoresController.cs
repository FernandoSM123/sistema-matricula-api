using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Application.Administradores;
using Matricula.Application.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Controllers;

[Authorize(Policy = Policies.Administrador)]
public class AdministradoresController(AdministradorService administradorService) : ApiControllerBase
{
    /// <summary>
    /// Registra un administrador con sus datos personales (RF-04). La temporal se envía a su correo y nunca
    /// aparece en la respuesta; con correoEnviado en false se puede reenviar.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AdministradorRegistradoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdministradorRegistradoResponse>> Create(
        RegistrarAdministradorRequest request, CancellationToken cancellationToken)
    {
        var registrado = await administradorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = registrado.Administrador.Id }, registrado);
    }

    /// <summary>
    /// Lista los administradores (RF-55). Por defecto solo los activos; search busca en nombre, apellidos,
    /// cédula y correo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AdministradorResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdministradorResponse>>> List(
        [FromQuery] ListarAdministradoresRequest request, CancellationToken cancellationToken) =>
        Ok(await administradorService.ListAsync(request, cancellationToken));

    /// <summary>
    /// Modifica los datos personales y el correo de un administrador, incluido el propio (RF-55).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<AdministradorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdministradorResponse>> Update(
        int id, ActualizarAdministradorRequest request, CancellationToken cancellationToken) =>
        Ok(await administradorService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Detalle de un administrador (RF-55).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AdministradorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdministradorResponse>> Get(int id, CancellationToken cancellationToken) =>
        Ok(await administradorService.GetAsync(id, cancellationToken));
}