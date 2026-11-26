using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.UnitTests.Domain;

public class RegistroErrorTests
{
    private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

    private static readonly DateTimeOffset Fecha = new(2026, 11, 13, 21, 4, 5, TimeSpan.Zero);

    private static RegistroError Crear(
        Exception excepcion,
        string descripcion = "Error no controlado en GET /api/v1/aulas",
        string? metodo = "GET",
        string? ruta = "/api/v1/aulas",
        string traceId = TraceId) =>
        new(Fecha, OrigenError.Peticion, descripcion, excepcion, metodo, ruta, 7, Rol.Administrador, traceId);

    [Fact(DisplayName = "Una excepción simple guarda todos los datos")]
    public void Constructor_SimpleException_StoresAllData()
    {
        var registro = Crear(new InvalidOperationException("Algo falló"));

        Assert.Equal(Fecha, registro.Fecha);
        Assert.Equal(OrigenError.Peticion, registro.Origen);
        Assert.Equal("Error no controlado en GET /api/v1/aulas", registro.Descripcion);
        Assert.Equal("System.InvalidOperationException", registro.TipoExcepcion);
        Assert.Equal("Algo falló", registro.Mensaje);
        Assert.StartsWith("System.InvalidOperationException: Algo falló", registro.Detalle, StringComparison.Ordinal);
        Assert.Equal("GET", registro.Metodo);
        Assert.Equal("/api/v1/aulas", registro.Ruta);
        Assert.Equal(7, registro.UsuarioId);
        Assert.Equal(Rol.Administrador, registro.UsuarioRol);
        Assert.Equal(TraceId, registro.TraceId);
    }

    [Fact(DisplayName = "El detalle incluye la excepción interna")]
    public void Constructor_WithInnerException_DetalleIncludesIt()
    {
        var interna = new TimeoutException("Sin respuesta del servidor");
        var registro = Crear(new InvalidOperationException("Falló el guardado", interna));

        Assert.Equal("Falló el guardado", registro.Mensaje);
        Assert.Contains("System.TimeoutException: Sin respuesta del servidor", registro.Detalle, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "El detalle de una excepción lanzada incluye la secuencia de llamadas")]
    public void Constructor_ThrownException_DetalleIncludesStackTrace()
    {
        Exception capturada;
        try
        {
            throw new InvalidOperationException("Con stack trace");
        }
        catch (InvalidOperationException e)
        {
            capturada = e;
        }

        var registro = Crear(capturada);

        Assert.Contains(nameof(Constructor_ThrownException_DetalleIncludesStackTrace), registro.Detalle, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Los textos largos se recortan a sus máximos")]
    public void Constructor_LongTexts_TruncatesToMaximums()
    {
        var mensaje = new string('m', RegistroError.MaxMensaje + 100);
        var interna = new InvalidOperationException(new string('d', RegistroError.MaxDetalle));
        var registro = Crear(
            new InvalidOperationException(mensaje, interna),
            descripcion: new string('x', RegistroError.MaxDescripcion + 1),
            ruta: "/" + new string('r', RegistroError.MaxRuta));

        Assert.Equal(RegistroError.MaxDescripcion, registro.Descripcion.Length);
        Assert.Equal(RegistroError.MaxMensaje, registro.Mensaje.Length);
        Assert.Equal(RegistroError.MaxDetalle, registro.Detalle.Length);
        Assert.Equal(RegistroError.MaxRuta, registro.Ruta!.Length);
        Assert.EndsWith("…", registro.Detalle, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Sin método ni ruta quedan nulos")]
    public void Constructor_WithoutMetodoOrRuta_LeavesThemNull()
    {
        var registro = new RegistroError(
            Fecha, OrigenError.Correo, "No se pudo enviar el correo de temporal a ana@correo.com",
            new InvalidOperationException("SMTP caído"), null, "", null, null, TraceId);

        Assert.Null(registro.Metodo);
        Assert.Null(registro.Ruta);
        Assert.Null(registro.UsuarioId);
        Assert.Null(registro.UsuarioRol);
        Assert.Equal(OrigenError.Correo, registro.Origen);
    }

    [Theory(DisplayName = "Una descripción vacía lanza una excepción")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyDescripcion_Throws(string descripcion)
    {
        Assert.Throws<ArgumentException>(() => Crear(new InvalidOperationException("x"), descripcion: descripcion));
    }

    [Theory(DisplayName = "Un identificador de operación vacío lanza una excepción")]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyTraceId_Throws(string traceId)
    {
        Assert.Throws<ArgumentException>(() => Crear(new InvalidOperationException("x"), traceId: traceId));
    }

    [Fact(DisplayName = "Crear el registro sin excepción lanza una excepción")]
    public void Constructor_WithoutException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Crear(null!));
    }
}
