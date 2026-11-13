using Matricula.Domain.Entities;
using Matricula.Domain.Reglas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matricula.Infrastructure.Persistence.Errores;

/// <summary>
/// Elimina los registros de error vencidos (RN-49) al arrancar y después cada 24 horas. Es la única escritura fuera del
/// change tracker que se permite: registro_error no se audita (docs/arquitectura.md §5 y §5.2).
/// </summary>
public partial class DepuracionErrores(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<ErroresOptions> opciones,
    ILogger<DepuracionErrores> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Al arrancar, para no esperar un día después de cada reinicio (supuesto 9).
        await DepurarAsync(stoppingToken);

        using var timer = new PeriodicTimer(Intervalo, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DepurarAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La aplicación se está deteniendo.
        }
    }

    private async Task DepurarAsync(CancellationToken stoppingToken)
    {
        try
        {
            var limite = ReglasErrores.LimiteRetencion(timeProvider.GetUtcNow(), opciones.Value.DiasRetencion);

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var eliminados = await db.Set<RegistroError>()
                .Where(e => e.Fecha < limite)
                .ExecuteDeleteAsync(stoppingToken);

            LogDepuracion(logger, eliminados, limite);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La aplicación se está deteniendo.
        }
#pragma warning disable CA1031 // Una BD caída no debe detener la API: el siguiente tick lo vuelve a intentar.
        catch (Exception e)
#pragma warning restore CA1031
        {
            LogFalloDepuracion(logger, e);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Depuración de errores: {Eliminados} registros anteriores a {Limite} eliminados")]
    private static partial void LogDepuracion(ILogger logger, int eliminados, DateTimeOffset limite);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo depurar los registros de error")]
    private static partial void LogFalloDepuracion(ILogger logger, Exception exception);
}
