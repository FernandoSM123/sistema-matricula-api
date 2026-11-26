using Matricula.Api.Common;
using Matricula.Api.Security;
using Matricula.Api.Services;
using Matricula.Application;
using Matricula.Application.Abstractions;
using Matricula.Infrastructure;
using Matricula.Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Net.Http.Headers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        // Los obligatorios los valida FluentValidation, con mensajes en español; sin esto, [ApiController]
        // rechaza antes los string no anulables con mensajes en inglés.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
    })
    .AddJsonOptions(options => JsonSettings.Configure(options.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
// El generador del OpenAPI lee estas opciones, no las de MVC (COR-03).
builder.Services.ConfigureHttpJsonOptions(options => JsonSettings.Configure(options.SerializerOptions));
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddSchemaTransformer<NonNullableEnumSchemaTransformer>();
});
builder.Services.AddHealthChecks();

// Sin orígenes configurados no se permite ninguno.
// Retry-After (429) y Location (201) no son legibles desde otro origen si no se exponen (COR-02).
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(origenesPermitidos)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(HeaderNames.RetryAfter, HeaderNames.Location)));

builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddRateLimitPolicies();

var app = builder.Build();

await app.Services.SeedInitialAdministradorAsync(app.Lifetime.ApplicationStopping);
await app.Services.SeedTestDataAsync(app.Environment.IsDevelopment(), app.Lifetime.ApplicationStopping);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();
