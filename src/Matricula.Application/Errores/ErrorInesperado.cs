using Matricula.Domain.Enums;

namespace Matricula.Application.Errores;

/// <summary>
/// Datos de un error inesperado que conoce quien lo captura. El usuario, la fecha y el trace id los completa
/// <see cref="Abstractions.IRegistroErrores"/>. Nunca lleva el contenido de la petición (RN-47).
/// </summary>
/// <param name="Ruta">Solo el path, sin query string.</param>
public record ErrorInesperado(
    OrigenError Origen,
    string Descripcion,
    Exception Excepcion,
    string? Metodo = null,
    string? Ruta = null);
