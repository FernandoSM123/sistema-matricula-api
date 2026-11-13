using Matricula.Domain.Enums;

namespace Matricula.Application.Errores;

/// <summary>
/// Un error con su detalle técnico (§5.13): la excepción completa, con las internas y el stack trace.
/// </summary>
public record RegistroErrorDetalleResponse(
    long Id,
    DateTimeOffset Fecha,
    OrigenError Origen,
    string Descripcion,
    string TipoExcepcion,
    string Mensaje,
    string? Metodo,
    string? Ruta,
    int? UsuarioId,
    Rol? UsuarioRol,
    string TraceId,
    string Detalle);
