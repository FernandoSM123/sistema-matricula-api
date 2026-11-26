using Matricula.Domain.Entities;

namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Lo que cada capa de la siembra deja para las siguientes. Después de cada guardado, las entidades ya tienen su Id.
/// </summary>
public sealed class SeededData
{
    public List<Curso> Cursos { get; } = [];
    public List<Aula> Aulas { get; } = [];
    public required Periodo Pasado { get; init; }
    public required Periodo Vigente { get; init; }
    public required Periodo Futuro { get; init; }
    public List<Profesor> Profesores { get; } = [];
    public List<Estudiante> Estudiantes { get; } = [];

    /// <summary>Profesor desactivado: no recibe grupos en los periodos vigente y futuro (punto 6 de la spec).</summary>
    public Profesor? DeactivatedProfesor { get; set; }

    /// <summary>Estudiantes desactivados: no reciben matrículas activas en el periodo vigente.</summary>
    public HashSet<Estudiante> DeactivatedEstudiantes { get; } = [];

    public List<GrupoSeed> Grupos { get; } = [];

    /// <summary>
    /// Un planificador por periodo. Lo que ocupan los grupos queda registrado, y la capa de matrículas lo reutiliza para
    /// los estudiantes (RN-05, RN-06).
    /// </summary>
    public Dictionary<Periodo, HorarioPlanner> Planners { get; } = [];

    /// <summary>Grupos que no se pudieron crear por falta de franjas libres.</summary>
    public int GruposWithoutRoom { get; set; }

    public List<MatriculaSeed> Matriculas { get; } = [];

    /// <summary>Si se hizo el cambio de grupo del periodo vigente (punto 5 de la spec).</summary>
    public bool HasGrupoChange { get; set; }
}
