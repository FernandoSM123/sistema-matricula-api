namespace Matricula.Application.Estudiantes;

public record RegistrarEstudianteRequest(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    string Correo,
    DateOnly? FechaNacimiento,
    string? Direccion);
