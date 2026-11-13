using Matricula.Domain.Enums;
using Matricula.Domain.Reglas;

namespace Matricula.Domain.Entities;

/// <summary>
/// Constancia de un error inesperado (RF-58, §5.13). No hereda de EntidadBase porque nunca se modifica: solo se
/// elimina al vencer (RN-49). No tiene relaciones, y nunca guarda el contenido de la petición (RN-47).
/// </summary>
public class RegistroError
{
    public const int MaxDescripcion = 500;
    public const int MaxTipoExcepcion = 300;
    public const int MaxMensaje = 2000;
    public const int MaxDetalle = 32_000;
    public const int MaxMetodo = 10;
    public const int MaxRuta = 500;
    public const int MaxTraceId = 32;

    // Para EF Core.
    private RegistroError()
    {
        Descripcion = TipoExcepcion = Mensaje = Detalle = TraceId = string.Empty;
    }

    public RegistroError(
        DateTimeOffset fecha,
        OrigenError origen,
        string descripcion,
        Exception excepcion,
        string? metodo,
        string? ruta,
        int? usuarioId,
        Rol? usuarioRol,
        string traceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);
        ArgumentNullException.ThrowIfNull(excepcion);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        Fecha = fecha;
        Origen = origen;
        Descripcion = ReglasErrores.Truncar(descripcion, MaxDescripcion);
        TipoExcepcion = ReglasErrores.Truncar(excepcion.GetType().FullName, MaxTipoExcepcion);
        Mensaje = ReglasErrores.Truncar(excepcion.Message, MaxMensaje);
        // Incluye las excepciones internas y el stack trace.
        Detalle = ReglasErrores.Truncar(excepcion.ToString(), MaxDetalle);
        Metodo = string.IsNullOrEmpty(metodo) ? null : ReglasErrores.Truncar(metodo, MaxMetodo);
        Ruta = string.IsNullOrEmpty(ruta) ? null : ReglasErrores.Truncar(ruta, MaxRuta);
        UsuarioId = usuarioId;
        UsuarioRol = usuarioRol;
        TraceId = ReglasErrores.Truncar(traceId, MaxTraceId);
    }

    public long Id { get; private set; }

    /// <summary>Momento del error, en UTC.</summary>
    public DateTimeOffset Fecha { get; private set; }

    public OrigenError Origen { get; private set; }

    /// <summary>Resumen legible, por ejemplo "Error no controlado en GET /api/v1/aulas".</summary>
    public string Descripcion { get; private set; }

    /// <summary>Nombre completo de la clase de la excepción.</summary>
    public string TipoExcepcion { get; private set; }

    public string Mensaje { get; private set; }

    /// <summary>La excepción completa, con las internas y el stack trace. Nunca se muestra al usuario.</summary>
    public string Detalle { get; private set; }

    /// <summary>Solo en los errores de una petición.</summary>
    public string? Metodo { get; private set; }

    /// <summary>Solo en los errores de una petición. Sin query string (RN-47).</summary>
    public string? Ruta { get; private set; }

    /// <summary>Cuenta con sesión cuando ocurrió el error; null si no había sesión. Sin FK.</summary>
    public int? UsuarioId { get; private set; }

    public Rol? UsuarioRol { get; private set; }

    /// <summary>El mismo valor que el OperacionId de la auditoría y que forma parte del traceId de los ProblemDetails.</summary>
    public string TraceId { get; private set; }
}
