using System.Text.Json;
using Matricula.Domain.Enums;

namespace Matricula.Application.Auditoria;

/// <summary>
/// Un cambio registrado (§5.12). Un autor null es el Sistema (RN-45). <see cref="Cambios"/> es un objeto JSON: en una
/// modificación, <c>{ "campo": { "antes": ..., "despues": ... } }</c>; en una creación o eliminación, los valores.
/// </summary>
public record RegistroAuditoriaResponse(
    long Id,
    DateTimeOffset Fecha,
    AccionAuditoria Accion,
    string Entidad,
    int EntidadId,
    int? UsuarioId,
    string? UsuarioCorreo,
    Rol? UsuarioRol,
    string OperacionId,
    JsonElement Cambios);