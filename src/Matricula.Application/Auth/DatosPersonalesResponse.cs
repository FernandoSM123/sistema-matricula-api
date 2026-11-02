namespace Matricula.Application.Auth;

/// <summary>
/// <see cref="FechaNacimiento"/>, <see cref="Direccion"/> y <see cref="FechaRegistro"/> solo los tienen los
/// estudiantes, y <see cref="Especialidad"/> y <see cref="FechaContratacion"/> los profesores; para los demás
/// roles van en null.
/// </summary>
public record DatosPersonalesResponse(
    string Cedula,
    string Nombre,
    string Apellidos,
    string Telefono,
    DateOnly? FechaNacimiento,
    string? Direccion,
    DateOnly? FechaRegistro,
    string? Especialidad,
    DateOnly? FechaContratacion);
