namespace Matricula.Application.Administradores;

public record RegistrarAdministradorRequest(string Cedula, string Nombre, string Apellidos, string Telefono, string Correo);