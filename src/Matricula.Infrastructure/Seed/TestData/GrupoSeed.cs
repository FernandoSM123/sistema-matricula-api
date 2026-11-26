using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Un grupo sembrado con sus timeSlots y el estado en que debe terminar. T4 lo crea y lo abre si recibirá matrículas;
/// la capa de matrículas lo lleva a su estado final.
/// </summary>
public sealed class GrupoSeed(Grupo grupo, Periodo periodo, IReadOnlyList<TimeSlot> timeSlots, EstadoGrupo finalEstado, bool isFull)
{
    public Grupo Grupo { get; } = grupo;
    public Periodo Periodo { get; } = periodo;
    public IReadOnlyList<TimeSlot> TimeSlots { get; } = timeSlots;
    public EstadoGrupo FinalEstado { get; } = finalEstado;

    /// <summary>El grupo del periodo vigente que se llena hasta el cupo (punto 5 de la spec).</summary>
    public bool IsFull { get; } = isFull;

    /// <summary>Recibe matrículas: todos salvo los que terminan Planificado.</summary>
    public bool AcceptsMatriculas => FinalEstado != EstadoGrupo.Planificado;
}
