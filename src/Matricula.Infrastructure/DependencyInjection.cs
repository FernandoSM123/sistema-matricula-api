using Matricula.Application.Abstractions;
using Matricula.Infrastructure.Email;
using Matricula.Infrastructure.Persistence;
using Matricula.Infrastructure.Persistence.Auditoria;
using Matricula.Infrastructure.Persistence.Errores;
using Matricula.Infrastructure.Persistence.Interceptors;
using Matricula.Infrastructure.Seed;
using Matricula.Infrastructure.Seguridad;
using Matricula.Infrastructure.Tiempo;
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
        services.AddOptions<InstitutoOptions>()
            .Bind(configuration.GetSection(InstitutoOptions.SectionName))
            .Validate(
                options => options.TieneZonaValida(),
                "La opción 'Instituto:ZonaHoraria' debe ser una zona horaria válida, por ejemplo America/Costa_Rica.")
            .ValidateOnStart();
        services.AddSingleton<IFechaLocal, FechaLocal>();
        services.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        services.AddSingleton<IGeneradorContrasenas, GeneradorContrasenas>();
        services.AddScoped<FechasInterceptor>();
        services.AddScoped<CapturaAuditoria>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            // Tablas y columnas en snake_case (estudiante, fecha_creacion), la convención de PostgreSQL:
            // evita tener que poner comillas en los nombres al escribir SQL a mano.
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<FechasInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IRegistroErrores, RegistroErroresEnBd>();
        services.AddOptions<ErroresOptions>()
            .Bind(configuration.GetSection(ErroresOptions.SectionName))
            .Validate(options => options.DiasRetencion >= 1, "La opción 'Errores:DiasRetencion' debe ser 1 o mayor.")
            .ValidateOnStart();
        services.AddHostedService<DepuracionErrores>();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddOptions<AdminSeedOptions>().Bind(configuration.GetSection(AdminSeedOptions.SectionName));
        services.AddScoped<AdminSeedService>();

        return services;
    }
}
