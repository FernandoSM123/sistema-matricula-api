using Matricula.Domain.Common;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Matricula.Domain.Rules;

namespace Matricula.Domain.Entities;

/// <summary>
/// Bloque semanal de un grupo: un día con su hora de inicio y de fin. Se crea con <see cref="Grupo.AddHorario"/>.
/// </summary>
public class Horario : EntidadBase
{
    // Para EF Core.
    private Horario()
    {
    }

    public Horario(DiaSemana dia, TimeOnly horaInicio, TimeOnly horaFin)
    {
        // El validador ya responde 400; aquí RN-15 se comprueba otra vez como invariante.
        if (!HorarioRules.AreValidHours(horaInicio, horaFin))
        {
            throw new BusinessRuleException("RN-15", HorarioRules.ValidHoursMessage);
        }

        Dia = dia;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
    }

    public int GrupoId { get; private set; }
    public DiaSemana Dia { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFin { get; private set; }

    /// <summary>RN-14.</summary>
    public bool OverlapsWith(Horario otro)
    {
        ArgumentNullException.ThrowIfNull(otro);
        return HorarioRules.Overlap(Dia, HoraInicio, HoraFin, otro.Dia, otro.HoraInicio, otro.HoraFin);
    }
}
