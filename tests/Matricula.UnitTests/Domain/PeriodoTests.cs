using Matricula.Domain.Entities;
using Matricula.Domain.Exceptions;

namespace Matricula.UnitTests.Domain;

public class PeriodoTests
{
    private static readonly DateOnly InicioMatricula = new(2027, 1, 10);
    private static readonly DateOnly FinMatricula = new(2027, 2, 10);
    private static readonly DateOnly InicioClases = new(2027, 2, 1);
    private static readonly DateOnly FinClases = new(2027, 6, 30);
    private static readonly DateOnly LimiteRetiro = new(2027, 3, 15);

    private static Periodo CrearPeriodo(string nombre = "2027-I") =>
        new(nombre, InicioClases, FinClases, InicioMatricula, FinMatricula, LimiteRetiro);

    [Fact]
    public void Constructor_NombreEnMinusculasConEspacios_LoNormaliza()
    {
        var periodo = CrearPeriodo("  2027-i ");

        Assert.Equal("2027-I", periodo.Nombre);
    }

    [Fact]
    public void NormalizarNombre_CapitalizacionDistinta_DevuelveElMismoNombre()
    {
        Assert.Equal(Periodo.NormalizarNombre("2027-I"), Periodo.NormalizarNombre(" 2027-i "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarNombre_NombreEnBlanco_LanzaExcepcion(string nombre)
    {
        Assert.Throws<ArgumentException>(() => Periodo.NormalizarNombre(nombre));
    }

    [Fact]
    public void Constructor_FechasIncoherentes_LanzaRn16()
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            new Periodo("2027-I", FinClases, InicioClases, InicioMatricula, FinMatricula, LimiteRetiro));

        Assert.Equal("RN-16", excepcion.Code);
    }

    [Fact]
    public void ActualizarDatos_DatosNuevos_CambiaTodosLosDatos()
    {
        var periodo = CrearPeriodo();
        var inicioMatricula = new DateOnly(2027, 6, 1);
        var finMatricula = new DateOnly(2027, 7, 1);
        var inicioClases = new DateOnly(2027, 7, 5);
        var finClases = new DateOnly(2027, 11, 30);
        var limite = new DateOnly(2027, 8, 1);

        periodo.ActualizarDatos("2027-ii", inicioClases, finClases, inicioMatricula, finMatricula, limite);

        Assert.Equal("2027-II", periodo.Nombre);
        Assert.Equal(inicioClases, periodo.InicioClases);
        Assert.Equal(finClases, periodo.FinClases);
        Assert.Equal(inicioMatricula, periodo.InicioMatricula);
        Assert.Equal(finMatricula, periodo.FinMatricula);
        Assert.Equal(limite, periodo.FechaLimiteRetiro);
    }

    [Fact]
    public void ActualizarDatos_FechasIncoherentes_LanzaRn16YConservaLosDatos()
    {
        var periodo = CrearPeriodo();

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            periodo.ActualizarDatos("OTRO", InicioClases, FinClases, FinMatricula, InicioMatricula, LimiteRetiro));

        Assert.Equal("RN-16", excepcion.Code);
        Assert.Equal("2027-I", periodo.Nombre);
        Assert.Equal(InicioMatricula, periodo.InicioMatricula);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void EnRangoMatricula_SegunDiasRespectoAlInicio_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, CrearPeriodo().EnRangoMatricula(InicioMatricula.AddDays(dias)));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void EnRangoMatricula_SegunDiasRespectoAlFin_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, CrearPeriodo().EnRangoMatricula(FinMatricula.AddDays(dias)));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void PermiteRetiro_SegunDiasRespectoAlLimite_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, CrearPeriodo().PermiteRetiro(LimiteRetiro.AddDays(dias)));
    }
}
