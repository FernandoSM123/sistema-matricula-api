using System.Globalization;
using Bogus;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Grupos con sus horarios para los tres periodos (punto 5 de la spec), sin traslapes de profesor ni de aula (punto 7).
/// Solo arma las entidades en memoria; el servicio las agrega y las guarda.
/// </summary>
public sealed class SembradorGrupos(Faker faker, DatosSembrados datos, Action<string> alFaltarEspacio)
{
    private const double ProporcionVirtuales = 0.2;

    private readonly HashSet<string> nrcs = [];

    public void Sembrar(int gruposPorPeriodo)
    {
        var todos = datos.Profesores.Select(p => p.Id).ToList();
        var activos = datos.Profesores.Where(p => p != datos.ProfesorDesactivado).Select(p => p.Id).ToList();

        // RN-37: el profesor desactivado no puede tener grupos activos, así que solo enseña en el periodo pasado.
        SembrarPeriodo(datos.Pasado, gruposPorPeriodo, todos, EstadosPasado);
        SembrarPeriodo(datos.Vigente, gruposPorPeriodo, activos, EstadosVigente);
        SembrarPeriodo(datos.Futuro, gruposPorPeriodo, activos, EstadosFuturo);
    }

    /// <summary>
    /// Todos finalizados; uno cancelado si hay 3 o más grupos. Es el tercero creado, así existe aunque falten franjas.
    /// </summary>
    private static (EstadoGrupo Estado, bool Lleno) EstadosPasado(int indice, int total) =>
        (indice == 2 ? EstadoGrupo.Cancelado : EstadoGrupo.Finalizado, false);

    /// <summary>Prioridad de la spec: abierto, abierto lleno, cancelado, planificado, cerrado y el resto abiertos.</summary>
    private static (EstadoGrupo Estado, bool Lleno) EstadosVigente(int indice, int total) => indice switch
    {
        0 => (EstadoGrupo.Abierto, false),
        1 => (EstadoGrupo.Abierto, true),
        2 => (EstadoGrupo.Cancelado, false),
        3 => (EstadoGrupo.Planificado, false),
        4 => (EstadoGrupo.Cerrado, false),
        _ => (EstadoGrupo.Abierto, false),
    };

    private static (EstadoGrupo Estado, bool Lleno) EstadosFuturo(int indice, int total) => (EstadoGrupo.Planificado, false);

    private void SembrarPeriodo(
        Periodo periodo,
        int cantidad,
        IReadOnlyList<int> profesores,
        Func<int, int, (EstadoGrupo Estado, bool Lleno)> estadoDe)
    {
        var planificador = new PlanificadorHorarios(new Random(faker.Random.Int()));
        datos.Planificadores[periodo] = planificador;
        var creados = new List<GrupoSembrado>();

        for (var intento = 0; intento < cantidad; intento++)
        {
            // El estado depende de cuántos grupos ya se crearon, para no perder un rol si un intento falla.
            var (estado, lleno) = estadoDe(creados.Count, cantidad);

            // El primer grupo del periodo futuro queda sin horarios: sirve para probar que no se puede abrir (RF-47).
            var sinHorarios = periodo == datos.Futuro && creados.Count == 0;

            var grupo = sinHorarios
                ? CrearSinHorarios(periodo, profesores, estado)
                : CrearConHorarios(periodo, profesores, planificador, estado, lleno, creados);

            if (grupo is null)
            {
                datos.GruposSinEspacio++;
                alFaltarEspacio(periodo.Nombre);
                continue;
            }

            creados.Add(grupo);
        }

        datos.Grupos.AddRange(creados);
    }

    private GrupoSembrado? CrearConHorarios(
        Periodo periodo,
        IReadOnlyList<int> profesores,
        PlanificadorHorarios planificador,
        EstadoGrupo estado,
        bool lleno,
        List<GrupoSembrado> creados)
    {
        var esVirtual = faker.Random.Double() < ProporcionVirtuales;
        var aulas = esVirtual ? null : datos.Aulas.Select(a => a.Id).ToList();
        var bloques = faker.Random.Number(1, PlanificadorHorarios.MaxBloques);

        // profesor1 da el primer grupo de cada periodo, si cabe: así tiene grupos en el pasado y el vigente (punto 6).
        var profesor1 = datos.Profesores[0].Id;
        var asignacion = (creados.Count == 0 && profesores.Contains(profesor1)
                ? planificador.IntentarAsignarGrupo(bloques, [profesor1], aulas)
                : null)
            ?? planificador.IntentarAsignarGrupo(bloques, profesores, aulas);
        if (asignacion is null)
        {
            return null;
        }

        // El grupo lleno comparte curso con el primer abierto: así el cambio de grupo tiene a dónde ir (punto 5).
        var curso = lleno && creados.Count > 0 ? creados[0].Grupo.CursoId : faker.PickRandom(datos.Cursos).Id;
        var aula = asignacion.AulaId is null ? null : datos.Aulas.Single(a => a.Id == asignacion.AulaId);

        // El grupo lleno tiene un cupo chico para que se pueda llenar con pocos estudiantes.
        var cupo = (aula, lleno) switch
        {
            (null, true) => faker.Random.Number(5, 8),
            (null, false) => faker.Random.Number(15, 40),
            (_, true) => Math.Min(aula.Capacidad, faker.Random.Number(5, 8)),
            (_, false) => Math.Min(aula.Capacidad, faker.Random.Number(10, 30)), // RN-09
        };
        var modalidad = aula is null ? Modalidad.Virtual : faker.PickRandom(Modalidad.Presencial, Modalidad.Hibrida);

        var grupo = new Grupo(NuevoNrc(), curso, asignacion.ProfesorId, periodo.Id, aula?.Id, cupo, modalidad);
        foreach (var franja in asignacion.Franjas)
        {
            grupo.AgregarHorario(franja.Dia, franja.HoraInicio, franja.HoraFin);
        }

        Abrir(grupo, estado);
        return new GrupoSembrado(grupo, periodo, asignacion.Franjas, estado, lleno);
    }

    /// <summary>Sin horarios no ocupa franjas, así que no puede traslaparse con ningún otro grupo.</summary>
    private GrupoSembrado CrearSinHorarios(Periodo periodo, IReadOnlyList<int> profesores, EstadoGrupo estado)
    {
        var aula = faker.PickRandom(datos.Aulas);
        var grupo = new Grupo(
            NuevoNrc(),
            faker.PickRandom(datos.Cursos).Id,
            faker.PickRandom<int>(profesores),
            periodo.Id,
            aula.Id,
            Math.Min(aula.Capacidad, faker.Random.Number(10, 30)),
            Modalidad.Presencial);

        return new GrupoSembrado(grupo, periodo, [], estado, lleno: false);
    }

    /// <summary>
    /// Los que reciben matrículas se abren ya (RF-47); su estado final (cerrado, cancelado o finalizado) se aplica
    /// después de matricular, como en la operación normal.
    /// </summary>
    private static void Abrir(Grupo grupo, EstadoGrupo estadoFinal)
    {
        if (estadoFinal != EstadoGrupo.Planificado)
        {
            grupo.CambiarEstado(EstadoGrupo.Abierto);
        }
    }

    private string NuevoNrc()
    {
        string nrc;
        do
        {
            nrc = faker.Random.Number(10000, 99999).ToString(CultureInfo.InvariantCulture);
        }
        while (!nrcs.Add(nrc));

        return nrc;
    }
}
