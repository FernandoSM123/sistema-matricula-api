namespace Matricula.Application.Profesores;

public record ActualizarProfesorRequest(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    string Correo,
    string? Especialidad,
    DateOnly? FechaContratacion);
