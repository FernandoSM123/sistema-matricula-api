namespace Matricula.Domain.Enums;

/// <summary>
/// Estados de un grupo (RF-47). El flujo normal es Planificado → Abierto → Cerrado → EnCurso → Finalizado.
/// </summary>
public enum EstadoGrupo
{
    Planificado,
    Abierto,
    Cerrado,
    EnCurso,
    Finalizado,
    Cancelado
}
