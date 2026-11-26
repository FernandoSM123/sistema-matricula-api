using Matricula.Domain.Common;
using Matricula.Domain.Enums;

namespace Matricula.Domain.Entities;

/// <summary>
/// Cuenta de acceso al sistema. Recibe siempre hashes, nunca contraseñas en claro.
/// </summary>
public class Usuario : EntidadBase
{
    public static readonly TimeSpan TemporaryPasswordLifetime = TimeSpan.FromHours(72);
    public static readonly TimeSpan ForgotPasswordLifetime = TimeSpan.FromHours(1);

    // Para EF Core.
    private Usuario()
    {
        Correo = string.Empty;
        SecurityStamp = NewStamp();
    }

    public Usuario(string correo, Rol rol, OrigenRegistro origenRegistro, int? creadoPorId = null)
    {
        Correo = NormalizeCorreo(correo);
        Rol = rol;
        OrigenRegistro = origenRegistro;
        CreadoPorId = creadoPorId;
        Activo = true;
        SecurityStamp = NewStamp();
    }

    public string Correo { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? TempPasswordHash { get; private set; }
    public DateTimeOffset? TempPasswordExpira { get; private set; }
    public MotivoTemporal? TempPasswordMotivo { get; private set; }

    // RN-31: el rol se fija al crear la cuenta y nunca cambia.
    public Rol Rol { get; init; }

    public bool Activo { get; private set; }
    public string SecurityStamp { get; private set; }
    public OrigenRegistro OrigenRegistro { get; init; }
    public int? CreadoPorId { get; init; }
    public DateTimeOffset? UltimoIngreso { get; private set; }
    public DateTimeOffset? FechaCambioContrasena { get; private set; }

    /// <summary>
    /// Único punto de normalización del correo: login, olvido, registro y edición comparan con este valor.
    /// </summary>
    public static string NormalizeCorreo(string correo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);

        return correo.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// RN-23 y RN-27: la nueva temporal reemplaza a la anterior y vence a las 72 horas, o a la hora si es por olvido.
    /// </summary>
    public void SetTemporaryPassword(string hash, MotivoTemporal motivo, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        TempPasswordHash = hash;
        TempPasswordMotivo = motivo;
        TempPasswordExpira = ahora + (motivo == MotivoTemporal.Olvido ? ForgotPasswordLifetime : TemporaryPasswordLifetime);
    }

    public bool HasValidTemporaryPassword(DateTimeOffset ahora) =>
        TempPasswordHash is not null && TempPasswordExpira > ahora;

    /// <summary>
    /// Fija la contraseña definitiva, anula la temporal e invalida los tokens emitidos.
    /// </summary>
    public void SetPassword(string hash, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        PasswordHash = hash;
        FechaCambioContrasena = ahora;
        ClearTemporaryPassword();
        RegenerateStamp();
    }

    /// <summary>
    /// Recalcula el hash de la misma contraseña (parámetros de hash más nuevos). No es un cambio de contraseña:
    /// no cambia la fecha ni el stamp, ni envía aviso.
    /// </summary>
    public void RehashPassword(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        PasswordHash = hash;
    }

    /// <summary>
    /// RF-11 y RN-27: ingresar con la contraseña actual anula la temporal pendiente.
    /// </summary>
    public void RecordLogin(DateTimeOffset ahora)
    {
        ClearTemporaryPassword();
        UltimoIngreso = ahora;
    }

    /// <summary>
    /// La temporal sigue vigente hasta que se complete el cambio obligatorio o venza.
    /// </summary>
    public void RecordTemporaryPasswordLogin(DateTimeOffset ahora) => UltimoIngreso = ahora;

    public void ChangeCorreo(string correo) => Correo = NormalizeCorreo(correo);

    public void Deactivate()
    {
        Activo = false;
        RegenerateStamp();
    }

    public void Reactivate()
    {
        Activo = true;
        RegenerateStamp();
    }

    /// <summary>
    /// Logout: invalida todos los tokens emitidos para la cuenta.
    /// </summary>
    public void EndSessions() => RegenerateStamp();

    private void ClearTemporaryPassword()
    {
        TempPasswordHash = null;
        TempPasswordExpira = null;
        TempPasswordMotivo = null;
    }

    private void RegenerateStamp() => SecurityStamp = NewStamp();

    private static string NewStamp() => Guid.NewGuid().ToString();
}