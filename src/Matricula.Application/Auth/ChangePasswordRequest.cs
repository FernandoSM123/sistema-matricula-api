namespace Matricula.Application.Auth;

public record ChangePasswordRequest(string ContrasenaActual, string NuevaContrasena, string Confirmacion);