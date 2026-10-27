namespace Matricula.Application.Auth;

public record CambiarContrasenaRequest(string ContrasenaActual, string NuevaContrasena, string Confirmacion);