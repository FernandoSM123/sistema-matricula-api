namespace Matricula.Domain.Enums;

/// <summary>
/// Estados de una matrícula (§5.10). Una matrícula retirada no vuelve a activarse: se crea otra.
/// </summary>
public enum EstadoMatricula
{
    Activa,
    Retirada
}
