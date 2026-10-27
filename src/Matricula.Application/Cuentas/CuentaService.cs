using Matricula.Application.Abstractions;
using Matricula.Application.Correos;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Cuentas;

/// <summary>
/// Creación de cuentas y cambio de correo para los módulos que registran personas (administradores,
/// estudiantes y profesores). Los métodos que crean o modifican no guardan: el llamador guarda la cuenta
/// junto con la persona en un solo SaveChangesAsync y después envía la temporal.
/// </summary>
public class CuentaService(
    IAppDbContext db,
    IHasherContrasenas hasher,
    IGeneradorContrasenas generadorContrasenas,
    ICurrentUser currentUser,
    EnvioCorreos envioCorreos,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Registro público (RF-01): siempre rol Estudiante (RN-19), con la contraseña que definió la persona.
    /// </summary>
    public async Task<Usuario> CrearCuentaPublicaAsync(string correo, string contrasena, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(contrasena);
        await ValidarCorreoDisponibleAsync(correo, exceptoUsuarioId: null, cancellationToken);

        var usuario = new Usuario(correo, Rol.Estudiante, OrigenRegistro.RegistroPublico);
        usuario.EstablecerContrasena(hasher.Hashear(contrasena), timeProvider.GetUtcNow());
        db.Usuarios.Add(usuario);

        return usuario;
    }

    /// <summary>
    /// Cuenta creada por el administrador actual con una temporal de 72 horas (RF-05, RN-22, RN-23).
    /// </summary>
    public async Task<CuentaNueva> CrearCuentaConTemporalAsync(string correo, Rol rol, CancellationToken cancellationToken)
    {
        await ValidarCorreoDisponibleAsync(correo, exceptoUsuarioId: null, cancellationToken);

        var temporal = generadorContrasenas.Generar();
        var usuario = new Usuario(correo, rol, OrigenRegistro.Administrador, currentUser.UsuarioId);
        usuario.AsignarTemporal(hasher.Hashear(temporal), MotivoTemporal.CuentaNueva, timeProvider.GetUtcNow());
        db.Usuarios.Add(usuario);

        return new CuentaNueva(usuario, temporal);
    }

    /// <summary>
    /// RN-31: el correo de la persona es el de su cuenta.
    /// </summary>
    public async Task CambiarCorreoAsync(Usuario usuario, string correo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (Usuario.NormalizarCorreo(correo) == usuario.Correo)
        {
            return;
        }

        await ValidarCorreoDisponibleAsync(correo, usuario.Id, cancellationToken);
        usuario.CambiarCorreo(correo);
    }

    /// <summary>
    /// Se llama después de guardar. Devuelve false si el correo no se pudo enviar.
    /// </summary>
    public Task<bool> EnviarTemporalAsync(CuentaNueva cuenta, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cuenta);

        return envioCorreos.EnviarTemporalAsync(cuenta.Usuario, cuenta.Temporal, MotivoTemporal.CuentaNueva, cancellationToken);
    }

    // RN-13. El índice único de usuario.correo es la red de seguridad ante dos registros simultáneos.
    private async Task ValidarCorreoDisponibleAsync(string correo, int? exceptoUsuarioId, CancellationToken cancellationToken)
    {
        var normalizado = Usuario.NormalizarCorreo(correo);
        var enUso = await db.Usuarios.AnyAsync(
            u => u.Correo == normalizado && (exceptoUsuarioId == null || u.Id != exceptoUsuarioId), cancellationToken);

        if (enUso)
        {
            throw new BusinessRuleException("RN-13", $"Ya existe una cuenta con el correo {normalizado}.");
        }
    }
}