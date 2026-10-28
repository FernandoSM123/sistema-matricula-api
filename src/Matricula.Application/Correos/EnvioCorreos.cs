using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matricula.Application.Correos;

/// <summary>
/// Envía los correos de cuentas después de guardar. Un fallo de envío no revierte la operación:
/// se registra (sin el contenido del correo) y se informa con el valor devuelto.
/// </summary>
public partial class EnvioCorreos(IEmailSender emailSender, IAppDbContext db, ILogger<EnvioCorreos> logger)
{
    public async Task<bool> EnviarTemporalAsync(Usuario usuario, string temporal, MotivoTemporal motivo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var nombre = await ObtenerNombreAsync(usuario.Id, cancellationToken);
        return await EnviarAsync(usuario.Correo, PlantillasCorreo.Temporal(nombre, temporal, motivo), "temporal", cancellationToken);
    }

    /// <summary>RN-33.</summary>
    public async Task<bool> EnviarAvisoCambioContrasenaAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var nombre = await ObtenerNombreAsync(usuario.Id, cancellationToken);
        return await EnviarAsync(usuario.Correo, PlantillasCorreo.AvisoCambioContrasena(nombre), "aviso de cambio", cancellationToken);
    }

    // El módulo profesores añade aquí el nombre de sus personas.
    private async Task<string?> ObtenerNombreAsync(int usuarioId, CancellationToken cancellationToken) =>
        await db.Administradores.AsNoTracking()
            .Where(a => a.UsuarioId == usuarioId)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync(cancellationToken)
        ?? await db.Estudiantes.AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId)
            .Select(e => e.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> EnviarAsync(string destinatario, CorreoPlantilla correo, string tipo, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.EnviarAsync(destinatario, correo.Asunto, correo.CuerpoHtml, cancellationToken);
            return true;
        }
#pragma warning disable CA1031 // Cualquier fallo del proveedor de correo se informa sin revertir lo ya guardado.
        catch (Exception e) when (e is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogFalloEnvio(logger, e, tipo, destinatario);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo enviar el correo de {Tipo} a {Destinatario}")]
    private static partial void LogFalloEnvio(ILogger logger, Exception exception, string tipo, string destinatario);
}