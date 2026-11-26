namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Reparte las franjas de un periodo sin traslapes (punto 7 de la spec): por profesor (RN-07), por aula (RN-08) y por
/// estudiante (RN-06), sin repetir curso por estudiante (RN-05). Trabaja con Ids, sin EF, para probarse sin BD. Las
/// elecciones al azar salen del <see cref="Random"/> recibido, así una misma semilla da la misma planificación.
/// </summary>
public sealed class HorarioPlanner(Random random)
{
    public const int MaxBloques = 2;

    private readonly Dictionary<int, HashSet<TimeSlot>> ocupacionProfesores = [];
    private readonly Dictionary<int, HashSet<TimeSlot>> ocupacionAulas = [];
    private readonly Dictionary<int, HashSet<TimeSlot>> ocupacionEstudiantes = [];
    private readonly Dictionary<int, HashSet<int>> cursosEstudiantes = [];

    /// <summary>
    /// Elige franjas en días distintos y un profesor y un aula libres en todas ellas, y las marca como ocupadas. Recorre
    /// todas las combinaciones en orden aleatorio, así que devuelve <c>null</c> solo si de verdad no queda ninguna.
    /// </summary>
    /// <param name="aulas"><c>null</c> para un grupo virtual, que no ocupa aula.</param>
    public GrupoAssignment? TryAssignGrupo(int cantidadBloques, IReadOnlyList<int> profesores, IReadOnlyList<int>? aulas)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cantidadBloques, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cantidadBloques, MaxBloques);
        ArgumentNullException.ThrowIfNull(profesores);

        var profesoresAlAzar = Shuffle(profesores);
        var aulasAlAzar = aulas is null ? null : Shuffle(aulas);

        foreach (var franjas in Shuffle(Combinations(cantidadBloques)))
        {
            var profesor = profesoresAlAzar.FirstOrDefault(p => IsFree(ocupacionProfesores, p, franjas), -1);
            if (profesor == -1)
            {
                continue;
            }

            int? aula = null;
            if (aulasAlAzar is not null)
            {
                aula = aulasAlAzar.FirstOrDefault(a => IsFree(ocupacionAulas, a, franjas), -1);
                if (aula == -1)
                {
                    continue;
                }

                Take(ocupacionAulas, aula.Value, franjas);
            }

            Take(ocupacionProfesores, profesor, franjas);
            return new GrupoAssignment(profesor, aula, franjas);
        }

        return null;
    }

    /// <summary>RN-05 (otro curso) y RN-06 (sin franjas compartidas) para el estudiante en este periodo.</summary>
    public bool CanEnroll(int estudianteId, int cursoId, IReadOnlyCollection<TimeSlot> franjas)
    {
        ArgumentNullException.ThrowIfNull(franjas);

        return !(cursosEstudiantes.TryGetValue(estudianteId, out var cursos) && cursos.Contains(cursoId))
            && IsFree(ocupacionEstudiantes, estudianteId, franjas);
    }

    public void RecordMatricula(int estudianteId, int cursoId, IReadOnlyCollection<TimeSlot> franjas)
    {
        if (!CanEnroll(estudianteId, cursoId, franjas))
        {
            throw new InvalidOperationException(
                $"El estudiante {estudianteId} ya tiene el curso {cursoId} o una de sus franjas en este periodo.");
        }

        Take(ocupacionEstudiantes, estudianteId, franjas);
        GetOrAdd(cursosEstudiantes, estudianteId).Add(cursoId);
    }

    /// <summary>Libera el curso y las franjas de una matrícula retirada (retiro, cambio de grupo o cancelación).</summary>
    public void ReleaseMatricula(int estudianteId, int cursoId, IReadOnlyCollection<TimeSlot> franjas)
    {
        ArgumentNullException.ThrowIfNull(franjas);

        if (ocupacionEstudiantes.TryGetValue(estudianteId, out var ocupadas))
        {
            ocupadas.ExceptWith(franjas);
        }

        if (cursosEstudiantes.TryGetValue(estudianteId, out var cursos))
        {
            cursos.Remove(cursoId);
        }
    }

    /// <summary>Todas las combinaciones de franjas en días distintos, en un orden fijo antes de barajarlas.</summary>
    private static List<TimeSlot[]> Combinations(int cantidadBloques)
    {
        var todas = TimeSlot.All;
        if (cantidadBloques == 1)
        {
            return [.. todas.Select(f => new[] { f })];
        }

        var combinaciones = new List<TimeSlot[]>();
        for (var i = 0; i < todas.Count; i++)
        {
            for (var j = i + 1; j < todas.Count; j++)
            {
                if (todas[i].Dia != todas[j].Dia)
                {
                    combinaciones.Add([todas[i], todas[j]]);
                }
            }
        }

        return combinaciones;
    }

    private T[] Shuffle<T>(IEnumerable<T> elementos)
    {
        T[] copia = [.. elementos];
        random.Shuffle(copia);
        return copia;
    }

    private static bool IsFree(Dictionary<int, HashSet<TimeSlot>> ocupacion, int id, IEnumerable<TimeSlot> franjas) =>
        !ocupacion.TryGetValue(id, out var ocupadas) || !ocupadas.Overlaps(franjas);

    private static void Take(Dictionary<int, HashSet<TimeSlot>> ocupacion, int id, IEnumerable<TimeSlot> franjas) =>
        GetOrAdd(ocupacion, id).UnionWith(franjas);

    private static HashSet<TValor> GetOrAdd<TValor>(Dictionary<int, HashSet<TValor>> diccionario, int id)
    {
        if (!diccionario.TryGetValue(id, out var conjunto))
        {
            conjunto = [];
            diccionario[id] = conjunto;
        }

        return conjunto;
    }
}
