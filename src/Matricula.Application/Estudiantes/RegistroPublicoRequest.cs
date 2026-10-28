namespace Matricula.Application.Estudiantes;

/// <summary>
/// RF-01. No tiene rol: el registro público siempre crea cuentas de estudiante (RN-19).
/// </summary>
public record RegistroPublicoRequest(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    string Correo,
    DateOnly? FechaNacimiento,
    string? Direccion,
    string Contrasena,
    string Confirmacion);
