using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Correos;
using Matricula.Application.Grupos;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Cuentas;

/// <summary>
/// Acciones del administrador sobre cualquier cuenta: reenviar la temporal y activar o desactivar.
/// </summary>
public class UsuarioService(
    IAppDbContext db,
    IHasherContrasenas hasher,
    IGeneradorContrasenas generadorContrasenas,
    EnvioCorreos envioCorreos,
    TimeProvider timeProvider,
    IFechaLocal fechaLocal)
{
    /// <summary>
    /// RF-12: nueva temporal de 72 horas que reemplaza a la anterior (RN-23, RN-27). No a cuentas inactivas (RN-29).
    /// </summary>
    public async Task<ReenvioTemporalResponse> ReenviarTemporalAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await CargarAsync(usuarioId, cancellationToken);
        if (!usuario.Activo)
        {
            throw new BusinessRuleException("RN-29", "La cuenta está desactivada y no puede recibir contraseñas temporales. Reactívela primero.");
        }

        var temporal = generadorContrasenas.Generar();
        usuario.AsignarTemporal(hasher.Hashear(temporal), MotivoTemporal.ReenvioAdministrador, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        var correoEnviado = await envioCorreos.EnviarTemporalAsync(usuario, temporal, MotivoTemporal.ReenvioAdministrador, cancellationToken);
        return new ReenvioTemporalResponse(correoEnviado);
    }

    /// <summary>
    /// RF-14: la cuenta deja de poder ingresar y sus tokens dejan de valer (RN-29). Nunca el último administrador
    /// activo (RN-30). A un profesor con grupos activos no (RN-37), ni a un estudiante con matrículas activas en un
    /// periodo en curso (RN-36).
    /// </summary>
    public async Task DesactivarAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await CargarAsync(usuarioId, cancellationToken);
        if (!usuario.Activo)
        {
            return;
        }

        if (usuario.Rol == Rol.Administrador
            && !await db.Usuarios.AnyAsync(u => u.Rol == Rol.Administrador && u.Activo && u.Id != usuarioId, cancellationToken))
        {
            throw new BusinessRuleException("RN-30", "No se puede desactivar al último administrador activo.");
        }

        // Aquí y no en ProfesorService, para que aplique también a /usuarios/{id}/desactivar.
        if (usuario.Rol == Rol.Profesor)
        {
            await db.ValidarSinGruposActivosAsync(
                g => g.Profesor.UsuarioId == usuarioId, "al profesor", cancellationToken);
        }

        if (usuario.Rol == Rol.Estudiante)
        {
            await ValidarSinMatriculasEnCursoAsync(usuarioId, cancellationToken);
        }

        usuario.Desactivar();
        await db.SaveChangesAsync(cancellationToken);
    }

    // RN-36 (supuesto 12): ninguna matrícula activa en un periodo en curso (InicioMatricula ≤ hoy ≤ FinClases), sea o
    // no el vigente.
    private async Task ValidarSinMatriculasEnCursoAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var hoy = fechaLocal.Hoy();
        var activas = await db.Matriculas.CountAsync(
            m => m.Estudiante.UsuarioId == usuarioId
                && m.Estado == EstadoMatricula.Activa
                && m.Grupo.Periodo.InicioMatricula <= hoy
                && hoy <= m.Grupo.Periodo.FinClases,
            cancellationToken);

        if (activas > 0)
        {
            throw new BusinessRuleException(
                "RN-36",
                activas == 1
                    ? "El estudiante tiene 1 matrícula activa en un periodo en curso. Retírelo primero."
                    : $"El estudiante tiene {activas} matrículas activas en periodos en curso. Retírelo primero.");
        }
    }

    /// <summary>
    /// RF-14 y RN-39: un registro inactivo se reactiva, no se vuelve a crear.
    /// </summary>
    public async Task ReactivarAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await CargarAsync(usuarioId, cancellationToken);
        if (usuario.Activo)
        {
            return;
        }

        usuario.Reactivar();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Usuario> CargarAsync(int usuarioId, CancellationToken cancellationToken) =>
        await db.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId, cancellationToken)
        ?? throw new NotFoundException($"No existe el usuario {usuarioId}.");
}