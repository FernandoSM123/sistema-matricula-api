using System.Security.Claims;
using Matricula.Api.Seguridad;
using Matricula.Application.Abstractions;

namespace Matricula.Api.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool EstaAutenticado => User?.Identity?.IsAuthenticated ?? false;
    public int? UsuarioId => LeerEntero(ClaimsToken.UsuarioId);
    public string? Rol => User?.FindFirstValue(ClaimsToken.Rol);
    public int? EstudianteId => LeerEntero(ClaimsToken.EstudianteId);
    public int? ProfesorId => LeerEntero(ClaimsToken.ProfesorId);
    public bool DebeCambiarContrasena => User?.HasClaim(c => c.Type == ClaimsToken.DebeCambiarContrasena) ?? false;

    private int? LeerEntero(string claim) =>
        int.TryParse(User?.FindFirstValue(claim), out var valor) ? valor : null;
}