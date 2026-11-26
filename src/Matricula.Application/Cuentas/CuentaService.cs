using Matricula.Application.Abstractions;
using Matricula.Application.Email;
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
    IPasswordHashService hasher,
    IPasswordGenerator passwordGenerator,
    ICurrentUser currentUser,
    EmailDispatcher emailDispatcher,
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
        usuario.SetPassword(hasher.Hash(contrasena), timeProvider.GetUtcNow());
        db.Usuarios.Add(usuario);

        return usuario;
    }

    /// <summary>
    /// Cuenta creada por el administrador actual con una temporal de 72 horas (RF-05, RN-22, RN-23).
    /// </summary>
    public async Task<CuentaWithTemporaryPassword> CrearCuentaConTemporalAsync(string correo, Rol rol, CancellationToken cancellationToken)
    {
        await ValidarCorreoDisponibleAsync(correo, exceptoUsuarioId: null, cancellationToken);

        var temporal = passwordGenerator.Generate();
        var usuario = new Usuario(correo, rol, OrigenRegistro.Administrador, currentUser.UsuarioId);
        usuario.SetTemporaryPassword(hasher.Hash(temporal), MotivoTemporal.CuentaNueva, timeProvider.GetUtcNow());
        db.Usuarios.Add(usuario);

        return new CuentaWithTemporaryPassword(usuario, temporal);
    }

    /// <summary>
    /// RN-31: el correo de la persona es el de su cuenta.
    /// </summary>
    public async Task CambiarCorreoAsync(Usuario usuario, string correo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (Usuario.NormalizeCorreo(correo) == usuario.Correo)
        {
            return;
        }

        await ValidarCorreoDisponibleAsync(correo, usuario.Id, cancellationToken);
        usuario.ChangeCorreo(correo);
    }

    /// <summary>
    /// Se llama después de guardar. Devuelve false si el correo no se pudo enviar.
    /// </summary>
    public Task<bool> SendTemporaryPasswordAsync(CuentaWithTemporaryPassword cuenta, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cuenta);

        return emailDispatcher.SendTemporaryPasswordAsync(cuenta.Usuario, cuenta.TemporaryPassword, MotivoTemporal.CuentaNueva, cancellationToken);
    }

    // RN-13. El índice único de usuario.correo es la red de seguridad ante dos registros simultáneos.
    private async Task ValidarCorreoDisponibleAsync(string correo, int? exceptoUsuarioId, CancellationToken cancellationToken)
    {
        var normalizado = Usuario.NormalizeCorreo(correo);
        var enUso = await db.Usuarios.AnyAsync(
            u => u.Correo == normalizado && (exceptoUsuarioId == null || u.Id != exceptoUsuarioId), cancellationToken);

        if (enUso)
        {
            throw new BusinessRuleException("RN-13", $"Ya existe una cuenta con el correo {normalizado}.");
        }
    }
}