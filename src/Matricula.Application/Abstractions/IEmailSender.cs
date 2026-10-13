namespace Matricula.Application.Abstractions;

public interface IEmailSender
{
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken = default);
}
