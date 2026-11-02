namespace Matricula.Application.Profesores;

/// <summary>
/// Con <see cref="UsuarioId"/> se reenvía la temporal y se activa o desactiva la cuenta (/api/v1/usuarios/{id}/...).
/// </summary>
public record ProfesorResponse(
    int Id,
    int UsuarioId,
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    string Correo,
    string? Especialidad,
    DateOnly? FechaContratacion,
    bool Activo,
    DateTimeOffset FechaCreacion,
    DateTimeOffset? UltimoIngreso);
