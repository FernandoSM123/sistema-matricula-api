using FluentValidation;
using Matricula.Application.Auth;
using Matricula.Application.Correos;
using Microsoft.Extensions.DependencyInjection;

namespace Matricula.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<EnvioCorreos>();
        services.AddScoped<AuthService>();

        return services;
    }
}
