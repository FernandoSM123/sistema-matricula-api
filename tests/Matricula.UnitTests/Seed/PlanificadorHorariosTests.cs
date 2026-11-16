using Matricula.Domain.Enums;
using Matricula.Infrastructure.Seed.DatosPrueba;

namespace Matricula.UnitTests.Seed;

public class PlanificadorHorariosTests
{
    private static readonly Franja LunesTemprano = new(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
    private static readonly Franja MartesTemprano = new(DiaSemana.Martes, new TimeOnly(8, 0), new TimeOnly(10, 0));

    [Fact]
    public void Todas_TieneLasFranjasDeLaSpec()
    {
        Assert.Equal(32, Franja.Todas.Count);
        Assert.Equal(2, Franja.Todas.Count(f => f.Dia == DiaSemana.Sabado));
        Assert.DoesNotContain(Franja.Todas, f => f.Dia == DiaSemana.Domingo);
        Assert.Equal(Franja.Todas.Count, Franja.Todas.Distinct().Count());
    }

    [Fact]
    public void IntentarAsignarGrupo_UnProfesorYUnAula_AsignaHastaAgotarLasFranjasSinRepetir()
    {
        var planificador = new PlanificadorHorarios(new Random(1));

        var asignaciones = Enumerable.Range(0, Franja.Todas.Count)
            .Select(_ => planificador.IntentarAsignarGrupo(1, [1], [10]))
            .ToList();

        Assert.All(asignaciones, Assert.NotNull);
        Assert.Equal(Franja.Todas.Count, asignaciones.SelectMany(a => a!.Franjas).Distinct().Count());
        Assert.Null(planificador.IntentarAsignarGrupo(1, [1], [10]));
    }

    [Fact]
    public void IntentarAsignarGrupo_VariosProfesoresYAulas_NuncaRepiteProfesorOAulaEnUnaFranja()
    {
        var random = new Random(7);
        var planificador = new PlanificadorHorarios(random);

        var asignaciones = Enumerable.Range(0, 200)
            .Select(i => planificador.IntentarAsignarGrupo(1 + (i % 2), [1, 2, 3, 4, 5], i % 5 == 0 ? null : [10, 11, 12]))
            .OfType<AsignacionGrupo>()
            .ToList();

        Assert.NotEmpty(asignaciones);
        var porProfesor = asignaciones.SelectMany(a => a.Franjas.Select(f => (a.ProfesorId, f))).ToList();
        Assert.Equal(porProfesor.Count, porProfesor.Distinct().Count());
        var porAula = asignaciones.Where(a => a.AulaId is not null)
            .SelectMany(a => a.Franjas.Select(f => (a.AulaId, f))).ToList();
        Assert.Equal(porAula.Count, porAula.Distinct().Count());
    }

    [Fact]
    public void IntentarAsignarGrupo_Virtual_NoOcupaAulas()
    {
        var planificador = new PlanificadorHorarios(new Random(3));

        for (var i = 0; i < Franja.Todas.Count; i++)
        {
            var virtualAsignado = planificador.IntentarAsignarGrupo(1, [1], null);
            Assert.NotNull(virtualAsignado);
            Assert.Null(virtualAsignado.AulaId);
        }

        // El aula 10 sigue libre en todas las franjas para otro profesor.
        for (var i = 0; i < Franja.Todas.Count; i++)
        {
            var presencial = planificador.IntentarAsignarGrupo(1, [2], [10]);
            Assert.NotNull(presencial);
            Assert.Equal(10, presencial.AulaId);
        }
    }

    [Fact]
    public void IntentarAsignarGrupo_DosBloques_CaenEnDiasDistintos()
    {
        var planificador = new PlanificadorHorarios(new Random(5));

        for (var i = 0; i < 50; i++)
        {
            var asignacion = planificador.IntentarAsignarGrupo(2, [1, 2, 3, 4, 5, 6], [10, 11, 12, 13]);

            Assert.NotNull(asignacion);
            Assert.Equal(2, asignacion.Franjas.Count);
            Assert.NotEqual(asignacion.Franjas[0].Dia, asignacion.Franjas[1].Dia);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void IntentarAsignarGrupo_CantidadDeBloquesFueraDeRango_Lanza(int cantidadBloques)
    {
        var planificador = new PlanificadorHorarios(new Random(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => planificador.IntentarAsignarGrupo(cantidadBloques, [1], [10]));
    }

    [Fact]
    public void IntentarAsignarGrupo_SinProfesores_DevuelveNull()
    {
        var planificador = new PlanificadorHorarios(new Random(1));

        Assert.Null(planificador.IntentarAsignarGrupo(1, [], [10]));
    }

    [Fact]
    public void IntentarAsignarGrupo_MismaSemilla_MismaSecuencia()
    {
        Assert.Equal(Planificar(42), Planificar(42));
        Assert.NotEqual(Planificar(42), Planificar(43));
    }

    [Fact]
    public void PuedeMatricular_MismoCurso_Rechaza()
    {
        var planificador = new PlanificadorHorarios(new Random(1));
        planificador.RegistrarMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.False(planificador.PuedeMatricular(1, cursoId: 100, [MartesTemprano]));
    }

    [Fact]
    public void PuedeMatricular_FranjaCompartida_Rechaza()
    {
        var planificador = new PlanificadorHorarios(new Random(1));
        planificador.RegistrarMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.False(planificador.PuedeMatricular(1, cursoId: 200, [MartesTemprano, LunesTemprano]));
    }

    [Fact]
    public void PuedeMatricular_OtroCursoEnOtraFranja_Acepta()
    {
        var planificador = new PlanificadorHorarios(new Random(1));
        planificador.RegistrarMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.True(planificador.PuedeMatricular(1, cursoId: 200, [MartesTemprano]));
        Assert.True(planificador.PuedeMatricular(2, cursoId: 100, [LunesTemprano]));
    }

    [Fact]
    public void RegistrarMatricula_NoPermitida_Lanza()
    {
        var planificador = new PlanificadorHorarios(new Random(1));
        planificador.RegistrarMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.Throws<InvalidOperationException>(() => planificador.RegistrarMatricula(1, cursoId: 200, [LunesTemprano]));
    }

    [Fact]
    public void LiberarMatricula_VuelveAPermitirElCursoYLaFranja()
    {
        var planificador = new PlanificadorHorarios(new Random(1));
        planificador.RegistrarMatricula(1, cursoId: 100, [LunesTemprano]);

        planificador.LiberarMatricula(1, cursoId: 100, [LunesTemprano]);

        Assert.True(planificador.PuedeMatricular(1, cursoId: 100, [LunesTemprano]));
    }

    private static List<string> Planificar(int semilla)
    {
        var planificador = new PlanificadorHorarios(new Random(semilla));

        return Enumerable.Range(0, 30)
            .Select(i => planificador.IntentarAsignarGrupo(1 + (i % 2), [1, 2, 3], [10, 11]))
            .Select(a => a is null
                ? "-"
                : $"{a.ProfesorId}/{a.AulaId}/{string.Join(",", a.Franjas.Select(f => $"{f.Dia}{f.HoraInicio:HH}"))}")
            .ToList();
    }
}
