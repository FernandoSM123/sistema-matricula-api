using Matricula.Application.Abstractions;
using Matricula.Application.Errores;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matricula.Application.Email;

/// <summary>
/// Envía los correos de cuentas después de guardar. Un fallo de envío no revierte la operación:
/// se registra (sin el contenido del correo) y se informa con el valor devuelto.
/// </summary>
public partial class EmailDispatcher(
    IEmailSender emailSender,
    IAppDbContext db,
    IErrorRecorder errorRecorder,
    ILogger<EmailDispatcher> logger)
{
    public async Task<bool> SendTemporaryPasswordAsync(Usuario usuario, string temporal, MotivoTemporal motivo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var nombre = await GetNombreAsync(usuario.Id, cancellationToken);
        return await SendAsync(usuario.Correo, EmailTemplates.TemporaryPassword(nombre, temporal, motivo), "temporal", cancellationToken);
    }

    /// <summary>RN-33.</summary>
    public async Task<bool> SendPasswordChangedAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var nombre = await GetNombreAsync(usuario.Id, cancellationToken);
        return await SendAsync(usuario.Correo, EmailTemplates.PasswordChanged(nombre), "aviso de cambio", cancellationToken);
    }

    private async Task<string?> GetNombreAsync(int usuarioId, CancellationToken cancellationToken) =>
        await db.Administradores.AsNoTracking()
            .Where(a => a.UsuarioId == usuarioId)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync(cancellationToken)
        ?? await db.Estudiantes.AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId)
            .Select(e => e.Nombre)
            .FirstOrDefaultAsync(cancellationToken)
        ?? await db.Profesores.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => p.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> SendAsync(string destinatario, EmailTemplate correo, string tipo, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(destinatario, correo.Subject, correo.HtmlBody, cancellationToken);
            return true;
        }
#pragma warning disable CA1031 // Cualquier fallo del proveedor de correo se informa sin revertir lo ya guardado.
        catch (Exception e) when (e is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogSendFailed(logger, e, tipo, destinatario);
            // RF-58: el destinatario sirve para saber a quién reenviar; el contenido nunca se guarda (RN-47).
            await errorRecorder.RecordAsync(new UnexpectedError(
                OrigenError.Correo, $"No se pudo enviar el correo de {tipo} a {destinatario}", e));
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo enviar el correo de {Tipo} a {Destinatario}")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, string tipo, string destinatario);
}