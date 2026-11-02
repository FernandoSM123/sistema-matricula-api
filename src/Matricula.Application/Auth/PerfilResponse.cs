using Matricula.Domain.Enums;

namespace Matricula.Application.Auth;

/// <summary>
/// RF-13. <see cref="DatosPersonales"/> viene de la persona asociada a la cuenta: administrador, estudiante o
/// profesor.
/// </summary>
public record PerfilResponse(
    int UsuarioId,
    string Correo,
    Rol Rol,
    bool Activo,
    DateTimeOffset FechaCreacion,
    DateTimeOffset? UltimoIngreso,
    DateTimeOffset? FechaCambioContrasena,
    DatosPersonalesResponse? DatosPersonales);