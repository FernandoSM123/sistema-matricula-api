using System.Security.Cryptography;
using Matricula.Application.Abstractions;

namespace Matricula.Infrastructure.Seguridad;

public class GeneradorContrasenas : IGeneradorContrasenas
{
    public const int Longitud = 12;

    // Sin caracteres que se confunden al copiarlos de un correo (0/O, 1/l/I).
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnopqrstuvwxyz";
    private const string Digitos = "23456789";
    private const string Todos = Mayusculas + Minusculas + Digitos;

    public string Generar()
    {
        Span<char> caracteres = stackalloc char[Longitud];

        // Uno de cada grupo garantiza RN-26; el resto se elige del alfabeto completo y luego se mezcla todo.
        caracteres[0] = RandomNumberGenerator.GetItems<char>(Mayusculas, 1)[0];
        caracteres[1] = RandomNumberGenerator.GetItems<char>(Minusculas, 1)[0];
        caracteres[2] = RandomNumberGenerator.GetItems<char>(Digitos, 1)[0];
        RandomNumberGenerator.GetItems<char>(Todos, caracteres[3..]);
        RandomNumberGenerator.Shuffle(caracteres);

        return new string(caracteres);
    }
}