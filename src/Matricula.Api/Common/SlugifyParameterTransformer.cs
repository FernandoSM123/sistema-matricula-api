using System.Text.RegularExpressions;

namespace Matricula.Api.Common;

/// <summary>
/// Convierte los tokens de ruta a kebab-case: el controller <c>CambioGrupo</c> responde en <c>/api/v1/cambio-grupo</c>.
/// </summary>
public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    private static readonly Regex InicioDePalabra = new("([a-z0-9])([A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string? TransformOutbound(object? value) =>
        value?.ToString() is { } texto
            ? InicioDePalabra.Replace(texto, "$1-$2").ToLowerInvariant()
            : null;
}
