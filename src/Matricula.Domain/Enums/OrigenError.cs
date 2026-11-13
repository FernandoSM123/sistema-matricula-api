namespace Matricula.Domain.Enums;

/// <summary>
/// Dónde ocurrió un error inesperado (RF-58).
/// </summary>
public enum OrigenError
{
    /// <summary>Una excepción no controlada que terminó en un 500.</summary>
    Peticion,

    /// <summary>Un correo que no se pudo enviar.</summary>
    Correo
}
