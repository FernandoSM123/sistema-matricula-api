namespace Matricula.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación incumple una regla de negocio. <see cref="Code"/> referencia la regla (por ejemplo, "RN-05").
/// </summary>
public class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
