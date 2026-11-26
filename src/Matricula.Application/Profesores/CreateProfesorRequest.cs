namespace Matricula.Application.Profesores;

public record CreateProfesorRequest(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    string Correo,
    string? Especialidad,
    DateOnly? FechaContratacion);
