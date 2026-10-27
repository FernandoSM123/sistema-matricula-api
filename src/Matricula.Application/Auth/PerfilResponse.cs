using Matricula.Domain.Enums;

namespace Matricula.Application.Auth;

/// <summary>
/// RF-13. <see cref="DatosPersonales"/> viene de la persona asociada a la cuenta; por ahora solo los
/// administradores, los módulos estudiantes y profesores lo completan para sus roles.
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