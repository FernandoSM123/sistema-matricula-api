using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Matricula.Api.Seguridad;

/// <summary>
/// Rate limiting por IP de los endpoints anónimos sensibles a fuerza bruta.
/// </summary>
public static class LimitesPeticiones
{
    public const string Login = "login";
    public const string OlvidoContrasena = "olvido-contrasena";

    public static IServiceCollection AddLimitesPeticiones(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Login, context => PorIp(context, limite: 10, ventana: TimeSpan.FromMinutes(1)));
            options.AddPolicy(OlvidoContrasena, context => PorIp(context, limite: 5, ventana: TimeSpan.FromMinutes(15)));
            options.OnRejected = ResponderAsync;
        });

        return services;
    }

    private static RateLimitPartition<string> PorIp(HttpContext context, int limite, TimeSpan ventana) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = ventana, QueueLimit = 0 });

    private static async ValueTask ResponderAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var reintentarEn))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(reintentarEn.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Demasiadas solicitudes",
                Detail = "Realizó demasiados intentos seguidos. Espere unos minutos e intente de nuevo."
            }
        });
    }
}