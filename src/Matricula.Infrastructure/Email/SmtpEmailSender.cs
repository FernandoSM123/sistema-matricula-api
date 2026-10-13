using MailKit.Net.Smtp;
using MailKit.Security;
using Matricula.Application.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Matricula.Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken = default)
    {
        using var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.NombreRemitente, _options.Remitente));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new BodyBuilder { HtmlBody = cuerpoHtml }.ToMessageBody();

        using var cliente = new SmtpClient();
        var seguridad = _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await cliente.ConnectAsync(_options.Host, _options.Port, seguridad, cancellationToken);

        if (!string.IsNullOrEmpty(_options.Usuario))
        {
            await cliente.AuthenticateAsync(_options.Usuario, _options.Contrasena ?? string.Empty, cancellationToken);
        }

        await cliente.SendAsync(mensaje, cancellationToken);
        await cliente.DisconnectAsync(true, cancellationToken);
    }
}
