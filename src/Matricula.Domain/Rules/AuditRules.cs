using System.Text.Json;
using System.Text.Json.Serialization;
using Matricula.Domain.Common;
using Matricula.Domain.Entities;

namespace Matricula.Domain.Rules;

/// <summary>
/// Qué se guarda de cada cambio en la auditoría y con qué formato (RN-44, RN-46). Es lógica pura: quien conoce EF
/// le pasa los nombres y valores de las propiedades escalares de la entidad.
/// </summary>
public static class AuditRules
{
    /// <summary>Valor que reemplaza a un dato secreto que tiene valor (RN-44).</summary>
    public const string Mask = "***";

    // Id ya está en EntidadId; las fechas las asigna el interceptor y la auditoría tiene su propia fecha (RN-46).
    private static readonly HashSet<string> ExcludedForAll =
    [
        nameof(EntidadBase.Id),
        nameof(EntidadBase.FechaCreacion),
        nameof(EntidadBase.FechaModificacion)
    ];

    private static readonly Dictionary<string, HashSet<string>> ExcludedByEntity = new()
    {
        [nameof(Usuario)] = [nameof(Usuario.UltimoIngreso), nameof(Usuario.SecurityStamp)],
        [nameof(Grupo)] = [nameof(Grupo.Version)]
    };

    private static readonly Dictionary<string, HashSet<string>> MaskedByEntity = new()
    {
        [nameof(Usuario)] = [nameof(Usuario.PasswordHash), nameof(Usuario.TempPasswordHash)]
    };

    // Las mismas convenciones que el JSON de la API: claves en camelCase y enums como texto.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Supuesto 7 de la spec: las clases del dominio que heredan de EntidadBase. Una entidad nueva queda auditada sin
    /// tocar esta lista.
    /// </summary>
    public static IReadOnlyList<string> AuditedEntities { get; } = typeof(EntidadBase).Assembly
        .GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(EntidadBase)))
        .Select(t => t.Name)
        .Order(StringComparer.Ordinal)
        .ToArray();

    /// <summary>
    /// El nombre exacto de una entidad auditada, sin distinguir mayúsculas, o null si no existe.
    /// </summary>
    public static string? FindEntity(string? nombre) =>
        AuditedEntities.FirstOrDefault(e => string.Equals(e, nombre?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsExcluded(string entidad, string propiedad) =>
        ExcludedForAll.Contains(propiedad)
        || (ExcludedByEntity.TryGetValue(entidad, out var excluidas) && excluidas.Contains(propiedad));

    public static bool IsMasked(string entidad, string propiedad) =>
        MaskedByEntity.TryGetValue(entidad, out var enmascaradas) && enmascaradas.Contains(propiedad);

    /// <summary>
    /// Creación o eliminación: todos los valores auditables, como <c>{ "estado": "Activa", ... }</c>. Siempre devuelve
    /// un JSON, aunque quede vacío, porque una creación o una eliminación siempre se registra.
    /// </summary>
    public static string Values(string entidad, IEnumerable<KeyValuePair<string, object?>> valores)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entidad);
        ArgumentNullException.ThrowIfNull(valores);

        var resultado = new Dictionary<string, object?>();
        foreach (var (nombre, valor) in valores)
        {
            if (!IsExcluded(entidad, nombre))
            {
                resultado[ToKey(nombre)] = ToValue(entidad, nombre, valor);
            }
        }

        return JsonSerializer.Serialize(resultado, SerializerOptions);
    }

    /// <summary>
    /// Modificación: solo las propiedades auditables cuyo valor cambió, como
    /// <c>{ "estado": { "antes": "Activa", "despues": "Retirada" } }</c>. Devuelve null si no queda ninguna: esa
    /// modificación no se registra (RN-46).
    /// </summary>
    public static string? Differences(string entidad, IEnumerable<AuditedProperty> propiedades)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entidad);
        ArgumentNullException.ThrowIfNull(propiedades);

        var resultado = new Dictionary<string, ChangedValue>();
        foreach (var propiedad in propiedades)
        {
            // Se compara el valor real, antes de enmascararlo: dos hashes distintos son un cambio aunque los dos se
            // muestren como "***".
            if (IsExcluded(entidad, propiedad.Nombre) || Equals(propiedad.Antes, propiedad.Despues))
            {
                continue;
            }

            resultado[ToKey(propiedad.Nombre)] = new ChangedValue(
                ToValue(entidad, propiedad.Nombre, propiedad.Antes),
                ToValue(entidad, propiedad.Nombre, propiedad.Despues));
        }

        return resultado.Count == 0 ? null : JsonSerializer.Serialize(resultado, SerializerOptions);
    }

    private static string ToKey(string nombre) => JsonNamingPolicy.CamelCase.ConvertName(nombre);

    private static object? ToValue(string entidad, string nombre, object? valor) =>
        valor is not null && IsMasked(entidad, nombre) ? Mask : valor;

    private sealed record ChangedValue(object? Antes, object? Despues);
}