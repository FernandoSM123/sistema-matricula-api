using Matricula.Domain.Enums;
using Matricula.Infrastructure.Seed.TestData;

namespace Matricula.UnitTests.Seed;

public class HorarioPlannerTests
{
    private static readonly TimeSlot LunesTemprano = new(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
    private static readonly TimeSlot MartesTemprano = new(DiaSemana.Martes, new TimeOnly(8, 0), new TimeOnly(10, 0));

    [Fact(DisplayName = "Las franjas horarias son las de la spec de datos de prueba")]
    public void All_HasSpecTimeSlots()
    {
        Assert.Equal(32, TimeSlot.All.Count);
        Assert.Equal(2, TimeSlot.All.Count(f => f.Dia == DiaSemana.Sabado));
        Assert.DoesNotContain(TimeSlot.All, f => f.Dia == DiaSemana.Domingo);
        Assert.Equal(TimeSlot.All.Count, TimeSlot.All.Distinct().Count());
    }

    [Fact(DisplayName = "Con un profesor y un aula asigna grupos hasta agotar las franjas, sin repetir")]
    public void TryAssignGrupo_OneProfesorAndOneAula_AssignsUntilSlotsRunOutWithoutRepeating()
    {
        var planificador = new HorarioPlanner(new Random(1));

        var asignaciones = Enumerable.Range(0, TimeSlot.All.Count)
            .Select(_ => planificador.TryAssignGrupo(1, [1], [10]))
            .ToList();

        Assert.All(asignaciones, Assert.NotNull);
        Assert.Equal(TimeSlot.All.Count, asignaciones.SelectMany(a => a!.TimeSlots).Distinct().Count());
        Assert.Null(planificador.TryAssignGrupo(1, [1], [10]));
    }

    [Fact(DisplayName = "Con varios profesores y aulas nunca repite profesor ni aula en una franja")]
    public void TryAssignGrupo_SeveralProfesoresAndAulas_NeverRepeatsProfesorOrAulaInSlot()
    {
        var random = new Random(7);
        var planificador = new HorarioPlanner(random);

        var asignaciones = Enumerable.Range(0, 200)
            .Select(i => planificador.TryAssignGrupo(1 + (i % 2), [1, 2, 3, 4, 5], i % 5 == 0 ? null : [10, 11, 12]))
            .OfType<GrupoAssignment>()
            .ToList();

        Assert.NotEmpty(asignaciones);
        var porProfesor = asignaciones.SelectMany(a => a.TimeSlots.Select(f => (a.ProfesorId, f))).ToList();
        Assert.Equal(porProfesor.Count, porProfesor.Distinct().Count());
        var porAula = asignaciones.Where(a => a.AulaId is not null)
            .SelectMany(a => a.TimeSlots.Select(f => (a.AulaId, f))).ToList();
        Assert.Equal(porAula.Count, porAula.Distinct().Count());
    }

    [Fact(DisplayName = "Un grupo virtual no ocupa aulas")]
    public void TryAssignGrupo_Virtual_DoesNotUseAulas()
    {
        var planificador = new HorarioPlanner(new Random(3));

        for (var i = 0; i < TimeSlot.All.Count; i++)
        {
            var virtualAsignado = planificador.TryAssignGrupo(1, [1], null);
            Assert.NotNull(virtualAsignado);
            Assert.Null(virtualAsignado.AulaId);
        }

        // El aula 10 sigue libre en todas las franjas para otro profesor.
        for (var i = 0; i < TimeSlot.All.Count; i++)
        {
            var presencial = planificador.TryAssignGrupo(1, [2], [10]);
            Assert.NotNull(presencial);
            Assert.Equal(10, presencial.AulaId);
        }
    }

    [Fact(DisplayName = "Los dos bloques de un grupo caen en días distintos")]
    public void TryAssignGrupo_TwoBlocks_FallOnDifferentDays()
    {
        var planificador = new HorarioPlanner(new Random(5));

        for (var i = 0; i < 50; i++)
        {
            var asignacion = planificador.TryAssignGrupo(2, [1, 2, 3, 4, 5, 6], [10, 11, 12, 13]);

            Assert.NotNull(asignacion);
            Assert.Equal(2, asignacion.TimeSlots.Count);
            Assert.NotEqual(asignacion.TimeSlots[0].Dia, asignacion.TimeSlots[1].Dia);
        }
    }

    [Theory(DisplayName = "Una cantidad de bloques fuera de rango lanza una excepción")]
    [InlineData(0)]
    [InlineData(3)]
    public void TryAssignGrupo_BlockCountOutOfRange_Throws(int cantidadBloques)
    {
        var planificador = new HorarioPlanner(new Random(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => planificador.TryAssignGrupo(cantidadBloques, [1], [10]));
    }

    [Fact(DisplayName = "Sin profesores no asigna el grupo")]
    public void TryAssignGrupo_WithoutProfesores_ReturnsNull()
    {
        var planificador = new HorarioPlanner(new Random(1));

        Assert.Null(planificador.TryAssignGrupo(1, [], [10]));
    }

    [Fact(DisplayName = "La misma semilla produce la misma secuencia")]
    public void TryAssignGrupo_SameSeed_SameSequence()
    {
        Assert.Equal(CreatePlanner(42), CreatePlanner(42));
        Assert.NotEqual(CreatePlanner(42), CreatePlanner(43));
    }

    [Fact(DisplayName = "No permite matricular dos grupos del mismo curso")]
    public void CanEnroll_SameCurso_Rejects()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.False(planificador.CanEnroll(1, cursoId: 100, [MartesTemprano]));
    }

    [Fact(DisplayName = "No permite matricular dos grupos en la misma franja")]
    public void CanEnroll_SharedSlot_Rejects()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.False(planificador.CanEnroll(1, cursoId: 200, [MartesTemprano, LunesTemprano]));
    }

    [Fact(DisplayName = "Permite matricular otro curso en otra franja")]
    public void CanEnroll_OtherCursoInOtherSlot_Allows()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.True(planificador.CanEnroll(1, cursoId: 200, [MartesTemprano]));
        Assert.True(planificador.CanEnroll(2, cursoId: 100, [LunesTemprano]));
    }

    [Fact(DisplayName = "Registrar una matrícula no permitida lanza una excepción")]
    public void RecordMatricula_NotAllowed_Throws()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.Throws<InvalidOperationException>(() => planificador.RecordMatricula(1, cursoId: 200, [LunesTemprano]));
    }

    [Fact(DisplayName = "Liberar una matrícula vuelve a permitir el curso y la franja")]
    public void ReleaseMatricula_AllowsCursoAndSlotAgain()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        planificador.ReleaseMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.True(planificador.CanEnroll(1, cursoId: 100, [LunesTemprano]));
    }

    private static List<string> CreatePlanner(int semilla)
    {
        var planificador = new HorarioPlanner(new Random(semilla));

        return Enumerable.Range(0, 30)
            .Select(i => planificador.TryAssignGrupo(1 + (i % 2), [1, 2, 3], [10, 11]))
            .Select(a => a is null
                ? "-"
                : $"{a.ProfesorId}/{a.AulaId}/{string.Join(",", a.TimeSlots.Select(f => $"{f.Dia}{f.HoraInicio:HH}"))}")
            .ToList();
    }
}
