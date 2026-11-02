using FluentValidation;
using Matricula.Application.Administradores;
using Matricula.Application.Aulas;
using Matricula.Application.Auth;
using Matricula.Application.Correos;
using Matricula.Application.Cuentas;
using Matricula.Application.Cursos;
using Matricula.Application.Estudiantes;
using Matricula.Application.Profesores;
using Microsoft.Extensions.DependencyInjection;

namespace Matricula.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<EnvioCorreos>();
        services.AddScoped<AuthService>();
        services.AddScoped<CuentaService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<AdministradorService>();
        services.AddScoped<EstudianteService>();
        services.AddScoped<ProfesorService>();
        services.AddScoped<CursoService>();
        services.AddScoped<AulaService>();

        return services;
    }
}
