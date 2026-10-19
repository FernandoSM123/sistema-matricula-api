using Matricula.Application.Abstractions;
using Matricula.Infrastructure.Email;
using Matricula.Infrastructure.Persistence;
using Matricula.Infrastructure.Persistence.Interceptors;
using Matricula.Infrastructure.Seguridad;
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
        services.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        services.AddSingleton<IGeneradorContrasenas, GeneradorContrasenas>();
        services.AddScoped<AuditoriaInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            // Tablas y columnas en snake_case (estudiante, fecha_creacion), la convención de PostgreSQL:
            // evita tener que poner comillas en los nombres al escribir SQL a mano.
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateOnStart();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
