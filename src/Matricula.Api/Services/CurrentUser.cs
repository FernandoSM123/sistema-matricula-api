using System.Security.Claims;
using Matricula.Application.Abstractions;

namespace Matricula.Api.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // Nombres de los claims propios que se incluirán en el JWT.
    public const string EstudianteIdClaim = "estudiante_id";
    public const string ProfesorIdClaim = "profesor_id";

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool EstaAutenticado => User?.Identity?.IsAuthenticated ?? false;
    public int? UsuarioId => LeerEntero(ClaimTypes.NameIdentifier);
    public string? Rol => User?.FindFirstValue(ClaimTypes.Role);
    public int? EstudianteId => LeerEntero(EstudianteIdClaim);
    public int? ProfesorId => LeerEntero(ProfesorIdClaim);

    private int? LeerEntero(string claim) =>
        int.TryParse(User?.FindFirstValue(claim), out var valor) ? valor : null;
}
