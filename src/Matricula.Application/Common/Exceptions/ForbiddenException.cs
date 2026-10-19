namespace Matricula.Application.Common.Exceptions;

/// <summary>
/// 403. <see cref="Code"/> referencia la regla cuando la hay (por ejemplo, "RN-29").
/// </summary>
public class ForbiddenException(string message = "No tiene permiso para realizar esta operación.", string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}