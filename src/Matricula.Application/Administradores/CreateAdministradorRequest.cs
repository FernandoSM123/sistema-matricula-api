namespace Matricula.Application.Administradores;

public record CreateAdministradorRequest(string Cedula, string Nombre, string Apellidos, string Telefono, string Correo);