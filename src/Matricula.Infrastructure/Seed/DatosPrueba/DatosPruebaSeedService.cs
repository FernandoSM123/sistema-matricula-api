using Matricula.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Siembra datos de prueba al arrancar (spec datos-prueba): solo con el flag activo, en Development y sobre una BD sin
/// datos de negocio.
/// </summary>
public partial class DatosPruebaSeedService(
    AppDbContext db,
    IOptions<DatosPruebaOptions> options,
    ILogger<DatosPruebaSeedService> logger)
{
    private readonly DatosPruebaOptions _options = options.Value;

    public async Task EjecutarAsync(bool esDesarrollo, CancellationToken cancellationToken)
    {
        if (!_options.Habilitado)
        {
            return;
        }

        if (!esDesarrollo)
        {
            LogFueraDeDesarrollo(logger);
            return;
        }

        var errores = _options.Validar();
        if (errores.Count > 0)
        {
            throw new InvalidOperationException(
                "La configuración de los datos de prueba no es válida:" + Environment.NewLine
                + string.Join(Environment.NewLine, errores.Select(e => "- " + e)));
        }

        if (await HayDatosDeNegocioAsync(cancellationToken))
        {
            LogBdConDatos(logger);
            return;
        }

        var semilla = _options.Semilla ?? Random.Shared.Next();
        LogSiembraPendiente(logger, semilla);
    }

    /// <summary>
    /// El administrador inicial no cuenta: lo crea el seed del administrador antes (supuesto 3 de la spec).
    /// </summary>
    private async Task<bool> HayDatosDeNegocioAsync(CancellationToken cancellationToken) =>
        await db.Cursos.AnyAsync(cancellationToken)
        || await db.Aulas.AnyAsync(cancellationToken)
        || await db.Periodos.AnyAsync(cancellationToken)
        || await db.Profesores.AnyAsync(cancellationToken)
        || await db.Estudiantes.AnyAsync(cancellationToken)
        || await db.Grupos.AnyAsync(cancellationToken)
        || await db.Matriculas.AnyAsync(cancellationToken);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "'DatosPrueba:Habilitado' está activo fuera de Development; no se siembran datos de prueba")]
    private static partial void LogFueraDeDesarrollo(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "La BD ya tiene datos; no se siembran datos de prueba")]
    private static partial void LogBdConDatos(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Se sembrarían datos de prueba con la semilla {Semilla}")]
    private static partial void LogSiembraPendiente(ILogger logger, int semilla);
}
