namespace Matricula.Application.Abstractions;

public enum PasswordCheckResult
{
    Failed,
    Success,

    /// <summary>
    /// La contraseña coincide, pero el hash usa parámetros antiguos y conviene recalcularlo.
    /// </summary>
    SuccessRehashNeeded
}