using System.Text.Json.Serialization;
using Matricula.Api.Common;
using Matricula.Api.Seguridad;
using Matricula.Api.Services;
using Matricula.Application;
using Matricula.Application.Abstractions;
using Matricula.Infrastructure;
using Matricula.Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
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
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = RespuestaModeloInvalido.Crear);
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks();

// Sin orígenes configurados no se permite ninguno.
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(origenesPermitidos)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddSeguridad(builder.Configuration);
builder.Services.AddLimitesPeticiones();

var app = builder.Build();

await app.Services.SembrarAdministradorInicialAsync(app.Lifetime.ApplicationStopping);

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
