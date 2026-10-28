namespace Matricula.Application.Auth;

/// <summary>
/// <see cref="FechaNacimiento"/>, <see cref="Direccion"/> y <see cref="FechaRegistro"/> solo los tienen los
/// estudiantes; para los administradores van en null.
/// </summary>
public record DatosPersonalesResponse(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    DateOnly? FechaNacimiento,
    string? Direccion,
    DateOnly? FechaRegistro);
