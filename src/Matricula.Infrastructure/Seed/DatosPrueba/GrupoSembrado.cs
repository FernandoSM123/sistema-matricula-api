using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Un grupo sembrado con sus franjas y el estado en que debe terminar. T4 lo crea y lo abre si recibirá matrículas;
/// la capa de matrículas lo lleva a su estado final.
/// </summary>
public sealed class GrupoSembrado(Grupo grupo, Periodo periodo, IReadOnlyList<Franja> franjas, EstadoGrupo estadoFinal, bool lleno)
{
    public Grupo Grupo { get; } = grupo;
    public Periodo Periodo { get; } = periodo;
    public IReadOnlyList<Franja> Franjas { get; } = franjas;
    public EstadoGrupo EstadoFinal { get; } = estadoFinal;

    /// <summary>El grupo del periodo vigente que se llena hasta el cupo (punto 5 de la spec).</summary>
    public bool Lleno { get; } = lleno;

    /// <summary>Recibe matrículas: todos salvo los que terminan Planificado.</summary>
    public bool RecibeMatriculas => EstadoFinal != EstadoGrupo.Planificado;
}
