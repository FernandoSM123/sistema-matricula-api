using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Matricula.Infrastructure.Seed;

/// <summary>
/// Crea el primer administrador al arrancar si no existe ninguno. Mientras ese administrador no defina su
/// contraseña, renueva la temporal en cada arranque para que un vencimiento no deje el sistema sin acceso.
/// </summary>
public partial class AdminSeedService(
    AppDbContext db,
    IOptions<AdminSeedOptions> options,
    IHasherContrasenas hasher,
    TimeProvider timeProvider,
    ILogger<AdminSeedService> logger)
{
    private readonly AdminSeedOptions _options = options.Value;

    public async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        bool hayAdministradores;
        try
        {
            hayAdministradores = await db.Usuarios.AnyAsync(u => u.Rol == Rol.Administrador, cancellationToken);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            throw new InvalidOperationException(
                "La base de datos no tiene las tablas del sistema. Aplique las migraciones con " +
                "'dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api'.", e);
        }

        if (!hayAdministradores)
        {
            await CrearAdministradorInicialAsync(cancellationToken);
            return;
        }

        await RenovarTemporalSiNoTieneContrasenaAsync(cancellationToken);
    }

    private async Task CrearAdministradorInicialAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ContrasenaTemporal))
        {
            throw new InvalidOperationException(
                "No existe ningún administrador y falta la contraseña temporal del administrador inicial. " +
                "Defina 'AdminSeed:ContrasenaTemporal' en user-secrets o en la variable de entorno AdminSeed__ContrasenaTemporal.");
        }

        var correo = Usuario.NormalizarCorreo(_options.Correo);
        var usuario = new Usuario(correo, Rol.Administrador, OrigenRegistro.Administrador);
        usuario.AsignarTemporal(hasher.Hashear(_options.ContrasenaTemporal), MotivoTemporal.CuentaNueva, timeProvider.GetUtcNow());

        db.Administradores.Add(new Administrador(usuario, _options.Cedula, _options.Nombre, _options.Apellidos, _options.Telefono));
        await db.SaveChangesAsync(cancellationToken);

        LogAdministradorInicialCreado(logger, correo);
    }

    private async Task RenovarTemporalSiNoTieneContrasenaAsync(CancellationToken cancellationToken)
    {
        // El administrador inicial es el único creado sin un administrador que lo registre.
        var inicial = await db.Usuarios.SingleOrDefaultAsync(
            u => u.Rol == Rol.Administrador && u.CreadoPorId == null && u.PasswordHash == null, cancellationToken);

        if (inicial is null)
        {
            return;
        }

        var correo = inicial.Correo;
        if (string.IsNullOrWhiteSpace(_options.ContrasenaTemporal))
        {
            LogSinTemporalParaRenovar(logger, correo);
            return;
        }

        inicial.AsignarTemporal(hasher.Hashear(_options.ContrasenaTemporal), MotivoTemporal.CuentaNueva, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        LogTemporalRenovada(logger, correo);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Administrador inicial creado con el correo {Correo}")]
    private static partial void LogAdministradorInicialCreado(ILogger logger, string correo);

    [LoggerMessage(Level = LogLevel.Information, Message = "Temporal del administrador inicial {Correo} renovada")]
    private static partial void LogTemporalRenovada(ILogger logger, string correo);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "El administrador inicial {Correo} aún no define su contraseña y no hay 'AdminSeed:ContrasenaTemporal' para renovar su temporal")]
    private static partial void LogSinTemporalParaRenovar(ILogger logger, string correo);
}