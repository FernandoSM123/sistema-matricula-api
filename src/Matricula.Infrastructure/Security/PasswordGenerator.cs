using System.Security.Cryptography;
using Matricula.Application.Abstractions;

namespace Matricula.Infrastructure.Security;

public class PasswordGenerator : IPasswordGenerator
{
    public const int Length = 12;

    // Sin caracteres que se confunden al copiarlos de un correo (0/O, 1/l/I).
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string All = Uppercase + Lowercase + Digits;

    public string Generate()
    {
        Span<char> caracteres = stackalloc char[Length];

        // Uno de cada grupo garantiza RN-26; el resto se elige del alfabeto completo y luego se mezcla todo.
        caracteres[0] = RandomNumberGenerator.GetItems<char>(Uppercase, 1)[0];
        caracteres[1] = RandomNumberGenerator.GetItems<char>(Lowercase, 1)[0];
        caracteres[2] = RandomNumberGenerator.GetItems<char>(Digits, 1)[0];
        RandomNumberGenerator.GetItems<char>(All, caracteres[3..]);
        RandomNumberGenerator.Shuffle(caracteres);

        return new string(caracteres);
    }
}