using Matricula.Infrastructure.Seed.TestData;
using Microsoft.Extensions.DependencyInjection;

namespace Matricula.Infrastructure.Seed;

public static class SeedExtensions
{
    /// <summary>
    /// Ejecuta el seed del administrador inicial en su propio scope. Se llama al inicio de Program, antes de atender peticiones.
    /// </summary>
    public static async Task SeedInitialAdministradorAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AdminSeedService>().RunAsync(cancellationToken);
    }

    /// <summary>
    /// Siembra los datos de prueba en su propio scope, si están habilitados (spec datos-prueba). Se llama después del
    /// seed del administrador. El entorno lo indica Program, para que Infrastructure no dependa del host.
    /// </summary>
    public static async Task SeedTestDataAsync(
        this IServiceProvider services, bool esDesarrollo, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDataSeedService>().RunAsync(esDesarrollo, cancellationToken);
    }
}