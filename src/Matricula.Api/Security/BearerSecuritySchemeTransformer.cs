using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Matricula.Api.Security;

/// <summary>
/// Declara el esquema Bearer en el documento OpenAPI, para que Scalar permita enviar el token.
/// </summary>
public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string Scheme = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[Scheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token obtenido con POST /api/v1/auth/login."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(Scheme, document)] = []
        });

        return Task.CompletedTask;
    }
}