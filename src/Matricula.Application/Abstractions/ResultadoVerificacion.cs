namespace Matricula.Application.Abstractions;

public enum ResultadoVerificacion
{
    Incorrecta,
    Correcta,

    /// <summary>
    /// La contraseña coincide, pero el hash usa parámetros antiguos y conviene recalcularlo.
    /// </summary>
    CorrectaRequiereRehash
}