using System.Text.Json;
using System.Text.Json.Serialization;

namespace Matricula.Api.Common;

/// <summary>
/// Opciones JSON comunes. Se aplican a las de MVC (lo que leen y escriben los controllers) y a las HTTP
/// (las que usan el generador del OpenAPI y los ProblemDetails de IProblemDetailsService). Si se separan,
/// el contrato publicado deja de coincidir con lo que la API envía y acepta (COR-03).
/// </summary>
public static class JsonSettings
{
    public static void Configure(JsonSerializerOptions opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);

        opciones.Converters.Add(new JsonStringEnumConverter());
        // Los valores web por defecto aceptan números escritos como texto, y el OpenAPI los declara integer | string.
        opciones.NumberHandling = JsonNumberHandling.Strict;
    }
}
