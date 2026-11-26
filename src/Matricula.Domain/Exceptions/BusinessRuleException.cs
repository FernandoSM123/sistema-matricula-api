namespace Matricula.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación incumple una regla de negocio. <see cref="Code"/> referencia la regla (por ejemplo, "RN-05").
/// <see cref="Extensions"/> lleva valores extra para el cliente (por ejemplo, el id del registro inactivo en RN-39).
/// </summary>
public class BusinessRuleException(string code, string message, IReadOnlyDictionary<string, object?>? extensions = null) : Exception(message)
{
    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object?> Extensions { get; } = extensions ?? new Dictionary<string, object?>();
}