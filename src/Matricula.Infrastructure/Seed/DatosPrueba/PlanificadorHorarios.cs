namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Reparte las franjas de un periodo sin traslapes (punto 7 de la spec): por profesor (RN-07), por aula (RN-08) y por
/// estudiante (RN-06), sin repetir curso por estudiante (RN-05). Trabaja con Ids, sin EF, para probarse sin BD. Las
/// elecciones al azar salen del <see cref="Random"/> recibido, así una misma semilla da la misma planificación.
/// </summary>
public sealed class PlanificadorHorarios(Random random)
{
    public const int MaxBloques = 2;

    private readonly Dictionary<int, HashSet<Franja>> ocupacionProfesores = [];
    private readonly Dictionary<int, HashSet<Franja>> ocupacionAulas = [];
    private readonly Dictionary<int, HashSet<Franja>> ocupacionEstudiantes = [];
    private readonly Dictionary<int, HashSet<int>> cursosEstudiantes = [];

    /// <summary>
    /// Elige franjas en días distintos y un profesor y un aula libres en todas ellas, y las marca como ocupadas. Recorre
    /// todas las combinaciones en orden aleatorio, así que devuelve <c>null</c> solo si de verdad no queda ninguna.
    /// </summary>
    /// <param name="aulas"><c>null</c> para un grupo virtual, que no ocupa aula.</param>
    public AsignacionGrupo? IntentarAsignarGrupo(int cantidadBloques, IReadOnlyList<int> profesores, IReadOnlyList<int>? aulas)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cantidadBloques, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cantidadBloques, MaxBloques);
        ArgumentNullException.ThrowIfNull(profesores);

        var profesoresAlAzar = Barajar(profesores);
        var aulasAlAzar = aulas is null ? null : Barajar(aulas);

        foreach (var franjas in Barajar(Combinaciones(cantidadBloques)))
        {
            var profesor = profesoresAlAzar.FirstOrDefault(p => EstaLibre(ocupacionProfesores, p, franjas), -1);
            if (profesor == -1)
            {
                continue;
            }

            int? aula = null;
            if (aulasAlAzar is not null)
            {
                aula = aulasAlAzar.FirstOrDefault(a => EstaLibre(ocupacionAulas, a, franjas), -1);
                if (aula == -1)
                {
                    continue;
                }

                Ocupar(ocupacionAulas, aula.Value, franjas);
            }

            Ocupar(ocupacionProfesores, profesor, franjas);
            return new AsignacionGrupo(profesor, aula, franjas);
        }

        return null;
    }

    /// <summary>RN-05 (otro curso) y RN-06 (sin franjas compartidas) para el estudiante en este periodo.</summary>
    public bool PuedeMatricular(int estudianteId, int cursoId, IReadOnlyCollection<Franja> franjas)
    {
        ArgumentNullException.ThrowIfNull(franjas);

        return !(cursosEstudiantes.TryGetValue(estudianteId, out var cursos) && cursos.Contains(cursoId))
            && EstaLibre(ocupacionEstudiantes, estudianteId, franjas);
    }

    public void RegistrarMatricula(int estudianteId, int cursoId, IReadOnlyCollection<Franja> franjas)
    {
        if (!PuedeMatricular(estudianteId, cursoId, franjas))
        {
            throw new InvalidOperationException(
                $"El estudiante {estudianteId} ya tiene el curso {cursoId} o una de sus franjas en este periodo.");
        }

        Ocupar(ocupacionEstudiantes, estudianteId, franjas);
        Obtener(cursosEstudiantes, estudianteId).Add(cursoId);
    }

    /// <summary>Libera el curso y las franjas de una matrícula retirada (retiro, cambio de grupo o cancelación).</summary>
    public void LiberarMatricula(int estudianteId, int cursoId, IReadOnlyCollection<Franja> franjas)
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
    private static List<Franja[]> Combinaciones(int cantidadBloques)
    {
        var todas = Franja.Todas;
        if (cantidadBloques == 1)
        {
            return [.. todas.Select(f => new[] { f })];
        }

        var combinaciones = new List<Franja[]>();
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

    private T[] Barajar<T>(IEnumerable<T> elementos)
    {
        T[] copia = [.. elementos];
        random.Shuffle(copia);
        return copia;
    }

    private static bool EstaLibre(Dictionary<int, HashSet<Franja>> ocupacion, int id, IEnumerable<Franja> franjas) =>
        !ocupacion.TryGetValue(id, out var ocupadas) || !ocupadas.Overlaps(franjas);

    private static void Ocupar(Dictionary<int, HashSet<Franja>> ocupacion, int id, IEnumerable<Franja> franjas) =>
        Obtener(ocupacion, id).UnionWith(franjas);

    private static HashSet<TValor> Obtener<TValor>(Dictionary<int, HashSet<TValor>> diccionario, int id)
    {
        if (!diccionario.TryGetValue(id, out var conjunto))
        {
            conjunto = [];
            diccionario[id] = conjunto;
        }

        return conjunto;
    }
}
