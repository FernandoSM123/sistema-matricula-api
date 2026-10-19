using Matricula.Domain.Enums;

namespace Matricula.Application.Auth;

/// <summary>
/// Respuesta de un ingreso. Con <see cref="DebeCambiarContrasena"/> el token solo sirve para el cambio obligatorio y el logout (RN-24).
/// </summary>
public record TokenResponse(string AccessToken, DateTimeOffset ExpiraEn, Rol Rol, bool DebeCambiarContrasena);