using System.Text.Json.Serialization;
using Matricula.Api.Common;
using Matricula.Api.Services;
using Matricula.Application;
using Matricula.Application.Abstractions;
using Matricula.Infrastructure;
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
        options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Sin orígenes configurados no se permite ninguno.
var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(origenesPermitidos)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Pendiente (paso 4 de docs/arquitectura.md): autenticación JWT, políticas de autorización,
// rate limiting en login y olvido de contraseña, y seed del primer administrador.

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
