using Matricula.Domain.Enums;
using Matricula.Infrastructure.Seed.TestData;

namespace Matricula.UnitTests.Seed;

public class HorarioPlannerTests
{
    private static readonly TimeSlot LunesTemprano = new(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
    private static readonly TimeSlot MartesTemprano = new(DiaSemana.Martes, new TimeOnly(8, 0), new TimeOnly(10, 0));

    [Fact]
    public void All_TieneLasFranjasDeLaSpec()
    {
        Assert.Equal(32, TimeSlot.All.Count);
        Assert.Equal(2, TimeSlot.All.Count(f => f.Dia == DiaSemana.Sabado));
        Assert.DoesNotContain(TimeSlot.All, f => f.Dia == DiaSemana.Domingo);
        Assert.Equal(TimeSlot.All.Count, TimeSlot.All.Distinct().Count());
    }

    [Fact]
    public void TryAssignGrupo_UnProfesorYUnAula_AsignaHastaAgotarLasFranjasSinRepetir()
    {
        var planificador = new HorarioPlanner(new Random(1));

        var asignaciones = Enumerable.Range(0, TimeSlot.All.Count)
            .Select(_ => planificador.TryAssignGrupo(1, [1], [10]))
            .ToList();

        Assert.All(asignaciones, Assert.NotNull);
        Assert.Equal(TimeSlot.All.Count, asignaciones.SelectMany(a => a!.TimeSlots).Distinct().Count());
        Assert.Null(planificador.TryAssignGrupo(1, [1], [10]));
    }

    [Fact]
    public void TryAssignGrupo_VariosProfesoresYAulas_NuncaRepiteProfesorOAulaEnUnaFranja()
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

    [Fact]
    public void TryAssignGrupo_Virtual_NoOcupaAulas()
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

    [Fact]
    public void TryAssignGrupo_DosBloques_CaenEnDiasDistintos()
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

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void TryAssignGrupo_CantidadDeBloquesFueraDeRango_Lanza(int cantidadBloques)
    {
        var planificador = new HorarioPlanner(new Random(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => planificador.TryAssignGrupo(cantidadBloques, [1], [10]));
    }

    [Fact]
    public void TryAssignGrupo_SinProfesores_DevuelveNull()
    {
        var planificador = new HorarioPlanner(new Random(1));

        Assert.Null(planificador.TryAssignGrupo(1, [], [10]));
    }

    [Fact]
    public void TryAssignGrupo_MismaSemilla_MismaSecuencia()
    {
        Assert.Equal(CreatePlanner(42), CreatePlanner(42));
        Assert.NotEqual(CreatePlanner(42), CreatePlanner(43));
    }

    [Fact]
    public void CanEnroll_MismoCurso_Rechaza()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.False(planificador.CanEnroll(1, cursoId: 100, [MartesTemprano]));
    }

    [Fact]
    public void CanEnroll_FranjaCompartida_Rechaza()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.False(planificador.CanEnroll(1, cursoId: 200, [MartesTemprano, LunesTemprano]));
    }

    [Fact]
    public void CanEnroll_OtroCursoEnOtraFranja_Acepta()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.True(planificador.CanEnroll(1, cursoId: 200, [MartesTemprano]));
        Assert.True(planificador.CanEnroll(2, cursoId: 100, [LunesTemprano]));
    }

    [Fact]
    public void RecordMatricula_NoPermitida_Lanza()
    {
        var planificador = new HorarioPlanner(new Random(1));
        planificador.RecordMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.Throws<InvalidOperationException>(() => planificador.RecordMatricula(1, cursoId: 200, [LunesTemprano]));
    }

    [Fact]
    public void ReleaseMatricula_VuelveAPermitirElCursoYLaFranja()
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
