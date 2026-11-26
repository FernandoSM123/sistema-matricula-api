using System.Net;
using Matricula.Domain.Enums;

namespace Matricula.Application.Email;

/// <summary>
/// Textos de los correos del sistema. Todo valor variable se codifica como HTML.
/// </summary>
public static class EmailTemplates
{
    public static EmailTemplate TemporaryPassword(string? nombre, string temporal, MotivoTemporal motivo)
    {
        var (asunto, introduccion, vigencia, nota) = motivo switch
        {
            MotivoTemporal.CuentaNueva => (
                "Su cuenta en el Sistema de Matrícula",
                "Se creó una cuenta para usted en el Sistema de Matrícula.",
                "72 horas",
                string.Empty),
            MotivoTemporal.Olvido => (
                "Contraseña temporal para ingresar",
                "Recibimos una solicitud de contraseña temporal para su cuenta.",
                "1 hora",
                "<p>Su contraseña actual sigue funcionando. Si no solicitó este cambio, ignore este correo.</p>"),
            _ => (
                "Nueva contraseña temporal",
                "La administración generó una nueva contraseña temporal para su cuenta.",
                "72 horas",
                "<p>Cualquier contraseña temporal anterior dejó de funcionar.</p>")
        };

        var cuerpo = $"""
            <p>{Greeting(nombre)}</p>
            <p>{introduccion}</p>
            <p>Su contraseña temporal es: <strong>{WebUtility.HtmlEncode(temporal)}</strong></p>
            <p>Vence en {vigencia}. Al ingresar con ella deberá definir una nueva contraseña.</p>
            {nota}
            """;

        return new EmailTemplate(asunto, cuerpo);
    }

    public static EmailTemplate PasswordChanged(string? nombre)
    {
        var cuerpo = $"""
            <p>{Greeting(nombre)}</p>
            <p>La contraseña de su cuenta en el Sistema de Matrícula se cambió.</p>
            <p>Si no fue usted, contacte a la administración de inmediato.</p>
            """;

        return new EmailTemplate("Su contraseña fue cambiada", cuerpo);
    }

    private static string Greeting(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre) ? "Hola:" : $"Hola, {WebUtility.HtmlEncode(nombre)}:";
}