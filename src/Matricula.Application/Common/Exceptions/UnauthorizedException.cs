namespace Matricula.Application.Common.Exceptions;

/// <summary>
/// 401: credenciales inválidas o sesión terminada. El mensaje es siempre genérico para no revelar qué correos existen
/// ni por qué terminó la sesión. <see cref="Code"/> referencia la regla cuando la hay (por ejemplo, "RF-60").
/// </summary>
public class UnauthorizedException(string message = "Correo o contraseña incorrectos.", string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
