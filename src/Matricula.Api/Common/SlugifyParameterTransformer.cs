using System.Text.RegularExpressions;

namespace Matricula.Api.Common;

/// <summary>
/// Convierte los tokens de ruta a kebab-case: el controller <c>CambioGrupo</c> responde en <c>/api/v1/cambio-grupo</c>.
/// </summary>
public partial class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value) =>
        value?.ToString() is { } texto
            ? InicioDePalabra().Replace(texto, "$1-$2").ToLowerInvariant()
            : null;

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex InicioDePalabra();
}
