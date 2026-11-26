using System.Globalization;
using System.Text;
using Bogus;

namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Datos personales de prueba (punto 8 de la spec). Bogus genera los textos; las cédulas, teléfonos y correos tienen
/// formato propio, y su unicidad se garantiza aquí, no por azar. Los textos se recortan a las longitudes de EF.
/// </summary>
public sealed class PersonGenerator(Faker faker)
{
    public const string EmailDomain = "prueba.test";

    private const int MaxNombreLength = 100;
    private const int MaxDireccionLength = 250;

    private readonly HashSet<string> cedulas = [];
    private readonly HashSet<string> correos = [];
    private int secuencia;

    public string Nombre() => Truncate(faker.Name.FirstName(), MaxNombreLength);

    /// <summary>Dos apellidos, como se usan en Costa Rica.</summary>
    public string Apellidos() => Truncate($"{faker.Name.LastName()} {faker.Name.LastName()}", MaxNombreLength);

    /// <summary>9 dígitos con el primero entre 1 y 7, como las cédulas físicas de Costa Rica.</summary>
    public string Cedula()
    {
        string cedula;
        do
        {
            cedula = faker.Random.Number(1, 7).ToString(CultureInfo.InvariantCulture)
                + faker.Random.ReplaceNumbers("########");
        }
        while (!cedulas.Add(cedula));

        return cedula;
    }

    /// <summary>8 dígitos que empiezan con 2 (fijo) o 6, 7 u 8 (celular).</summary>
    public string Telefono() => faker.PickRandom("2", "6", "7", "8") + faker.Random.ReplaceNumbers("#######");

    /// <summary>
    /// Dirección por señas, como se usan en Costa Rica. Las direcciones de Bogus en español son de España.
    /// </summary>
    public string Direccion()
    {
        var (canton, provincia) = faker.PickRandom<(string, string)>(SampleCatalogs.Cantones);
        var metros = faker.Random.Number(1, 8) * 50;
        var punto = faker.PickRandom<string>(SampleCatalogs.Landmarks);
        var rumbo = faker.PickRandom("norte", "sur", "este", "oeste");

        return Truncate($"{canton}, {provincia}. {metros} m al {rumbo} {punto}", MaxDireccionLength);
    }

    /// <summary>
    /// Correo fijo (por ejemplo <c>estudiante1@prueba.test</c>) o, sin él, <c>nombre.apellido.N@prueba.test</c> sin
    /// tildes ni ñ. El número de secuencia lo hace único entre todas las cuentas de prueba.
    /// </summary>
    public string Correo(string nombre, string apellidos, string? fijo = null)
    {
        var correo = fijo is not null
            ? $"{fijo}@{EmailDomain}"
            : $"{Slugify(nombre)}.{Slugify(apellidos)}.{++secuencia}@{EmailDomain}";

        if (!correos.Add(correo))
        {
            throw new InvalidOperationException($"El correo de prueba {correo} ya se generó.");
        }

        return correo;
    }

    /// <summary>Primera palabra en minúsculas, solo con letras ASCII: "José María" → "jose".</summary>
    public static string Slugify(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        var primera = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var sinMarcas = primera.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        var slug = new string([.. sinMarcas.Where(char.IsAsciiLetter).Select(char.ToLowerInvariant)]);

        return slug.Length > 0 ? slug : "persona";
    }

    private static string Truncate(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo].TrimEnd();
}
