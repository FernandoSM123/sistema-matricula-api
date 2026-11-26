using System.Diagnostics;
using Matricula.Application.Abstractions;
using Matricula.Application.Errores;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Matricula.Infrastructure.Persistence.ErrorLog;

/// <summary>
/// Guarda los errores inesperados en registro_error (RF-58), con un AppDbContext propio y sin lanzar excepciones
/// (RN-48). Detalle en docs/arquitectura.md §5.2.
/// </summary>
public partial class DbErrorRecorder(
    IServiceScopeFactory scopeFactory,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<DbErrorRecorder> logger) : IErrorRecorder
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task RecordAsync(UnexpectedError errorInesperado)
    {
        // Antes del try: lo necesita el log si el registro falla.
        var traceId = CurrentTraceId();

        try
        {
            // Dentro del try: un argumento nulo tampoco puede afectar la respuesta (RN-48).
            ArgumentNullException.ThrowIfNull(errorInesperado);

            Rol? rol = Enum.TryParse<Rol>(currentUser.Rol, out var leido) ? leido : null;
            var registro = new RegistroError(
                timeProvider.GetUtcNow(), errorInesperado.Origen, errorInesperado.Descripcion, errorInesperado.Excepcion,
                errorInesperado.Metodo, errorInesperado.Ruta, currentUser.UsuarioId, rol, traceId);

            // Contexto propio: el de la petición tiene los cambios que fallaron y volvería a intentar guardarlos.
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            using var timeout = new CancellationTokenSource(Timeout);

            db.Set<RegistroError>().Add(registro);
            await db.SaveChangesAsync(timeout.Token);
        }
#pragma warning disable CA1031 // RN-48: registrar un error nunca puede afectar la respuesta.
        catch (Exception e)
#pragma warning restore CA1031
        {
            LogRecordFailed(logger, e, traceId);
        }
    }

    // El mismo cálculo que el OperacionId de la auditoría (AuditCapture).
    private static string CurrentTraceId() =>
        Activity.Current is { TraceId: var traceId } && traceId != default
            ? traceId.ToHexString()
            : ActivityTraceId.CreateRandom().ToHexString();

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo registrar el error {TraceId}")]
    private static partial void LogRecordFailed(ILogger logger, Exception exception, string traceId);
}
