using System.Security.Claims;
using Matricula.Api.Security;
using Matricula.Application.Abstractions;

namespace Matricula.Api.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public int? UsuarioId => ReadInt(TokenClaims.UsuarioId);
    public string? Rol => User?.FindFirstValue(TokenClaims.Rol);
    public int? EstudianteId => ReadInt(TokenClaims.EstudianteId);
    public int? ProfesorId => ReadInt(TokenClaims.ProfesorId);
    public bool DebeCambiarContrasena => User?.HasClaim(c => c.Type == TokenClaims.DebeCambiarContrasena) ?? false;

    private int? ReadInt(string claim) =>
        int.TryParse(User?.FindFirstValue(claim), out var valor) ? valor : null;
}