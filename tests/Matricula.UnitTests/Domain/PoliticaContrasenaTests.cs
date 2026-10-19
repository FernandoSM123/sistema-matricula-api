using Matricula.Domain.Reglas;

namespace Matricula.UnitTests.Domain;

public class PoliticaContrasenaTests
{
    [Theory]
    [InlineData("Segura123")]
    [InlineData("ABCDEFG1")]
    [InlineData("Ñandú2026")]
    public void Cumple_ContrasenaValida_DevuelveTrue(string contrasena)
    {
        Assert.True(PoliticaContrasena.Cumple(contrasena));
    }

    [Theory]
    [InlineData("Corta1")]
    [InlineData("Abcdef1")]
    [InlineData("sinmayuscula1")]
    [InlineData("SinNumeros")]
    [InlineData("")]
    [InlineData(null)]
    public void Cumple_ContrasenaInvalida_DevuelveFalse(string? contrasena)
    {
        Assert.False(PoliticaContrasena.Cumple(contrasena));
    }

    [Fact]
    public void Cumple_DigitoNoAscii_NoCuentaComoNumero()
    {
        // "٣" es el 3 arábigo-índico: char.IsDigit lo acepta, pero RN-26 pide un número común.
        Assert.False(PoliticaContrasena.Cumple("Segura٣٣٣"));
    }
}