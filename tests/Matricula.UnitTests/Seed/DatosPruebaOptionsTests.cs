using Matricula.Infrastructure.Seed.DatosPrueba;

namespace Matricula.UnitTests.Seed;

public class DatosPruebaOptionsTests
{
    private const string ContrasenaValida = "Prueba123";

    [Fact]
    public void Validar_ValoresPorDefectoConContrasenaValida_SinErrores()
    {
        var options = new DatosPruebaOptions { Contrasena = ContrasenaValida };

        Assert.Empty(options.Validar());
    }

    [Fact]
    public void Validar_LimitesDeLosRangos_SinErrores()
    {
        var minimos = new DatosPruebaOptions
        {
            Contrasena = ContrasenaValida,
            Cursos = 1, Aulas = 1, Profesores = 1, Estudiantes = 0, GruposPorPeriodo = 0, MaxMatriculasPorEstudiante = 0,
        };
        var maximos = new DatosPruebaOptions
        {
            Contrasena = ContrasenaValida,
            Cursos = 200, Aulas = 100, Profesores = 200, Estudiantes = 5000, GruposPorPeriodo = 200,
            MaxMatriculasPorEstudiante = 6,
        };

        Assert.Empty(minimos.Validar());
        Assert.Empty(maximos.Validar());
    }

    [Theory]
    [InlineData("Cursos", 0)]
    [InlineData("Cursos", 201)]
    [InlineData("Aulas", 0)]
    [InlineData("Aulas", 101)]
    [InlineData("Profesores", 0)]
    [InlineData("Profesores", 201)]
    [InlineData("Estudiantes", -1)]
    [InlineData("Estudiantes", 5001)]
    [InlineData("GruposPorPeriodo", -1)]
    [InlineData("GruposPorPeriodo", 201)]
    [InlineData("MaxMatriculasPorEstudiante", -1)]
    [InlineData("MaxMatriculasPorEstudiante", 7)]
    public void Validar_ClaveFueraDeRango_DevuelveErrorQueLaNombra(string clave, int valor)
    {
        var options = Con(clave, valor);

        var error = Assert.Single(options.Validar());
        Assert.Contains($"'DatosPrueba:{clave}'", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("prueba123")]
    [InlineData("PruebaSinNumero")]
    [InlineData("Prue1")]
    public void Validar_ContrasenaAusenteOInvalida_DevuelveErrorQueLaNombra(string? contrasena)
    {
        var options = new DatosPruebaOptions { Contrasena = contrasena };

        var error = Assert.Single(options.Validar());
        Assert.Contains("'DatosPrueba:Contrasena'", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validar_VariosErrores_DevuelveUnoPorClave()
    {
        var options = new DatosPruebaOptions { Cursos = 0, Estudiantes = -1 };

        Assert.Equal(3, options.Validar().Count);
    }

    private static DatosPruebaOptions Con(string clave, int valor) => clave switch
    {
        "Cursos" => new DatosPruebaOptions { Contrasena = ContrasenaValida, Cursos = valor },
        "Aulas" => new DatosPruebaOptions { Contrasena = ContrasenaValida, Aulas = valor },
        "Profesores" => new DatosPruebaOptions { Contrasena = ContrasenaValida, Profesores = valor },
        "Estudiantes" => new DatosPruebaOptions { Contrasena = ContrasenaValida, Estudiantes = valor },
        "GruposPorPeriodo" => new DatosPruebaOptions { Contrasena = ContrasenaValida, GruposPorPeriodo = valor },
        "MaxMatriculasPorEstudiante" => new DatosPruebaOptions
        {
            Contrasena = ContrasenaValida, MaxMatriculasPorEstudiante = valor,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(clave), clave, null),
    };
}
