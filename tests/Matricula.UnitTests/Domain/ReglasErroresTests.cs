using Matricula.Domain.Reglas;

namespace Matricula.UnitTests.Domain;

public class ReglasErroresTests
{
    private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

    [Theory]
    [InlineData("corto", 10)]
    [InlineData("exacto", 6)]
    public void Truncar_TextoQueCabe_LoDevuelveIgual(string texto, int maximo)
    {
        Assert.Equal(texto, ReglasErrores.Truncar(texto, maximo));
    }

    [Fact]
    public void Truncar_TextoLargo_MideElMaximoYTerminaEnPuntos()
    {
        var resultado = ReglasErrores.Truncar("abcdefghij", 5);

        Assert.Equal("abcd…", resultado);
        Assert.Equal(5, resultado.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Truncar_SinTexto_DevuelveVacio(string? texto)
    {
        Assert.Equal(string.Empty, ReglasErrores.Truncar(texto, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Truncar_MaximoMenorQueUno_LanzaExcepcion(int maximo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasErrores.Truncar("texto", maximo));
    }

    [Theory]
    [InlineData("00-4BF92F3577B34DA6A3CE929D0E0E4736-00F067AA0BA902B7-01")]
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00")]
    [InlineData(" 4bf92f3577b34da6a3ce929d0e0e4736 ")]
    [InlineData("4BF92F3577B34DA6A3CE929D0E0E4736")]
    public void NormalizarTraceId_FormatoValido_DevuelveTraceIdEnMinusculas(string valor)
    {
        Assert.Equal(TraceId, ReglasErrores.NormalizarTraceId(valor));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("4bf92f3577b34da6a3ce929d0e0e473")] // 31 caracteres
    [InlineData("4bf92f3577b34da6a3ce929d0e0e47366")] // 33 caracteres
    [InlineData("4bf92f3577b34da6a3ce929d0e0e473g")] // carácter no hexadecimal
    [InlineData("00000000000000000000000000000000")]
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b-01")] // span id de 15
    [InlineData("0-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01")] // versión de 1
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7")] // sin flags
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902zz-01")] // span id no hexadecimal
    [InlineData("00-00000000000000000000000000000000-00f067aa0ba902b7-01")]
    public void NormalizarTraceId_FormatoInvalido_DevuelveNull(string? valor)
    {
        Assert.Null(ReglasErrores.NormalizarTraceId(valor));
    }

    [Theory]
    [InlineData(90)]
    [InlineData(1)]
    public void LimiteRetencion_DiasValidos_RestaLosDiasExactos(int dias)
    {
        var ahora = new DateTimeOffset(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

        Assert.Equal(ahora.AddDays(-dias), ReglasErrores.LimiteRetencion(ahora, dias));
    }

    [Fact]
    public void LimiteRetencion_NoventaDiasDesdeElTreceDeNoviembre_EsElQuinceDeAgosto()
    {
        var ahora = new DateTimeOffset(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 8, 15, 15, 30, 0, TimeSpan.Zero), ReglasErrores.LimiteRetencion(ahora, 90));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LimiteRetencion_DiasMenoresQueUno_LanzaExcepcion(int dias)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasErrores.LimiteRetencion(DateTimeOffset.UtcNow, dias));
    }
}
