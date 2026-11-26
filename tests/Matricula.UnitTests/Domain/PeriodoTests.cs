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

    [Fact(DisplayName = "Un nombre en minúsculas y con espacios se normaliza")]
    public void Constructor_LowercaseNombreWithSpaces_NormalizesIt()
    {
        var periodo = CrearPeriodo("  2027-i ");

        Assert.Equal("2027-I", periodo.Nombre);
    }

    [Fact(DisplayName = "Nombres con distinta capitalización se normalizan al mismo valor")]
    public void NormalizeNombre_DifferentCasing_ReturnsSameNombre()
    {
        Assert.Equal(Periodo.NormalizeNombre("2027-I"), Periodo.NormalizeNombre(" 2027-i "));
    }

    [Theory(DisplayName = "Un nombre en blanco lanza una excepción")]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeNombre_BlankNombre_Throws(string nombre)
    {
        Assert.Throws<ArgumentException>(() => Periodo.NormalizeNombre(nombre));
    }

    [Fact(DisplayName = "Crear un periodo con fechas incoherentes lanza RN-16")]
    public void Constructor_InconsistentDates_ThrowsRn16()
    {
        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            new Periodo("2027-I", FinClases, InicioClases, InicioMatricula, FinMatricula, LimiteRetiro));

        Assert.Equal("RN-16", excepcion.Code);
    }

    [Fact(DisplayName = "Actualizar con datos nuevos cambia todos los datos")]
    public void Update_NewData_ChangesAllData()
    {
        var periodo = CrearPeriodo();
        var inicioMatricula = new DateOnly(2027, 6, 1);
        var finMatricula = new DateOnly(2027, 7, 1);
        var inicioClases = new DateOnly(2027, 7, 5);
        var finClases = new DateOnly(2027, 11, 30);
        var limite = new DateOnly(2027, 8, 1);

        periodo.Update("2027-ii", inicioClases, finClases, inicioMatricula, finMatricula, limite);

        Assert.Equal("2027-II", periodo.Nombre);
        Assert.Equal(inicioClases, periodo.InicioClases);
        Assert.Equal(finClases, periodo.FinClases);
        Assert.Equal(inicioMatricula, periodo.InicioMatricula);
        Assert.Equal(finMatricula, periodo.FinMatricula);
        Assert.Equal(limite, periodo.FechaLimiteRetiro);
    }

    [Fact(DisplayName = "Actualizar con fechas incoherentes lanza RN-16 y conserva los datos")]
    public void Update_InconsistentDates_ThrowsRn16AndKeepsData()
    {
        var periodo = CrearPeriodo();

        var excepcion = Assert.Throws<BusinessRuleException>(() =>
            periodo.Update("OTRO", InicioClases, FinClases, FinMatricula, InicioMatricula, LimiteRetiro));

        Assert.Equal("RN-16", excepcion.Code);
        Assert.Equal("2027-I", periodo.Nombre);
        Assert.Equal(InicioMatricula, periodo.InicioMatricula);
    }

    [Theory(DisplayName = "El rango de matrícula empieza en su fecha de inicio (RN-04)")]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void IsInMatriculaRange_ByDaysFromStart_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, CrearPeriodo().IsInMatriculaRange(InicioMatricula.AddDays(dias)));
    }

    [Theory(DisplayName = "El rango de matrícula incluye su fecha de fin (RN-04)")]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsInMatriculaRange_ByDaysFromEnd_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, CrearPeriodo().IsInMatriculaRange(FinMatricula.AddDays(dias)));
    }

    [Theory(DisplayName = "El retiro se permite hasta la fecha límite inclusive (RN-10)")]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void AllowsRetiro_ByDaysFromLimit_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, CrearPeriodo().AllowsRetiro(LimiteRetiro.AddDays(dias)));
    }
}
