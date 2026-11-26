using Matricula.Domain.Enums;

namespace Matricula.Application.Auth;

/// <summary>
/// RF-13. <see cref="PersonaId"/> y <see cref="DatosPersonales"/> vienen de la persona asociada a la cuenta:
/// administrador, estudiante o profesor. PersonaId es el id que piden sus endpoints, por ejemplo
/// PUT /administradores/{id} (COR-01, B-02).
/// </summary>
public record ProfileResponse(
    int UsuarioId,
    int? PersonaId,
    string Correo,
    Rol Rol,
    bool Activo,
    DateTimeOffset FechaCreacion,
    DateTimeOffset? UltimoIngreso,
    DateTimeOffset? FechaCambioContrasena,
    DatosPersonalesResponse? DatosPersonales);