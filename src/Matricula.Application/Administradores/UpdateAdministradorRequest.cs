namespace Matricula.Application.Administradores;

public record UpdateAdministradorRequest(string Cedula, string Nombre, string Apellidos, string Telefono, string Correo);