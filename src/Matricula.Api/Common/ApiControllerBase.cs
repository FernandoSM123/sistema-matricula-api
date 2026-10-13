using Microsoft.AspNetCore.Mvc;

namespace Matricula.Api.Common;

/// <summary>
/// Base de todos los controllers: prefijo /api/v1 y comportamiento de [ApiController].
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase;
