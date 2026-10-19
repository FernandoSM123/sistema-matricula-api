using Microsoft.Extensions.DependencyInjection;

namespace Matricula.Infrastructure.Seed;

public static class SeedExtensions
{
    /// <summary>
    /// Ejecuta el seed del administrador inicial en su propio scope. Se llama al inicio de Program, antes de atender peticiones.
    /// </summary>
    public static async Task SembrarAdministradorInicialAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AdminSeedService>().EjecutarAsync(cancellationToken);
    }
}