using System.Net;
using Matricula.Domain.Enums;

namespace Matricula.Application.Correos;

/// <summary>
/// Textos de los correos del sistema. Todo valor variable se codifica como HTML.
/// </summary>
public static class PlantillasCorreo
{
    public static CorreoPlantilla Temporal(string? nombre, string temporal, MotivoTemporal motivo)
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
            <p>{Saludo(nombre)}</p>
            <p>{introduccion}</p>
            <p>Su contraseña temporal es: <strong>{WebUtility.HtmlEncode(temporal)}</strong></p>
            <p>Vence en {vigencia}. Al ingresar con ella deberá definir una nueva contraseña.</p>
            {nota}
            """;

        return new CorreoPlantilla(asunto, cuerpo);
    }

    public static CorreoPlantilla AvisoCambioContrasena(string? nombre)
    {
        var cuerpo = $"""
            <p>{Saludo(nombre)}</p>
            <p>La contraseña de su cuenta en el Sistema de Matrícula se cambió.</p>
            <p>Si no fue usted, contacte a la administración de inmediato.</p>
            """;

        return new CorreoPlantilla("Su contraseña fue cambiada", cuerpo);
    }

    private static string Saludo(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre) ? "Hola:" : $"Hola, {WebUtility.HtmlEncode(nombre)}:";
}