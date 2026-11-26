namespace Matricula.Domain.Rules;

/// <summary>
/// RN-26: al menos 8 caracteres, con al menos una letra mayúscula y un número.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public const string Message = "La contraseña debe tener al menos 8 caracteres, una letra mayúscula y un número.";

    public static bool IsValid(string? contrasena) =>
        contrasena is not null
        && contrasena.Length >= MinLength
        && contrasena.Any(char.IsUpper)
        && contrasena.Any(char.IsAsciiDigit);
}