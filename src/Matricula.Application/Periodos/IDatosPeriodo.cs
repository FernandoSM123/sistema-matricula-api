namespace Matricula.Application.Periodos;

/// <summary>
/// Datos que validan el registro y la edición. Las fechas son anulables porque una fecha que falta llegaría como
/// 0001-01-01, que puede pasar RN-16 sin error.
/// </summary>
public interface IDatosPeriodo
{
    string Nombre { get; }
    DateOnly? InicioClases { get; }
    DateOnly? FinClases { get; }
    DateOnly? InicioMatricula { get; }
    DateOnly? FinMatricula { get; }
    DateOnly? FechaLimiteRetiro { get; }
}
