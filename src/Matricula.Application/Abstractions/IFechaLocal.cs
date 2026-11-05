namespace Matricula.Application.Abstractions;

/// <summary>
/// La fecha de hoy en la zona horaria del instituto. Es el único punto que la calcula; las marcas de tiempo siguen
/// usando TimeProvider en UTC.
/// </summary>
public interface IFechaLocal
{
    DateOnly Hoy();
}
