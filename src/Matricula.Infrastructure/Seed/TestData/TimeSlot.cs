using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Bloque fijo de 2 horas en que se siembran los horarios (supuesto 7 de la spec). Como las franjas no se cruzan entre
/// sí, dos horarios se traslapan (RN-14) solo si caen en la misma franja.
/// </summary>
public sealed record TimeSlot(DiaSemana Dia, TimeOnly HoraInicio, TimeOnly HoraFin)
{
    private static readonly (int Inicio, int Fin)[] WeekdayHours = [(8, 10), (10, 12), (13, 15), (15, 17), (18, 20), (20, 22)];

    private static readonly (int Inicio, int Fin)[] SaturdayHours = [(8, 10), (10, 12)];

    /// <summary>Lunes a viernes, seis franjas por día; sábado, dos.</summary>
    public static IReadOnlyList<TimeSlot> All { get; } = Create();

    private static TimeSlot[] Create()
    {
        DiaSemana[] diasSemana = [DiaSemana.Lunes, DiaSemana.Martes, DiaSemana.Miercoles, DiaSemana.Jueves, DiaSemana.Viernes];

        return
        [
            .. diasSemana.SelectMany(dia => WeekdayHours.Select(h => Create(dia, h))),
            .. SaturdayHours.Select(h => Create(DiaSemana.Sabado, h)),
        ];
    }

    private static TimeSlot Create(DiaSemana dia, (int Inicio, int Fin) horas) =>
        new(dia, new TimeOnly(horas.Inicio, 0), new TimeOnly(horas.Fin, 0));
}
