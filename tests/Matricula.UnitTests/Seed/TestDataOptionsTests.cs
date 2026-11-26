using Matricula.Infrastructure.Seed.TestData;

namespace Matricula.UnitTests.Seed;

public class TestDataOptionsTests
{
    private const string ContrasenaValida = "Prueba123";

    [Fact(DisplayName = "Los valores por defecto con una contraseña válida no tienen errores")]
    public void Validate_DefaultsWithValidPassword_NoErrors()
    {
        var options = new TestDataOptions { Password = ContrasenaValida };

        Assert.Empty(options.Validate());
    }

    [Fact(DisplayName = "Los límites de los rangos no tienen errores")]
    public void Validate_RangeLimits_NoErrors()
    {
        var minimos = new TestDataOptions
        {
            Password = ContrasenaValida,
            Cursos = 1, Aulas = 1, Profesores = 1, Estudiantes = 0, GruposPerPeriodo = 0, MaxMatriculasPerEstudiante = 0,
        };
        var maximos = new TestDataOptions
        {
            Password = ContrasenaValida,
            Cursos = 200, Aulas = 100, Profesores = 200, Estudiantes = 5000, GruposPerPeriodo = 200,
            MaxMatriculasPerEstudiante = 6,
        };

        Assert.Empty(minimos.Validate());
        Assert.Empty(maximos.Validate());
    }

    [Theory(DisplayName = "Una clave fuera de rango da un error que la nombra")]
    [InlineData("Cursos", 0)]
    [InlineData("Cursos", 201)]
    [InlineData("Aulas", 0)]
    [InlineData("Aulas", 101)]
    [InlineData("Profesores", 0)]
    [InlineData("Profesores", 201)]
    [InlineData("Estudiantes", -1)]
    [InlineData("Estudiantes", 5001)]
    [InlineData("GruposPerPeriodo", -1)]
    [InlineData("GruposPerPeriodo", 201)]
    [InlineData("MaxMatriculasPerEstudiante", -1)]
    [InlineData("MaxMatriculasPerEstudiante", 7)]
    public void Validate_KeyOutOfRange_ReturnsErrorNamingIt(string clave, int valor)
    {
        var options = With(clave, valor);

        var error = Assert.Single(options.Validate());
        Assert.Contains($"'TestData:{clave}'", error, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "Una contraseña ausente o inválida da un error que nombra la clave")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("prueba123")]
    [InlineData("PruebaSinNumero")]
    [InlineData("Prue1")]
    public void Validate_MissingOrInvalidPassword_ReturnsErrorNamingIt(string? contrasena)
    {
        var options = new TestDataOptions { Password = contrasena };

        var error = Assert.Single(options.Validate());
        Assert.Contains("'TestData:Password'", error, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Varios errores dan uno por clave")]
    public void Validate_SeveralErrors_ReturnsOnePerKey()
    {
        var options = new TestDataOptions { Cursos = 0, Estudiantes = -1 };

        Assert.Equal(3, options.Validate().Count);
    }

    private static TestDataOptions With(string clave, int valor) => clave switch
    {
        "Cursos" => new TestDataOptions { Password = ContrasenaValida, Cursos = valor },
        "Aulas" => new TestDataOptions { Password = ContrasenaValida, Aulas = valor },
        "Profesores" => new TestDataOptions { Password = ContrasenaValida, Profesores = valor },
        "Estudiantes" => new TestDataOptions { Password = ContrasenaValida, Estudiantes = valor },
        "GruposPerPeriodo" => new TestDataOptions { Password = ContrasenaValida, GruposPerPeriodo = valor },
        "MaxMatriculasPerEstudiante" => new TestDataOptions
        {
            Password = ContrasenaValida, MaxMatriculasPerEstudiante = valor,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(clave), clave, null),
    };
}
