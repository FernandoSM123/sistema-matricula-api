using Matricula.Domain.Common;
using Matricula.Domain.Enums;

namespace Matricula.Domain.Entities;

/// <summary>
/// Cuenta de acceso al sistema. Recibe siempre hashes, nunca contraseñas en claro.
/// </summary>
public class Usuario : EntidadBase
{
    public static readonly TimeSpan VigenciaTemporal = TimeSpan.FromHours(72);
    public static readonly TimeSpan VigenciaTemporalOlvido = TimeSpan.FromHours(1);

    // Para EF Core.
    private Usuario()
    {
        Correo = string.Empty;
        SecurityStamp = NuevoStamp();
    }

    public Usuario(string correo, Rol rol, OrigenRegistro origenRegistro, int? creadoPorId = null)
    {
        Correo = NormalizarCorreo(correo);
        Rol = rol;
        OrigenRegistro = origenRegistro;
        CreadoPorId = creadoPorId;
        Activo = true;
        SecurityStamp = NuevoStamp();
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
    public static string NormalizarCorreo(string correo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);

        return correo.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// RN-23 y RN-27: la nueva temporal reemplaza a la anterior y vence a las 72 horas, o a la hora si es por olvido.
    /// </summary>
    public void AsignarTemporal(string hash, MotivoTemporal motivo, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        TempPasswordHash = hash;
        TempPasswordMotivo = motivo;
        TempPasswordExpira = ahora + (motivo == MotivoTemporal.Olvido ? VigenciaTemporalOlvido : VigenciaTemporal);
    }

    public bool TemporalVigente(DateTimeOffset ahora) =>
        TempPasswordHash is not null && TempPasswordExpira > ahora;

    /// <summary>
    /// Fija la contraseña definitiva, anula la temporal e invalida los tokens emitidos.
    /// </summary>
    public void EstablecerContrasena(string hash, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        PasswordHash = hash;
        FechaCambioContrasena = ahora;
        AnularTemporal();
        RegenerarStamp();
    }

    /// <summary>
    /// RF-11 y RN-27: ingresar con la contraseña actual anula la temporal pendiente.
    /// </summary>
    public void RegistrarIngresoConDefinitiva(DateTimeOffset ahora)
    {
        AnularTemporal();
        UltimoIngreso = ahora;
    }

    /// <summary>
    /// La temporal sigue vigente hasta que se complete el cambio obligatorio o venza.
    /// </summary>
    public void RegistrarIngresoConTemporal(DateTimeOffset ahora) => UltimoIngreso = ahora;

    public void CambiarCorreo(string correo) => Correo = NormalizarCorreo(correo);

    public void Desactivar()
    {
        Activo = false;
        RegenerarStamp();
    }

    public void Reactivar()
    {
        Activo = true;
        RegenerarStamp();
    }

    /// <summary>
    /// Logout: invalida todos los tokens emitidos para la cuenta.
    /// </summary>
    public void CerrarSesiones() => RegenerarStamp();

    private void AnularTemporal()
    {
        TempPasswordHash = null;
        TempPasswordExpira = null;
        TempPasswordMotivo = null;
    }

    private void RegenerarStamp() => SecurityStamp = NuevoStamp();

    private static string NuevoStamp() => Guid.NewGuid().ToString();
}