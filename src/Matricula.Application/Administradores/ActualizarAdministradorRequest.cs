namespace Matricula.Application.Administradores;

public record ActualizarAdministradorRequest(string Cedula, string Nombre, string Apellidos, string Telefono, string Correo);