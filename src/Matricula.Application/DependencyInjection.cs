using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Matricula.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Registrar aquí los servicios de cada funcionalidad, por ejemplo:
        // services.AddScoped<EstudianteService>();

        return services;
    }
}
