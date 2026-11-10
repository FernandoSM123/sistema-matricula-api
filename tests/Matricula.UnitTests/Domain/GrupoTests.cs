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
        grupo.AgregarHorario(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
        return grupo;
    }

    // Lleva el grupo por el flujo normal hasta el estado pedido.
    private static Grupo CrearGrupoEn(EstadoGrupo estado)
    {
        var grupo = CrearGrupoConHorario();
        if (estado == EstadoGrupo.Cancelado)
        {
            grupo.CambiarEstado(EstadoGrupo.Cancelado);
            return grupo;
        }

        EstadoGrupo[] flujo = [EstadoGrupo.Abierto, EstadoGrupo.Cerrado, EstadoGrupo.EnCurso, EstadoGrupo.Finalizado];
        foreach (var paso in flujo.TakeWhile(_ => grupo.Estado != estado))
        {
            grupo.CambiarEstado(paso);
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
        grupo.ActualizarDatos(nrc ?? grupo.Nrc, cursoId, profesorId, periodoId, aulaId, cupo, modalidad);

    [Fact]
    public void Constructor_DatosValidos_CreaPlanificadoSinOcupadosNiHorarios()
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

    [Fact]
    public void Constructor_NrcEnMinusculasConEspacios_LoNormaliza()
    {
        Assert.Equal("NRC-01", CrearGrupo("  nrc-01 ").Nrc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarNrc_NrcEnBlanco_LanzaExcepcion(string nrc)
    {
        Assert.Throws<ArgumentException>(() => Grupo.NormalizarNrc(nrc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_CupoNoPositivo_LanzaExcepcion(int cupo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Grupo("10001", CursoId, ProfesorId, PeriodoId, AulaId, cupo, Modalidad.Presencial));
    }

    [Theory]
    [InlineData(Modalidad.Presencial)]
    [InlineData(Modalidad.Hibrida)]
    public void Constructor_ModalidadConAulaSinAula_LanzaRn09(Modalidad modalidad)
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() => CrearGrupo(modalidad: modalidad, aulaId: null));

        Assert.Equal("RN-09", excepcion.Code);
    }

    [Fact]
    public void Constructor_VirtualConAula_LanzaRn09()
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() => CrearGrupo(modalidad: Modalidad.Virtual));

        Assert.Equal("RN-09", excepcion.Code);
    }

    [Fact]
    public void Constructor_VirtualSinAula_CreaElGrupo()
    {
        var grupo = CrearGrupo(modalidad: Modalidad.Virtual, aulaId: null);

        Assert.Null(grupo.AulaId);
    }

    [Theory]
    [InlineData(Modalidad.Presencial, true)]
    [InlineData(Modalidad.Hibrida, true)]
    [InlineData(Modalidad.Virtual, false)]
    public void RequiereAula_SegunModalidad_DevuelveLoEsperado(Modalidad modalidad, bool esperado)
    {
        Assert.Equal(esperado, Grupo.RequiereAula(modalidad));
    }

    [Theory]
    [InlineData(EstadoGrupo.Planificado, EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Abierto, EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.Cerrado, EstadoGrupo.EnCurso)]
    [InlineData(EstadoGrupo.EnCurso, EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Planificado, EstadoGrupo.Cancelado)]
    [InlineData(EstadoGrupo.Abierto, EstadoGrupo.Cancelado)]
    [InlineData(EstadoGrupo.Cerrado, EstadoGrupo.Cancelado)]
    [InlineData(EstadoGrupo.EnCurso, EstadoGrupo.Cancelado)]
    public void CambiarEstado_TransicionPermitida_CambiaElEstado(EstadoGrupo desde, EstadoGrupo hacia)
    {
        var grupo = CrearGrupoEn(desde);

        grupo.CambiarEstado(hacia);

        Assert.Equal(hacia, grupo.Estado);
    }

    [Theory]
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
    public void CambiarEstado_TransicionNoPermitida_LanzaRf47(EstadoGrupo desde, EstadoGrupo hacia)
    {
        var grupo = CrearGrupoEn(desde);

        var excepcion = Assert.Throws<BusinessRuleException>(() => grupo.CambiarEstado(hacia));

        Assert.Equal("RF-47", excepcion.Code);
        Assert.Equal(desde, grupo.Estado);
    }

    [Theory]
    [InlineData(EstadoGrupo.Planificado)]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void CambiarEstado_MismoEstado_NoCambiaNada(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        grupo.CambiarEstado(estado);

        Assert.Equal(estado, grupo.Estado);
    }

    [Fact]
    public void CambiarEstado_AbrirSinHorarios_LanzaRf47()
    {
        var grupo = CrearGrupo();

        var excepcion = Assert.Throws<BusinessRuleException>(() => grupo.CambiarEstado(EstadoGrupo.Abierto));

        Assert.Equal("RF-47", excepcion.Code);
        Assert.Equal(EstadoGrupo.Planificado, grupo.Estado);
    }

    [Fact]
    public void CambiarEstado_CancelarSinHorarios_CancelaElGrupo()
    {
        var grupo = CrearGrupo();

        grupo.CambiarEstado(EstadoGrupo.Cancelado);

        Assert.Equal(EstadoGrupo.Cancelado, grupo.Estado);
    }

    [Fact]
    public void AgregarHorario_BloqueValido_LoAgregaYLoDevuelve()
    {
        var grupo = CrearGrupo();

        var horario = grupo.AgregarHorario(DiaSemana.Martes, new TimeOnly(18, 0), new TimeOnly(20, 0));

        Assert.Same(horario, Assert.Single(grupo.Horarios));
    }

    [Fact]
    public void AgregarHorario_BloqueTraslapadoConOtroDelGrupo_LanzaRn14()
    {
        var grupo = CrearGrupoConHorario();

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            grupo.AgregarHorario(DiaSemana.Lunes, new TimeOnly(9, 0), new TimeOnly(11, 0)));

        Assert.Equal("RN-14", excepcion.Code);
        Assert.Single(grupo.Horarios);
    }

    [Fact]
    public void AgregarHorario_BloqueContiguoYOtroDia_LosAgrega()
    {
        var grupo = CrearGrupoConHorario();

        grupo.AgregarHorario(DiaSemana.Lunes, new TimeOnly(10, 0), new TimeOnly(12, 0));
        grupo.AgregarHorario(DiaSemana.Miercoles, new TimeOnly(8, 0), new TimeOnly(10, 0));

        Assert.Equal(3, grupo.Horarios.Count);
    }

    [Fact]
    public void AgregarHorario_HorasInvalidas_LanzaRn15()
    {
        var grupo = CrearGrupo();

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            grupo.AgregarHorario(DiaSemana.Lunes, new TimeOnly(10, 0), new TimeOnly(10, 0)));

        Assert.Equal("RN-15", excepcion.Code);
    }

    [Theory]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    public void AgregarHorario_GrupoEnMarcha_LoAgrega(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        grupo.AgregarHorario(DiaSemana.Viernes, new TimeOnly(8, 0), new TimeOnly(10, 0));

        Assert.Equal(2, grupo.Horarios.Count);
    }

    [Theory]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void AgregarHorario_GrupoFinalizadoOCancelado_LanzaRf48(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            grupo.AgregarHorario(DiaSemana.Viernes, new TimeOnly(8, 0), new TimeOnly(10, 0)));

        Assert.Equal("RF-48", excepcion.Code);
    }

    [Fact]
    public void QuitarHorario_GrupoPlanificado_LoQuitaYLoDevuelve()
    {
        var grupo = CrearGrupo();
        var horario = grupo.AgregarHorario(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
        horario.Id = 7;

        var quitado = grupo.QuitarHorario(7);

        Assert.Same(horario, quitado);
        Assert.Empty(grupo.Horarios);
    }

    [Fact]
    public void QuitarHorario_BloqueDeOtroGrupo_DevuelveNull()
    {
        var grupo = CrearGrupoConHorario();

        Assert.Null(grupo.QuitarHorario(99));
        Assert.Single(grupo.Horarios);
    }

    [Theory]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void QuitarHorario_GrupoNoPlanificado_LanzaRf48(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);
        var horario = grupo.Horarios.Single();
        horario.Id = 7;

        var excepcion = Assert.Throws<BusinessRuleException>(() => grupo.QuitarHorario(7));

        Assert.Equal("RF-48", excepcion.Code);
        Assert.Single(grupo.Horarios);
    }

    [Fact]
    public void ActualizarDatos_GrupoPlanificado_CambiaTodosLosDatos()
    {
        var grupo = CrearGrupo();

        grupo.ActualizarDatos(" nrc-2 ", 11, 12, 13, null, 30, Modalidad.Virtual);

        Assert.Equal("NRC-2", grupo.Nrc);
        Assert.Equal(11, grupo.CursoId);
        Assert.Equal(12, grupo.ProfesorId);
        Assert.Equal(13, grupo.PeriodoId);
        Assert.Null(grupo.AulaId);
        Assert.Equal(30, grupo.Cupo);
        Assert.Equal(Modalidad.Virtual, grupo.Modalidad);
    }

    [Theory]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    public void ActualizarDatos_GrupoEnMarcha_PermiteReasignarProfesorAulaModalidadYCupo(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        ActualizarCon(grupo, profesorId: 20, aulaId: 40, cupo: 15, modalidad: Modalidad.Hibrida);

        Assert.Equal(20, grupo.ProfesorId);
        Assert.Equal(40, grupo.AulaId);
        Assert.Equal(15, grupo.Cupo);
        Assert.Equal(Modalidad.Hibrida, grupo.Modalidad);
    }

    [Fact]
    public void ActualizarDatos_GrupoEnMarchaConElMismoNrcEnOtraCapitalizacion_NoLoConsideraUnCambio()
    {
        var grupo = CrearGrupoEn(EstadoGrupo.Abierto);

        ActualizarCon(grupo, nrc: " 10001 ", cupo: 30);

        Assert.Equal(30, grupo.Cupo);
    }

    [Theory]
    [InlineData(EstadoGrupo.Abierto)]
    [InlineData(EstadoGrupo.Cerrado)]
    [InlineData(EstadoGrupo.EnCurso)]
    public void ActualizarDatos_GrupoEnMarchaCambiandoNrcCursoOPeriodo_LanzaRf45(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        Assert.Equal("RF-45", Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, nrc: "OTRO")).Code);
        Assert.Equal("RF-45", Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, cursoId: 99)).Code);
        Assert.Equal("RF-45", Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, periodoId: 99)).Code);
        Assert.Equal("10001", grupo.Nrc);
        Assert.Equal(CursoId, grupo.CursoId);
        Assert.Equal(PeriodoId, grupo.PeriodoId);
    }

    [Theory]
    [InlineData(EstadoGrupo.Finalizado)]
    [InlineData(EstadoGrupo.Cancelado)]
    public void ActualizarDatos_GrupoFinalizadoOCancelado_LanzaRf45(EstadoGrupo estado)
    {
        var grupo = CrearGrupoEn(estado);

        var excepcion = Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, cupo: 30));

        Assert.Equal("RF-45", excepcion.Code);
        Assert.Equal(25, grupo.Cupo);
    }

    [Fact]
    public void ActualizarDatos_ModalidadSinAulaRequerida_LanzaRn09YNoCambiaNada()
    {
        var grupo = CrearGrupo();

        var excepcion = Assert.Throws<BusinessRuleException>(() => ActualizarCon(grupo, aulaId: null, cupo: 30));

        Assert.Equal("RN-09", excepcion.Code);
        Assert.Equal(25, grupo.Cupo);
        Assert.Equal(AulaId, grupo.AulaId);
    }
}
