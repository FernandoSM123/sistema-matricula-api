namespace Matricula.Application.Estudiantes;

public record ActualizarEstudianteRequest(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    string Correo,
    DateOnly? FechaNacimiento,
    string? Direccion);
