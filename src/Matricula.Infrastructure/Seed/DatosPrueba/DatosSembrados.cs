using Matricula.Domain.Entities;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Lo que cada capa de la siembra deja para las siguientes. Después de cada guardado, las entidades ya tienen su Id.
/// </summary>
public sealed class DatosSembrados
{
    public List<Curso> Cursos { get; } = [];
    public List<Aula> Aulas { get; } = [];
    public required Periodo Pasado { get; init; }
    public required Periodo Vigente { get; init; }
    public required Periodo Futuro { get; init; }
    public List<Profesor> Profesores { get; } = [];
    public List<Estudiante> Estudiantes { get; } = [];

    /// <summary>Profesor desactivado: no recibe grupos en los periodos vigente y futuro (punto 6 de la spec).</summary>
    public Profesor? ProfesorDesactivado { get; set; }

    /// <summary>Estudiantes desactivados: no reciben matrículas activas en el periodo vigente.</summary>
    public HashSet<Estudiante> EstudiantesDesactivados { get; } = [];
}
