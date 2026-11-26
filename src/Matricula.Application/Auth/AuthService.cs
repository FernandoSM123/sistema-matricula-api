using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Email;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IPasswordHashService hasher,
    ITokenGenerator tokenGenerator,
    ICurrentUser currentUser,
    IPasswordGenerator passwordGenerator,
    EmailDispatcher emailDispatcher,
    TimeProvider timeProvider)
{
    public const string ForgotPasswordMessage = "Si el correo está registrado, recibirá una contraseña temporal.";

    /// <summary>
    /// RF-10: si el correo es de una cuenta activa, envía una temporal de 1 hora que reemplaza a la anterior
    /// (RN-27). La contraseña actual sigue funcionando (RN-32). La respuesta es siempre la misma (RN-28, RN-29).
    /// </summary>
    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correo = Usuario.NormalizeCorreo(request.Correo);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo, cancellationToken);

        if (usuario is { Activo: true })
        {
            var temporal = passwordGenerator.Generate();
            usuario.SetTemporaryPassword(hasher.Hash(temporal), MotivoTemporal.Olvido, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);

            await emailDispatcher.SendTemporaryPasswordAsync(usuario, temporal, MotivoTemporal.Olvido, cancellationToken);
        }

        return new MessageResponse(ForgotPasswordMessage);
    }

    /// <summary>
    /// RF-07: quien ingresó con una temporal define su nueva contraseña. Devuelve un token sin cambio pendiente;
    /// el anterior deja de valer porque cambia el stamp.
    /// </summary>
    public async Task<TokenResponse> RequiredPasswordChangeAsync(RequiredPasswordChangeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUser.DebeCambiarContrasena)
        {
            throw new ForbiddenException("No tiene un cambio de contraseña pendiente. Para cambiarla use cambiar-contrasena.");
        }

        var usuario = await LoadCurrentUsuarioAsync(cancellationToken);
        var ahora = timeProvider.GetUtcNow();

        if (!usuario.HasValidTemporaryPassword(ahora))
        {
            throw new BusinessRuleException("RN-23", "La contraseña temporal venció. Solicite una nueva.");
        }

        if (hasher.Verify(usuario.TempPasswordHash!, request.NuevaContrasena) != PasswordCheckResult.Failed)
        {
            throw new BusinessRuleException("RN-25", "La nueva contraseña debe ser distinta de la contraseña temporal.");
        }

        EnsureDifferentFromCurrent(usuario, request.NuevaContrasena);

        usuario.SetPassword(hasher.Hash(request.NuevaContrasena), ahora);
        var (refreshToken, refreshExpiraEn) = StartSession(usuario, ahora);
        await db.SaveChangesAsync(cancellationToken);

        await emailDispatcher.SendPasswordChangedAsync(usuario, cancellationToken);
        return await IssueTokensAsync(usuario, debeCambiarContrasena: false, refreshToken, refreshExpiraEn, cancellationToken);
    }

    /// <summary>
    /// RF-09: cambio de la contraseña propia con la actual. Envía el aviso (RN-33) y devuelve un token nuevo;
    /// los tokens anteriores, también los de otras sesiones, dejan de valer.
    /// </summary>
    public async Task<TokenResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await LoadCurrentUsuarioAsync(cancellationToken);

        if (usuario.PasswordHash is null
            || hasher.Verify(usuario.PasswordHash, request.ContrasenaActual) == PasswordCheckResult.Failed)
        {
            throw new BusinessRuleException("RF-09", "La contraseña actual no es correcta.");
        }

        EnsureDifferentFromCurrent(usuario, request.NuevaContrasena);

        var ahora = timeProvider.GetUtcNow();
        usuario.SetPassword(hasher.Hash(request.NuevaContrasena), ahora);
        var (refreshToken, refreshExpiraEn) = StartSession(usuario, ahora);
        await db.SaveChangesAsync(cancellationToken);

        await emailDispatcher.SendPasswordChangedAsync(usuario, cancellationToken);
        return await IssueTokensAsync(usuario, debeCambiarContrasena: false, refreshToken, refreshExpiraEn, cancellationToken);
    }

    /// <summary>
    /// RF-08: cierra todas las sesiones del usuario, porque regenera el stamp.
    /// </summary>
    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var usuario = await LoadCurrentUsuarioAsync(cancellationToken);

        usuario.EndSessions();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// RF-13: datos de la cuenta del usuario actual y, si los tiene, sus datos personales (de administrador, de
    /// estudiante o de profesor).
    /// </summary>
    public async Task<ProfileResponse> GetProfileAsync(CancellationToken cancellationToken)
    {
        var usuarioId = currentUser.UsuarioId ?? throw new UnauthorizedException("No hay una sesión activa.");

        var resultado = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new
            {
                Perfil = new ProfileResponse(
                    u.Id,
                    null,
                    u.Correo,
                    u.Rol,
                    u.Activo,
                    u.FechaCreacion,
                    u.UltimoIngreso,
                    u.FechaCambioContrasena,
                    null),
                Administrador = db.Administradores
                    .Where(a => a.UsuarioId == u.Id)
                    .Select(a => new
                    {
                        a.Id,
                        Datos = new DatosPersonalesResponse(
                            a.Cedula, a.Nombre, a.Apellidos, a.Telefono, null, null, null, null, null)
                    })
                    .FirstOrDefault(),
                Estudiante = db.Estudiantes
                    .Where(e => e.UsuarioId == u.Id)
                    .Select(e => new
                    {
                        e.Id,
                        Datos = new DatosPersonalesResponse(
                            e.Cedula, e.Nombre, e.Apellidos, e.Telefono, e.FechaNacimiento, e.Direccion, e.FechaRegistro, null, null)
                    })
                    .FirstOrDefault(),
                Profesor = db.Profesores
                    .Where(p => p.UsuarioId == u.Id)
                    .Select(p => new
                    {
                        p.Id,
                        Datos = new DatosPersonalesResponse(
                            p.Cedula, p.Nombre, p.Apellidos, p.Telefono, null, null, null, p.Especialidad, p.FechaContratacion)
                    })
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("La cuenta ya no existe.");

        var persona = resultado.Administrador ?? resultado.Estudiante ?? resultado.Profesor;
        return resultado.Perfil with { PersonaId = persona?.Id, DatosPersonales = persona?.Datos };
    }

    /// <summary>
    /// RF-06, RF-07 y RF-11. Credenciales inválidas, correo inexistente y temporal vencida dan el mismo 401.
    /// </summary>
    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correo = Usuario.NormalizeCorreo(request.Correo);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Correo == correo, cancellationToken)
            ?? throw new UnauthorizedException();

        var ahora = timeProvider.GetUtcNow();

        var definitiva = usuario.PasswordHash is null
            ? PasswordCheckResult.Failed
            : hasher.Verify(usuario.PasswordHash, request.Contrasena);
        if (definitiva != PasswordCheckResult.Failed)
        {
            EnsureActive(usuario);
            if (definitiva == PasswordCheckResult.SuccessRehashNeeded)
            {
                usuario.RehashPassword(hasher.Hash(request.Contrasena));
            }

            usuario.RecordLogin(ahora);
            await RemoveExpiredSessionsAsync(usuario.Id, ahora, cancellationToken);
            var (refreshToken, refreshExpiraEn) = StartSession(usuario, ahora);
            await db.SaveChangesAsync(cancellationToken);
            return await IssueTokensAsync(usuario, debeCambiarContrasena: false, refreshToken, refreshExpiraEn, cancellationToken);
        }

        if (usuario.HasValidTemporaryPassword(ahora)
            && hasher.Verify(usuario.TempPasswordHash!, request.Contrasena) != PasswordCheckResult.Failed)
        {
            EnsureActive(usuario);
            usuario.RecordTemporaryPasswordLogin(ahora);
            await db.SaveChangesAsync(cancellationToken);
            // RN-24: la sesión con una temporal solo sirve para el cambio obligatorio; no se puede renovar (RN-52).
            return await IssueTokensAsync(usuario, debeCambiarContrasena: true, null, null, cancellationToken);
        }

        throw new UnauthorizedException();
    }

    /// <summary>
    /// Se usa al validar cada token: la sesión deja de valer si la cuenta se desactivó o cambió su stamp
    /// (logout, cambio de contraseña o desactivación).
    /// </summary>
    public Task<bool> IsSessionValidAsync(int usuarioId, string securityStamp, CancellationToken cancellationToken) =>
        db.Usuarios.AsNoTracking()
            .AnyAsync(u => u.Id == usuarioId && u.Activo && u.SecurityStamp == securityStamp, cancellationToken);

    private async Task<Usuario> LoadCurrentUsuarioAsync(CancellationToken cancellationToken)
    {
        var usuarioId = currentUser.UsuarioId ?? throw new UnauthorizedException("No hay una sesión activa.");

        return await db.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId, cancellationToken)
            ?? throw new NotFoundException("La cuenta ya no existe.");
    }

    // RN-25.
    private void EnsureDifferentFromCurrent(Usuario usuario, string nuevaContrasena)
    {
        if (usuario.PasswordHash is not null
            && hasher.Verify(usuario.PasswordHash, nuevaContrasena) != PasswordCheckResult.Failed)
        {
            throw new BusinessRuleException("RN-25", "La nueva contraseña debe ser distinta de la contraseña actual.");
        }
    }

    // RN-29. Solo se informa cuando la contraseña ya resultó correcta, así no revela qué correos existen.
    private static void EnsureActive(Usuario usuario)
    {
        if (!usuario.Activo)
        {
            throw new ForbiddenException("La cuenta está desactivada. Contacte a la administración.", "RN-29");
        }
    }

    /// <summary>
    /// Abre una familia nueva de refresh tokens con el stamp actual de la cuenta. Va después de SetPassword, que
    /// regenera el stamp, y antes del guardado de la operación. Devuelve el token en claro, que solo viaja en la respuesta.
    /// </summary>
    private (string Token, DateTimeOffset ExpiraEn) StartSession(Usuario usuario, DateTimeOffset ahora)
    {
        var generado = tokenGenerator.GenerateRefreshToken();
        var refreshToken = RefreshToken.Issue(
            usuario.Id, generado.Hash, usuario.SecurityStamp, ahora, tokenGenerator.RefreshTokenLifetime);
        db.RefreshTokens.Add(refreshToken);

        return (generado.Token, refreshToken.ExpiresAt);
    }

    /// <summary>
    /// Supuesto 3: las sesiones vencidas de la cuenta se borran al iniciar sesión. Las anuladas que no han vencido se
    /// conservan, porque sirven para detectar el reuso (RN-51).
    /// </summary>
    private async Task RemoveExpiredSessionsAsync(int usuarioId, DateTimeOffset ahora, CancellationToken cancellationToken)
    {
        var vencidas = await db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.ExpiresAt <= ahora)
            .ToListAsync(cancellationToken);
        db.RefreshTokens.RemoveRange(vencidas);
    }

    private async Task<TokenResponse> IssueTokensAsync(
        Usuario usuario,
        bool debeCambiarContrasena,
        string? refreshToken,
        DateTimeOffset? refreshExpiraEn,
        CancellationToken cancellationToken)
    {
        // RN-18: matriculas y grupos identifican a la persona por estos claims, sin que el cliente envíe su id.
        var estudianteId = usuario.Rol == Rol.Estudiante
            ? await db.Estudiantes.AsNoTracking()
                .Where(e => e.UsuarioId == usuario.Id)
                .Select(e => (int?)e.Id)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var profesorId = usuario.Rol == Rol.Profesor
            ? await db.Profesores.AsNoTracking()
                .Where(p => p.UsuarioId == usuario.Id)
                .Select(p => (int?)p.Id)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        var token = tokenGenerator.Generate(new TokenData(
            usuario.Id, usuario.Correo, usuario.Rol, usuario.SecurityStamp, debeCambiarContrasena, estudianteId, profesorId));

        return new TokenResponse(
            token.AccessToken, token.ExpiraEn, usuario.Rol, debeCambiarContrasena, refreshToken, refreshExpiraEn);
    }
}