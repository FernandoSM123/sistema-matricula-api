using Matricula.Domain.Rules;

namespace Matricula.UnitTests.Domain;

public class PasswordPolicyTests
{
    [Theory(DisplayName = "Una contraseña que cumple la política es válida (RN-26)")]
    [InlineData("Segura123")]
    [InlineData("ABCDEFG1")]
    [InlineData("Ñandú2026")]
    public void IsValid_ValidPassword_ReturnsTrue(string contrasena)
    {
        Assert.True(PasswordPolicy.IsValid(contrasena));
    }

    [Theory(DisplayName = "Una contraseña que no cumple la política no es válida (RN-26)")]
    [InlineData("Corta1")]
    [InlineData("Abcdef1")]
    [InlineData("sinmayuscula1")]
    [InlineData("SinNumeros")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_InvalidPassword_ReturnsFalse(string? contrasena)
    {
        Assert.False(PasswordPolicy.IsValid(contrasena));
    }

    [Fact(DisplayName = "Un dígito no ASCII no cuenta como número")]
    public void IsValid_NonAsciiDigit_DoesNotCountAsNumber()
    {
        // "٣" es el 3 arábigo-índico: char.IsDigit lo acepta, pero RN-26 pide un número común.
        Assert.False(PasswordPolicy.IsValid("Segura٣٣٣"));
    }
}