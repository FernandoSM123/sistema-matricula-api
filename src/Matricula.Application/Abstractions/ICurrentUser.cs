namespace Matricula.Application.Abstractions;

/// <summary>
/// Datos del usuario autenticado en la petición actual, leídos de los claims del token.
/// </summary>
public interface ICurrentUser
{
    int? UsuarioId { get; }
    string? Rol { get; }
    int? EstudianteId { get; }
    int? ProfesorId { get; }
    bool EstaAutenticado { get; }
}
