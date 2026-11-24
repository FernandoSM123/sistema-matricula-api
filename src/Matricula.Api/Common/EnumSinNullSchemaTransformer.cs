using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Matricula.Api.Common;

/// <summary>
/// Quita el null de la lista de valores de los enums. El generador lo agrega cuando un enum se usa como
/// anulable (Rol? en la auditoría) y queda en el esquema compartido, así que Rol saldría anulable también
/// donde nunca lo es (perfil, login). La nulabilidad ya la declara cada propiedad con su oneOf (COR-03).
/// </summary>
public class EnumSinNullSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        var tipo = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (tipo.IsEnum && schema.Enum is { Count: > 0 } valores)
        {
            for (var i = valores.Count - 1; i >= 0; i--)
            {
                if (valores[i] is null || valores[i]!.GetValueKind() == JsonValueKind.Null)
                {
                    valores.RemoveAt(i);
                }
            }
        }

        return Task.CompletedTask;
    }
}
