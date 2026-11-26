using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;

namespace Matricula.UnitTests.Domain;

public class GrupoTests
{
    private const int CursoId = 1;
    private const int ProfesorId = 2;
    private const int PeriodoId = 3;
    private const int AulaId = 4;

    private static Grupo CrearGrupo(string nrc = "10001", Modalidad modalidad = Modalidad.Presencial, int? aulaId = AulaId) =>
        new(nrc, CursoId, ProfesorId, PeriodoId, aulaId, 25, modalidad);

    private static Grupo CrearGrupoConHorario()
    {
        var grupo = CrearGrupo();
        grupo.AddHorario(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
        return grupo;
    }

    // Lleva el grupo por el flujo normal hasta el estado pedido.
    private static Grupo CrearGrupoEn(EstadoGrupo estado)
    {
        var grupo = CrearGrupoConHorario();
        if (estado == EstadoGrupo.Cancelado)
        {
            grupo.ChangeEstado(EstadoGrupo.Cancelado);
            return grupo;
        }

        EstadoGrupo[] flujo = [EstadoGrupo.Abierto, EstadoGrupo.Cerrado, EstadoGrupo.EnCurso, EstadoGrupo.Finalizado];
        foreach (var paso in flujo.TakeWhile(_ => grupo.Estado != estado))
        {
            grupo.ChangeEstado(paso);
        }

        return grupo;
    }

    private static void ActualizarCon(
        Grupo grupo,
        string? nrc = null,
        int cursoId = CursoId,
        int profesorId = ProfesorId,
        int periodoId = PeriodoId,
        int? aulaId = AulaId,
        int cupo = 25,
        Modalidad modalidad = Modalidad.Presencial) =>
        grupo.Update(nrc ?? grupo.Nrc, cursoId, profesorId, periodoId, aulaId, cupo, modalidad);

    [Fact(DisplayName = "Crear un grupo con datos válidos lo deja Planificado, sin cupos ocupados ni horarios")]
    public void Constructor_ValidData_CreatesPlanificadoWithoutOcupadosOrHorarios()
    {
        var grupo = CrearGrupo();

        Assert.Equal(EstadoGrupo.Planificado, grupo.Estado);
        Assert.Equal(0, grupo.CuposOcupados);
        Assert.Equal(25, grupo.CuposDisponibles);
        Assert.Empty(grupo.Horarios);
        Assert.Equal(CursoId, grupo.CursoId);
        Assert.Equal(ProfesorId, grupo.ProfesorId);
        Assert.Equal(PeriodoId, grupo.PeriodoId);
        Assert.Equal(AulaId, grupo.AulaId);
    }

    [Fact(DisplayName = "Un NRC en minúsculas y con espacios se normaliza")]
    public void Constructor_LowercaseNrcWithSpaces_NormalizesIt()
    {
        Assert.Equal("NRC-01", CrearGrupo("  nrc-01 ").Nrc);
    }

    [Theory(DisplayName = "Un NRC en blanco lanza una excepción")]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeNrc_BlankNrc_Throws(string nrc)
    {
        Assert.Throws<ArgumentException>(() => Grupo.NormalizeNrc(nrc));
    }

    [Theory(DisplayName = "Un cupo cero o negativo lanza una excepción")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveCupo_Throws(int cupo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Grupo("10001", CursoId, ProfesorId, PeriodoId, AulaId, cupo, Modalidad.Presencial));
    }

    [Theory(DisplayName = "Una modalidad que requiere aula sin aula lanza RN-09")]
    [InlineData(Modalidad.Presencial)]
    [InlineData(Modalidad.Hibrida)]
    public void Constructor_ModalidadWithAulaWithoutAula_ThrowsRn09(Modalidad modalidad)
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() => CrearGrupo(modalidad: modalidad, aulaId: null));

        Assert.Equal("RN-09", excepcion.Code);
    }

    [Fact(DisplayName = "Un grupo virtual con aula lanza RN-09")]
    public void Constructor_VirtualWithAula_ThrowsRn09()
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() => CrearGrupo(modalidad: Modalidad.Virtual));

        Assert.Equal("RN-09", excepcion.Code);
    }

    [Fact(DisplayName = "Un grupo virtual sin aula se crea")]
    public void Constructor_VirtualWithoutAula_CreatesGrupo()
    {
        var grupo = CrearGrupo(modalidad: Modalidad.Virtual, aulaId: null);

        Assert.Null(grupo.AulaId);
    }

    [Theory(DisplayName = "Si requiere aula depende de la modalidad")]
    [InlineData(Modalidad.Presencial, true)]
    [InlineData(Modalidad.Hibrida, true)]
    [InlineData(Modalidad.Virtual, false)]
    public void RequiresAula_ByModalidad_ReturnsExpected(Modalidad modalidad, bool esperado)
    {
        Assert.Equal(esperado, Grupo.RequiresAula(modalidad));
    }

    [Theory(DisplayName = "Una transición permitida cambia el estado (RF-47)")]
    [InlineData(EstadoGrupo.Planificado, EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Abierto, EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.Cerrado, EstadoGrupo.EnCurso)]
    [InlineData(EstadoGrupo.EnCurso, EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Planificado, EstadoGrupo.Cancelado)]
    [InlineData(EstadoGrupo.Abierto, EstadoGrupo.Cancelado)]
    [InlineData(EstadoGrupo.Cerrado, EstadoGrupo.Cancelado)]
    [InlineData(EstadoGrupo.EnCurso, EstadoGrupo.Cancelado)]
    public void ChangeEstado_AllowedTransition_ChangesEstado(EstadoGrupo desde, EstadoGrupo hacia)
    {
        var grupo = CrearGrupoEn(desde);

        grupo.ChangeEstado(hacia);

        Assert.Equal(hacia, grupo.Estado);
    }

    [Theory(DisplayName = "Una transición no permitida lanza RF-47")]
    [InlineData(EstadoGrupo.Planificado, EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.Planificado, EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Abierto, EstadoGrupo.Planificado)]
    [InlineData(EstadoGrupo.Abierto, EstadoGrupo.EnCurso)]
    [InlineData(EstadoGrupo.Cerrado, EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.EnCurso, EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.Finalizado, EstadoGrupo.Cancelado)]
    [InlineData(EstadoGrupo.Finalizado, EstadoGrupo.EnCurso)]
    [InlineData(EstadoGrupo.Cancelado, EstadoGrupo.Planificado)]
    [InlineData(EstadoGrupo.Cancelado, EstadoGrupo.Abierto)]
    public void ChangeEstado_NotAllowedTransition_ThrowsRf47(EstadoGrupo desde, EstadoGrupo hacia)
    {
        var grupo = CrearGrupoEn(desde);

        var excepcion = Assert.Throws<BusinessRuleException>(() => grupo.ChangeEstado(hacia));

        Assert.Equal("RF-47", excepcion.Code);
        Assert.Equal(desde, grupo.Estado);
    }

    [Theory(DisplayName = "Cambiar al mismo estado no cambia nada")]
    [InlineData(EstadoGrupo.Planificado)]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void ChangeEstado_SameEstado_ChangesNothing(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        grupo.ChangeEstado(estado);

        Assert.Equal(estado, grupo.Estado);
    }

    [Fact(DisplayName = "Abrir un grupo sin horarios lanza RF-47")]
    public void ChangeEstado_OpenWithoutHorarios_ThrowsRf47()
    {
        var grupo = CrearGrupo();

        var excepcion = Assert.Throws<BusinessRuleException>(() => grupo.ChangeEstado(EstadoGrupo.Abierto));

        Assert.Equal("RF-47", excepcion.Code);
        Assert.Equal(EstadoGrupo.Planificado, grupo.Estado);
    }

    [Fact(DisplayName = "Cancelar un grupo sin horarios lo cancela")]
    public void ChangeEstado_CancelWithoutHorarios_CancelsGrupo()
    {
        var grupo = CrearGrupo();

        grupo.ChangeEstado(EstadoGrupo.Cancelado);

        Assert.Equal(EstadoGrupo.Cancelado, grupo.Estado);
    }

    [Fact(DisplayName = "Un bloque válido se agrega y se devuelve")]
    public void AddHorario_ValidBlock_AddsAndReturnsIt()
    {
        var grupo = CrearGrupo();

        var horario = grupo.AddHorario(DiaSemana.Martes, new TimeOnly(18, 0), new TimeOnly(20, 0));

        Assert.Same(horario, Assert.Single(grupo.Horarios));
    }

    [Fact(DisplayName = "Un bloque que se traslapa con otro del grupo lanza RN-14")]
    public void AddHorario_BlockOverlappingAnotherOfGrupo_ThrowsRn14()
    {
        var grupo = CrearGrupoConHorario();

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            grupo.AddHorario(DiaSemana.Lunes, new TimeOnly(9, 0), new TimeOnly(11, 0)));

        Assert.Equal("RN-14", excepcion.Code);
        Assert.Single(grupo.Horarios);
    }

    [Fact(DisplayName = "Un bloque contiguo y uno de otro día se agregan")]
    public void AddHorario_ContiguousAndOtherDayBlocks_AddsThem()
    {
        var grupo = CrearGrupoConHorario();

        grupo.AddHorario(DiaSemana.Lunes, new TimeOnly(10, 0), new TimeOnly(12, 0));
        grupo.AddHorario(DiaSemana.Miercoles, new TimeOnly(8, 0), new TimeOnly(10, 0));

        Assert.Equal(3, grupo.Horarios.Count);
    }

    [Fact(DisplayName = "Un bloque con horas inválidas lanza RN-15")]
    public void AddHorario_InvalidHours_ThrowsRn15()
    {
        var grupo = CrearGrupo();

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            grupo.AddHorario(DiaSemana.Lunes, new TimeOnly(10, 0), new TimeOnly(10, 0)));

        Assert.Equal("RN-15", excepcion.Code);
    }

    [Theory(DisplayName = "A un grupo en marcha se le puede agregar un bloque")]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    public void AddHorario_GrupoInProgress_AddsIt(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        grupo.AddHorario(DiaSemana.Viernes, new TimeOnly(8, 0), new TimeOnly(10, 0));

        Assert.Equal(2, grupo.Horarios.Count);
    }

    [Theory(DisplayName = "Agregar un bloque a un grupo finalizado o cancelado lanza RF-48")]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void AddHorario_FinalizadoOrCanceladoGrupo_ThrowsRf48(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            grupo.AddHorario(DiaSemana.Viernes, new TimeOnly(8, 0), new TimeOnly(10, 0)));

        Assert.Equal("RF-48", excepcion.Code);
    }

    [Fact(DisplayName = "Quitar un bloque de un grupo planificado lo quita y lo devuelve")]
    public void RemoveHorario_PlanificadoGrupo_RemovesAndReturnsIt()
    {
        var grupo = CrearGrupo();
        var horario = grupo.AddHorario(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
        horario.Id = 7;

        var quitado = grupo.RemoveHorario(7);

        Assert.Same(horario, quitado);
        Assert.Empty(grupo.Horarios);
    }

    [Fact(DisplayName = "Quitar un bloque de otro grupo devuelve null")]
    public void RemoveHorario_BlockOfOtherGrupo_ReturnsNull()
    {
        var grupo = CrearGrupoConHorario();

        Assert.Null(grupo.RemoveHorario(99));
        Assert.Single(grupo.Horarios);
    }

    [Theory(DisplayName = "Quitar un bloque de un grupo no planificado lanza RF-48")]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void RemoveHorario_NotPlanificadoGrupo_ThrowsRf48(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);
        var horario = grupo.Horarios.Single();
        horario.Id = 7;

        var excepcion = Assert.Throws<BusinessRuleException>(() => grupo.RemoveHorario(7));

        Assert.Equal("RF-48", excepcion.Code);
        Assert.Single(grupo.Horarios);
    }

    [Fact(DisplayName = "Actualizar un grupo planificado cambia todos los datos")]
    public void Update_PlanificadoGrupo_ChangesAllData()
    {
        var grupo = CrearGrupo();

        grupo.Update(" nrc-2 ", 11, 12, 13, null, 30, Modalidad.Virtual);

        Assert.Equal("NRC-2", grupo.Nrc);
        Assert.Equal(11, grupo.CursoId);
        Assert.Equal(12, grupo.ProfesorId);
        Assert.Equal(13, grupo.PeriodoId);
        Assert.Null(grupo.AulaId);
        Assert.Equal(30, grupo.Cupo);
        Assert.Equal(Modalidad.Virtual, grupo.Modalidad);
    }

    [Theory(DisplayName = "En un grupo en marcha se pueden reasignar el profesor, el aula, la modalidad y el cupo")]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    public void Update_GrupoInProgress_AllowsReassigningProfesorAulaModalidadAndCupo(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        ActualizarCon(grupo, profesorId: 20, aulaId: 40, cupo: 15, modalidad: Modalidad.Hibrida);

        Assert.Equal(20, grupo.ProfesorId);
        Assert.Equal(40, grupo.AulaId);
        Assert.Equal(15, grupo.Cupo);
        Assert.Equal(Modalidad.Hibrida, grupo.Modalidad);
    }

    [Fact(DisplayName = "En un grupo en marcha, el mismo NRC con otra capitalización no cuenta como cambio")]
    public void Update_GrupoInProgressWithSameNrcInOtherCasing_IsNotAChange()
    {
        var grupo = CrearGrupoEn(EstadoGrupo.Abierto);

        ActualizarCon(grupo, nrc: " 10001 ", cupo: 30);

        Assert.Equal(30, grupo.Cupo);
    }

    [Theory(DisplayName = "Cambiar el NRC, el curso o el periodo de un grupo en marcha lanza RF-45")]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    public void Update_GrupoInProgressChangingNrcCursoOrPeriodo_ThrowsRf45(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        Assert.Equal("RF-45", Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, nrc: "OTRO")).Code);
        Assert.Equal("RF-45", Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, cursoId: 99)).Code);
        Assert.Equal("RF-45", Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, periodoId: 99)).Code);
        Assert.Equal("10001", grupo.Nrc);
        Assert.Equal(CursoId, grupo.CursoId);
        Assert.Equal(PeriodoId, grupo.PeriodoId);
    }

    [Theory(DisplayName = "Actualizar un grupo finalizado o cancelado lanza RF-45")]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void Update_FinalizadoOrCanceladoGrupo_ThrowsRf45(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        var excepcion = Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, cupo: 30));

        Assert.Equal("RF-45", excepcion.Code);
        Assert.Equal(25, grupo.Cupo);
    }

    [Fact(DisplayName = "Tomar cupos hasta llenar aumenta los ocupados y reduce los disponibles")]
    public void TakeCupo_UntilFull_IncreasesOcupadosAndReducesDisponibles()
    {
        var grupo = CrearGrupo();

        for (var i = 0; i < 25; i++)
        {
            grupo.TakeCupo();
        }

        Assert.Equal(25, grupo.CuposOcupados);
        Assert.Equal(0, grupo.CuposDisponibles);
    }

    [Fact(DisplayName = "Tomar un cupo de un grupo lleno lanza RN-03 y no cambia los ocupados")]
    public void TakeCupo_FullGrupo_ThrowsRn03AndKeepsOcupados()
    {
        var grupo = new Grupo("10001", CursoId, ProfesorId, PeriodoId, AulaId, 1, Modalidad.Presencial);
        grupo.TakeCupo();

        var excepcion = Assert.Throws<BusinessRuleException>(grupo.TakeCupo);

        Assert.Equal("RN-03", excepcion.Code);
        Assert.Equal(1, grupo.CuposOcupados);
    }

    [Fact(DisplayName = "Liberar un cupo reduce los ocupados")]
    public void ReleaseCupo_WithOcupados_ReducesOcupados()
    {
        var grupo = CrearGrupo();
        grupo.TakeCupo();
        grupo.TakeCupo();

        grupo.ReleaseCupo();

        Assert.Equal(1, grupo.CuposOcupados);
        Assert.Equal(24, grupo.CuposDisponibles);
    }

    [Fact(DisplayName = "Liberar un cupo sin ocupados no baja de cero")]
    public void ReleaseCupo_WithoutOcupados_DoesNotGoBelowZero()
    {
        var grupo = CrearGrupo();

        grupo.ReleaseCupo();

        Assert.Equal(0, grupo.CuposOcupados);
    }

    // Verificación heredada de grupos T1: el cupo no baja de los ocupados.
    [Fact(DisplayName = "Un cupo menor que los ocupados lanza RF-45 y no cambia el cupo")]
    public void Update_CupoBelowOcupados_ThrowsRf45AndKeepsCupo()
    {
        var grupo = CrearGrupo();
        for (var i = 0; i < 10; i++)
        {
            grupo.TakeCupo();
        }

        var excepcion = Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, cupo: 9));

        Assert.Equal("RF-45", excepcion.Code);
        Assert.Equal(25, grupo.Cupo);
    }

    [Fact(DisplayName = "Un cupo igual a los ocupados se acepta")]
    public void Update_CupoEqualToOcupados_AcceptsIt()
    {
        var grupo = CrearGrupo();
        for (var i = 0; i < 10; i++)
        {
            grupo.TakeCupo();
        }

        ActualizarCon(grupo, cupo: 10);

        Assert.Equal(10, grupo.Cupo);
        Assert.Equal(0, grupo.CuposDisponibles);
    }

    [Theory(DisplayName = "Un estado que permite matrículas y retiros no lanza")]
    [InlineData(EstadoGrupo.Abierto, false)]
    [InlineData(EstadoGrupo.Abierto, true)]
    [InlineData(EstadoGrupo.Planificado, true)]
    [InlineData(EstadoGrupo.Cerrado, true)]
    [InlineData(EstadoGrupo.EnCurso, true)]
    public void EnsureAllowsMovimiento_AllowedEstado_DoesNotThrow(EstadoGrupo estado, bool esAdministrador)
    {
        var grupo = CrearGrupoEn(estado);

        var excepcion = Record.Exception(() => grupo.EnsureAllowsMovimiento(esAdministrador));

        Assert.Null(excepcion);
    }

    [Theory(DisplayName = "Un estado que no permite matrículas ni retiros lanza su regla")]
    [InlineData(EstadoGrupo.Planificado, false, "RN-02")]
    [InlineData(EstadoGrupo.Cerrado, false, "RN-11")]
    [InlineData(EstadoGrupo.EnCurso, false, "RN-11")]
    [InlineData(EstadoGrupo.Finalizado, false, "RN-11")]
    [InlineData(EstadoGrupo.Cancelado, false, "RN-11")]
    [InlineData(EstadoGrupo.Finalizado, true, "RN-11")]
    [InlineData(EstadoGrupo.Cancelado, true, "RN-11")]
    public void EnsureAllowsMovimiento_NotAllowedEstado_ThrowsRule(EstadoGrupo estado, bool esAdministrador, string codigo)
    {
        var grupo = CrearGrupoEn(estado);

        var excepcion = Assert.Throws<BusinessRuleException>(() => grupo.EnsureAllowsMovimiento(esAdministrador));

        Assert.Equal(codigo, excepcion.Code);
    }

    [Fact(DisplayName = "Una modalidad que requiere aula sin aula lanza RN-09 y no cambia nada")]
    public void Update_ModalidadWithoutRequiredAula_ThrowsRn09AndChangesNothing()
    {
        var grupo = CrearGrupo();

        var excepcion = Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, aulaId: null, cupo: 30));

        Assert.Equal("RN-09", excepcion.Code);
        Assert.Equal(25, grupo.Cupo);
        Assert.Equal(AulaId, grupo.AulaId);
    }
}
