using Matricula.Domain.Enums;

namespace Matricula.Application.Errores;

/// <summary>
/// Un error del listado (§5.13), sin el detalle técnico, que se pide con el detalle por id. Un usuario null indica
/// que no había sesión.
/// </summary>
public record RegistroErrorResponse(
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
    string TraceId);
