namespace Matricula.Application.Abstractions;

/// <summary>
/// La fecha de hoy en la zona horaria del instituto. Es el único punto que la calcula; las marcas de tiempo siguen
/// usando TimeProvider en UTC.
/// </summary>
public interface ILocalClock
{
    DateOnly Today();

    /// <summary>
    /// Rango UTC de un rango de fechas locales con los dos extremos incluidos; el fin devuelto es exclusivo. Un extremo
    /// nulo queda abierto.
    /// </summary>
    (DateTimeOffset? Inicio, DateTimeOffset? Fin) ToUtcRange(DateOnly? desde, DateOnly? hasta);
}
