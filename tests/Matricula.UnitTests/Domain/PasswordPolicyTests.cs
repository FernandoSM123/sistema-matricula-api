using Matricula.Domain.Rules;

namespace Matricula.UnitTests.Domain;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Segura123")]
    [InlineData("ABCDEFG1")]
    [InlineData("Ñandú2026")]
    public void IsValid_ContrasenaValida_DevuelveTrue(string contrasena)
    {
        Assert.True(PasswordPolicy.IsValid(contrasena));
    }

    [Theory]
    [InlineData("Corta1")]
    [InlineData("Abcdef1")]
    [InlineData("sinmayuscula1")]
    [InlineData("SinNumeros")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_ContrasenaInvalida_DevuelveFalse(string? contrasena)
    {
        Assert.False(PasswordPolicy.IsValid(contrasena));
    }

    [Fact]
    public void IsValid_DigitoNoAscii_NoCuentaComoNumero()
    {
        // "٣" es el 3 arábigo-índico: char.IsDigit lo acepta, pero RN-26 pide un número común.
        Assert.False(PasswordPolicy.IsValid("Segura٣٣٣"));
    }
}