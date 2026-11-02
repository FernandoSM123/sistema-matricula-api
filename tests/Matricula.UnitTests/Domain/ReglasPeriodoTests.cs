using Matricula.Domain.Reglas;

namespace Matricula.UnitTests.Domain;

public class ReglasPeriodoTests
{
    private static readonly DateOnly Dia = new(2027, 2, 15);
    private static readonly DateOnly InicioMatricula = new(2027, 1, 10);
    private static readonly DateOnly FinMatricula = new(2027, 2, 10);
    private static readonly DateOnly InicioClases = new(2027, 2, 1);
    private static readonly DateOnly FinClases = new(2027, 6, 30);
    private static readonly DateOnly LimiteRetiro = new(2027, 3, 15);

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void MatriculaOrdenada_SegunDiasRespectoAlFin_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, ReglasPeriodo.MatriculaOrdenada(Dia.AddDays(dias), Dia));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void ClasesOrdenadas_SegunDiasRespectoAlFin_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, ReglasPeriodo.ClasesOrdenadas(Dia.AddDays(dias), Dia));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void MatriculaDentroDeClases_SegunDiasRespectoAlFinDeClases_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, ReglasPeriodo.MatriculaDentroDeClases(Dia.AddDays(dias), Dia));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void RetiroDentroDelPeriodo_SegunDiasRespectoAlInicioDeMatricula_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, ReglasPeriodo.RetiroDentroDelPeriodo(InicioMatricula.AddDays(dias), InicioMatricula, FinClases));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void RetiroDentroDelPeriodo_SegunDiasRespectoAlFinDeClases_DevuelveLoEsperado(int dias, bool esperado)
    {
        Assert.Equal(esperado, ReglasPeriodo.RetiroDentroDelPeriodo(FinClases.AddDays(dias), InicioMatricula, FinClases));
    }

    [Fact]
    public void Coherente_FechasValidas_DevuelveTrue()
    {
        Assert.True(ReglasPeriodo.Coherente(InicioClases, FinClases, InicioMatricula, FinMatricula, LimiteRetiro));
    }

    [Fact]
    public void Coherente_FinMatriculaIgualAFinClases_DevuelveTrue()
    {
        Assert.True(ReglasPeriodo.Coherente(InicioClases, FinClases, InicioMatricula, FinClases, LimiteRetiro));
    }

    [Fact]
    public void Coherente_LimiteRetiroAntesDelInicioDeClases_DevuelveTrue()
    {
        var limite = InicioClases.AddDays(-1);

        Assert.True(ReglasPeriodo.Coherente(InicioClases, FinClases, InicioMatricula, FinMatricula, limite));
    }

    [Fact]
    public void Coherente_MatriculaDesordenada_DevuelveFalse()
    {
        Assert.False(ReglasPeriodo.Coherente(InicioClases, FinClases, FinMatricula, InicioMatricula, LimiteRetiro));
    }

    [Fact]
    public void Coherente_ClasesDesordenadas_DevuelveFalse()
    {
        Assert.False(ReglasPeriodo.Coherente(FinClases, InicioClases, InicioMatricula, FinMatricula, LimiteRetiro));
    }

    [Fact]
    public void Coherente_MatriculaTerminaDespuesDeClases_DevuelveFalse()
    {
        Assert.False(ReglasPeriodo.Coherente(InicioClases, FinClases, InicioMatricula, FinClases.AddDays(1), LimiteRetiro));
    }

    [Fact]
    public void Coherente_LimiteRetiroFueraDelPeriodo_DevuelveFalse()
    {
        Assert.False(ReglasPeriodo.Coherente(InicioClases, FinClases, InicioMatricula, FinMatricula, FinClases.AddDays(1)));
    }
}
