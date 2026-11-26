namespace Matricula.Domain.Rules;

/// <summary>
/// Una propiedad escalar de una entidad modificada, con su valor original y su valor actual (ver
/// <see cref="AuditRules.Differences"/>).
/// </summary>
public sealed record AuditedProperty(string Nombre, object? Antes, object? Despues);