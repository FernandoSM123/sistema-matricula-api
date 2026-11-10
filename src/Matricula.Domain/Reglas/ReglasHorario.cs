using Matricula.Domain.Enums;

namespace Matricula.Domain.Reglas;

/// <summary>
/// Reglas de los bloques de horario. Las usan el validador de entrada y la entidad <c>Horario</c>.
/// </summary>
public static class ReglasHorario
{
    public const string MensajeHorasValidas = "La hora de fin debe ser posterior a la hora de inicio.";

    /// <summary>RN-15: la hora de fin es posterior a la de inicio.</summary>
    public static bool HorasValidas(TimeOnly horaInicio, TimeOnly horaFin) => horaFin > horaInicio;

    /// <summary>
    /// RN-14: mismo día y uno empieza antes de que termine el otro. Los bloques contiguos (uno termina a la hora en
    /// que empieza el otro) no se traslapan.
    /// </summary>
    public static bool SeTraslapan(
        DiaSemana diaA,
        TimeOnly inicioA,
        TimeOnly finA,
        DiaSemana diaB,
        TimeOnly inicioB,
        TimeOnly finB) =>
        diaA == diaB && inicioA < finB && inicioB < finA;
}
