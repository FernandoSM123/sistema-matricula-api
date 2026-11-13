namespace Matricula.Domain.Reglas;

/// <summary>
/// Una propiedad escalar de una entidad modificada, con su valor original y su valor actual (ver
/// <see cref="ReglasAuditoria.Diferencias"/>).
/// </summary>
public sealed record PropiedadAuditada(string Nombre, object? Antes, object? Despues);