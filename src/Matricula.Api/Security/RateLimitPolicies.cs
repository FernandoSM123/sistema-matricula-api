using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Matricula.Api.Security;

/// <summary>
/// Rate limiting por IP de los endpoints anónimos sensibles a fuerza bruta.
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string ForgotPassword = "forgot-password";
    public const string SignUp = "sign-up";
    public const string Refresh = "refresh";

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Login, context => ByIp(context, limite: 10, ventana: TimeSpan.FromMinutes(1)));
            options.AddPolicy(ForgotPassword, context => ByIp(context, limite: 5, ventana: TimeSpan.FromMinutes(15)));
            // Más holgado que el olvido: varios estudiantes pueden registrarse desde la red del instituto.
            options.AddPolicy(SignUp, context => ByIp(context, limite: 10, ventana: TimeSpan.FromMinutes(15)));
            // RF-60: con access tokens de 15 minutos un usuario renueva unas 4 veces por hora; deja margen para muchos
            // usuarios detrás de la IP del instituto (supuesto 4 de la spec de sesiones).
            options.AddPolicy(Refresh, context => ByIp(context, limite: 30, ventana: TimeSpan.FromMinutes(1)));
            options.OnRejected = RespondAsync;
        });

        return services;
    }

    private static RateLimitPartition<string> ByIp(HttpContext context, int limite, TimeSpan ventana) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = ventana, QueueLimit = 0 });

    private static async ValueTask RespondAsync(OnRejectedContext context, CancellationToken cancellationToken)
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