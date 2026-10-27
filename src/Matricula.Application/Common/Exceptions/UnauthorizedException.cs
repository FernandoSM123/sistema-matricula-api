namespace Matricula.Application.Common.Exceptions;

/// <summary>
/// 401: credenciales inválidas. El mensaje es siempre genérico para no revelar qué correos existen.
/// </summary>
public class UnauthorizedException(string message = "Correo o contraseña incorrectos.") : Exception(message);