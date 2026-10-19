namespace Matricula.Domain.Reglas;

/// <summary>
/// RN-26: al menos 8 caracteres, con al menos una letra mayúscula y un número.
/// </summary>
public static class PoliticaContrasena
{
    public const int LongitudMinima = 8;

    public const string Mensaje = "La contraseña debe tener al menos 8 caracteres, una letra mayúscula y un número.";

    public static bool Cumple(string? contrasena) =>
        contrasena is not null
        && contrasena.Length >= LongitudMinima
        && contrasena.Any(char.IsUpper)
        && contrasena.Any(char.IsAsciiDigit);
}