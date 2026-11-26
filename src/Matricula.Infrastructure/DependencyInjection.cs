using Matricula.Application.Abstractions;
using Matricula.Infrastructure.Email;
using Matricula.Infrastructure.Persistence;
using Matricula.Infrastructure.Persistence.Audit;
using Matricula.Infrastructure.Persistence.ErrorLog;
using Matricula.Infrastructure.Persistence.Interceptors;
using Matricula.Infrastructure.Seed;
using Matricula.Infrastructure.Seed.DatosPrueba;
using Matricula.Infrastructure.Security;
using Matricula.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matricula.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Matricula")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Matricula'.");

        services.AddSingleton(TimeProvider.System);
        services.AddOptions<InstituteOptions>()
            .Bind(configuration.GetSection(InstituteOptions.SectionName))
            .Validate(
                options => options.HasValidTimeZone(),
                "La opción 'Instituto:ZonaHoraria' debe ser una zona horaria válida, por ejemplo America/Costa_Rica.")
            .ValidateOnStart();
        services.AddSingleton<ILocalClock, LocalClock>();
        services.AddSingleton<IPasswordHashService, PasswordHashService>();
        services.AddSingleton<IPasswordGenerator, PasswordGenerator>();
        services.AddScoped<TimestampsInterceptor>();
        services.AddScoped<AuditCapture>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            // Tablas y columnas en snake_case (estudiante, fecha_creacion), la convención de PostgreSQL:
            // evita tener que poner comillas en los nombres al escribir SQL a mano.
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<TimestampsInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IErrorRecorder, DbErrorRecorder>();
        services.AddOptions<ErrorLogOptions>()
            .Bind(configuration.GetSection(ErrorLogOptions.SectionName))
            .Validate(options => options.DiasRetencion >= 1, "La opción 'Errores:DiasRetencion' debe ser 1 o mayor.")
            .ValidateOnStart();
        services.AddHostedService<ErrorLogPurgeService>();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddOptions<AdminSeedOptions>().Bind(configuration.GetSection(AdminSeedOptions.SectionName));
        services.AddScoped<AdminSeedService>();

        // Sin ValidateOnStart: las opciones se validan solo cuando la siembra va a ejecutarse (spec datos-prueba).
        services.AddOptions<DatosPruebaOptions>().Bind(configuration.GetSection(DatosPruebaOptions.SectionName));
        services.AddScoped<DatosPruebaSeedService>();

        return services;
    }
}
