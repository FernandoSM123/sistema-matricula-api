using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Correos;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IHasherContrasenas hasher,
    IGeneradorToken generadorToken,
    ICurrentUser currentUser,
    IGeneradorContrasenas generadorContrasenas,
    EnvioCorreos envioCorreos,
    TimeProvider timeProvider)
{
    public const string MensajeOlvido = "Si el correo está registrado, recibirás una contraseña temporal.";

    /// <summary>
    /// RF-10: si el correo es de una cuenta activa, envía una temporal de 1 hora que reemplaza a la anterior
    /// (RN-27). La contraseña actual sigue funcionando (RN-32). La respuesta es siempre la misma (RN-28, RN-29).
    /// </summary>
    public async Task<MensajeResponse> OlvidoContrasenaAsync(OlvidoContrasenaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correo = Usuario.NormalizarCorreo(request.Correo);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo, cancellationToken);

        if (usuario is { Activo: true })
        {
            var temporal = generadorContrasenas.Generar();
            usuario.AsignarTemporal(hasher.Hashear(temporal), MotivoTemporal.Olvido, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);

            await envioCorreos.EnviarTemporalAsync(usuario, temporal, MotivoTemporal.Olvido, cancellationToken);
        }

        return new MensajeResponse(MensajeOlvido);
    }

    /// <summary>
    /// RF-07: quien ingresó con una temporal define su nueva contraseña. Devuelve un token sin cambio pendiente;
    /// el anterior deja de valer porque cambia el stamp.
    /// </summary>
    public async Task<TokenResponse> CambioObligatorioAsync(CambioObligatorioRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUser.DebeCambiarContrasena)
        {
            throw new ForbiddenException("No tiene un cambio de contraseña pendiente. Para cambiarla use cambiar-contrasena.");
        }

        var usuario = await CargarUsuarioActualAsync(cancellationToken);
        var ahora = timeProvider.GetUtcNow();

        if (!usuario.TemporalVigente(ahora))
        {
            throw new BusinessRuleException("RN-23", "La contraseña temporal venció. Solicite una nueva.");
        }

        if (hasher.Verificar(usuario.TempPasswordHash!, request.NuevaContrasena) != ResultadoVerificacion.Incorrecta)
        {
            throw new BusinessRuleException("RN-25", "La nueva contraseña debe ser distinta de la contraseña temporal.");
        }

        ValidarDistintaDeLaActual(usuario, request.NuevaContrasena);

        usuario.EstablecerContrasena(hasher.Hashear(request.NuevaContrasena), ahora);
        await db.SaveChangesAsync(cancellationToken);

        await envioCorreos.EnviarAvisoCambioContrasenaAsync(usuario, cancellationToken);
        return EmitirToken(usuario, debeCambiarContrasena: false);
    }

    /// <summary>
    /// RF-09: cambio de la contraseña propia con la actual. Envía el aviso (RN-33) y devuelve un token nuevo;
    /// los tokens anteriores, también los de otras sesiones, dejan de valer.
    /// </summary>
    public async Task<TokenResponse> CambiarContrasenaAsync(CambiarContrasenaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await CargarUsuarioActualAsync(cancellationToken);

        if (usuario.PasswordHash is null
            || hasher.Verificar(usuario.PasswordHash, request.ContrasenaActual) == ResultadoVerificacion.Incorrecta)
        {
            throw new BusinessRuleException("RF-09", "La contraseña actual no es correcta.");
        }

        ValidarDistintaDeLaActual(usuario, request.NuevaContrasena);

        usuario.EstablecerContrasena(hasher.Hashear(request.NuevaContrasena), timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        await envioCorreos.EnviarAvisoCambioContrasenaAsync(usuario, cancellationToken);
        return EmitirToken(usuario, debeCambiarContrasena: false);
    }

    /// <summary>
    /// RF-08: cierra todas las sesiones del usuario, porque regenera el stamp.
    /// </summary>
    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var usuario = await CargarUsuarioActualAsync(cancellationToken);

        usuario.CerrarSesiones();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// RF-13: datos de la cuenta del usuario actual y, si los tiene, sus datos personales.
    /// </summary>
    public async Task<PerfilResponse> ObtenerPerfilAsync(CancellationToken cancellationToken)
    {
        var usuarioId = currentUser.UsuarioId ?? throw new UnauthorizedException("No hay una sesión activa.");

        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new PerfilResponse(
                u.Id,
                u.Correo,
                u.Rol,
                u.Activo,
                u.FechaCreacion,
                u.UltimoIngreso,
                u.FechaCambioContrasena,
                db.Administradores
                    .Where(a => a.UsuarioId == u.Id)
                    .Select(a => new DatosPersonalesResponse(a.Cedula, a.Nombre, a.Apellidos, a.Telefono))
                    .FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("La cuenta ya no existe.");
    }

    /// <summary>
    /// RF-06, RF-07 y RF-11. Credenciales inválidas, correo inexistente y temporal vencida dan el mismo 401.
    /// </summary>
    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correo = Usuario.NormalizarCorreo(request.Correo);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo, cancellationToken)
            ?? throw new UnauthorizedException();

        var ahora = timeProvider.GetUtcNow();

        var definitiva = usuario.PasswordHash is null
            ? ResultadoVerificacion.Incorrecta
            : hasher.Verificar(usuario.PasswordHash, request.Contrasena);
        if (definitiva != ResultadoVerificacion.Incorrecta)
        {
            ValidarActiva(usuario);
            if (definitiva == ResultadoVerificacion.CorrectaRequiereRehash)
            {
                usuario.ActualizarHashContrasena(hasher.Hashear(request.Contrasena));
            }

            usuario.RegistrarIngresoConDefinitiva(ahora);
            await db.SaveChangesAsync(cancellationToken);
            return EmitirToken(usuario, debeCambiarContrasena: false);
        }

        if (usuario.TemporalVigente(ahora)
            && hasher.Verificar(usuario.TempPasswordHash!, request.Contrasena) != ResultadoVerificacion.Incorrecta)
        {
            ValidarActiva(usuario);
            usuario.RegistrarIngresoConTemporal(ahora);
            await db.SaveChangesAsync(cancellationToken);
            return EmitirToken(usuario, debeCambiarContrasena: true);
        }

        throw new UnauthorizedException();
    }

    /// <summary>
    /// Se usa al validar cada token: la sesión deja de valer si la cuenta se desactivó o cambió su stamp
    /// (logout, cambio de contraseña o desactivación).
    /// </summary>
    public Task<bool> SesionValidaAsync(int usuarioId, string securityStamp, CancellationToken cancellationToken) =>
        db.Usuarios.AsNoTracking()
            .AnyAsync(u => u.Id == usuarioId && u.Activo && u.SecurityStamp == securityStamp, cancellationToken);

    private async Task<Usuario> CargarUsuarioActualAsync(CancellationToken cancellationToken)
    {
        var usuarioId = currentUser.UsuarioId ?? throw new UnauthorizedException("No hay una sesión activa.");

        return await db.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId, cancellationToken)
            ?? throw new NotFoundException("La cuenta ya no existe.");
    }

    // RN-25.
    private void ValidarDistintaDeLaActual(Usuario usuario, string nuevaContrasena)
    {
        if (usuario.PasswordHash is not null
            && hasher.Verificar(usuario.PasswordHash, nuevaContrasena) != ResultadoVerificacion.Incorrecta)
        {
            throw new BusinessRuleException("RN-25", "La nueva contraseña debe ser distinta de la contraseña actual.");
        }
    }

    // RN-29. Solo se informa cuando la contraseña ya resultó correcta, así no revela qué correos existen.
    private static void ValidarActiva(Usuario usuario)
    {
        if (!usuario.Activo)
        {
            throw new ForbiddenException("La cuenta está desactivada. Contacte a la administración.", "RN-29");
        }
    }

    private TokenResponse EmitirToken(Usuario usuario, bool debeCambiarContrasena)
    {
        var token = generadorToken.Generar(
            new DatosToken(usuario.Id, usuario.Correo, usuario.Rol, usuario.SecurityStamp, debeCambiarContrasena));

        return new TokenResponse(token.AccessToken, token.ExpiraEn, usuario.Rol, debeCambiarContrasena);
    }
}