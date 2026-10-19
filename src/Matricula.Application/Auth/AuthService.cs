using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IHasherContrasenas hasher,
    IGeneradorToken generadorToken,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
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