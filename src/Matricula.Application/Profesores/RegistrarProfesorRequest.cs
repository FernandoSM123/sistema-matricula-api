namespace Matricula.Application.Profesores;

public record RegistrarProfesorRequest(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    string Correo,
    string? Especialidad,
    DateOnly? FechaContratacion);
