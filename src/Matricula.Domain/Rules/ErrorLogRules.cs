namespace Matricula.Domain.Rules;

/// <summary>
/// Reglas puras del registro de errores: recorte de textos, formato del identificador de operación y límite de
/// conservación (RN-47, RN-49).
/// </summary>
public static class ErrorLogRules
{
    private const string Ellipsis = "…";
    private const int TraceIdLength = 32;
    private const int SpanIdLength = 16;

    /// <summary>
    /// Recorta el texto para que mida como máximo <paramref name="maximo"/> caracteres. Si se recorta, termina en "…".
    /// </summary>
    public static string Truncate(string? texto, int maximo)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximo, 1);

        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        return texto.Length <= maximo ? texto : string.Concat(texto.AsSpan(0, maximo - 1), Ellipsis);
    }

    /// <summary>
    /// Devuelve el trace id en minúsculas a partir de sus 32 caracteres hexadecimales o del traceId W3C que reciben
    /// los clientes en los ProblemDetails (<c>00-&lt;trace id&gt;-&lt;span id&gt;-&lt;flags&gt;</c>), o null si el
    /// formato no es válido.
    /// </summary>
    public static string? NormalizeTraceId(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var texto = valor.Trim();
        string traceId;

        if (texto.Length == TraceIdLength)
        {
            traceId = texto;
        }
        else
        {
            var partes = texto.Split('-');
            if (partes is not [{ Length: 2 } version, { Length: TraceIdLength } id, { Length: SpanIdLength } span, { Length: 2 } flags]
                || !IsHex(version) || !IsHex(span) || !IsHex(flags))
            {
                return null;
            }

            traceId = id;
        }

        // W3C declara inválido el trace id de puros ceros.
        if (!IsHex(traceId) || traceId.All(c => c == '0'))
        {
            return null;
        }

#pragma warning disable CA1308 // El trace id se guarda en hexadecimal en minúsculas, como lo genera .NET.
        return traceId.ToLowerInvariant();
#pragma warning restore CA1308
    }

    /// <summary>Fecha antes de la cual un registro de error ya venció (RN-49).</summary>
    public static DateTimeOffset RetentionLimit(DateTimeOffset ahora, int diasRetencion)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(diasRetencion, 1);

        return ahora.AddDays(-diasRetencion);
    }

    private static bool IsHex(string texto) => texto.All(char.IsAsciiHexDigit);
}
