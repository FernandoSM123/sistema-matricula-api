using MailKit.Net.Smtp;
using MailKit.Security;
using Matricula.Application.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Matricula.Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        mensaje.To.Add(MailboxAddress.Parse(recipient));
        mensaje.Subject = subject;
        mensaje.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var cliente = new SmtpClient();
        var seguridad = _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await cliente.ConnectAsync(_options.Host, _options.Port, seguridad, cancellationToken);

        if (!string.IsNullOrEmpty(_options.User))
        {
            await cliente.AuthenticateAsync(_options.User, _options.Password ?? string.Empty, cancellationToken);
        }

        await cliente.SendAsync(mensaje, cancellationToken);
        await cliente.DisconnectAsync(true, cancellationToken);
    }
}
