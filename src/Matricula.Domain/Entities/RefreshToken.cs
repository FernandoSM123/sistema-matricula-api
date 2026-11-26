namespace Matricula.Domain.Entities;

/// <summary>
/// Sesión renovable (RF-60). Es una pieza técnica: no hereda de EntidadBase, así que la auditoría no la captura
/// (RN-46). Solo guarda el hash del token, nunca el token en claro.
/// </summary>
public class RefreshToken
{
    public const int TokenHashLength = 64;

    // Para EF Core.
    private RefreshToken()
    {
        TokenHash = SecurityStamp = string.Empty;
    }

    private RefreshToken(
        int usuarioId,
        string tokenHash,
        Guid familyId,
        string securityStamp,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        SecurityStamp = securityStamp;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public long Id { get; private set; }
    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;

    /// <summary>SHA-256 del token en hexadecimal.</summary>
    public string TokenHash { get; private set; }

    /// <summary>Igual para todos los tokens que salen de un mismo ingreso.</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>Copia del stamp de la cuenta al emitirlo: si la cuenta lo regenera, la sesión ya no renueva (RN-52).</summary>
    public string SecurityStamp { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Fin de la sesión. Al rotar se copia, no se extiende (RN-50).</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Cuándo se usó (rotación) o se anuló (reuso).</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    public uint Version { get; private set; }

    /// <summary>
    /// Abre una familia nueva: un ingreso con la contraseña definitiva o un cambio de contraseña.
    /// </summary>
    public static RefreshToken Issue(
        int usuarioId,
        string tokenHash,
        string securityStamp,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityStamp);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        return new RefreshToken(usuarioId, tokenHash, Guid.NewGuid(), securityStamp, now, now + lifetime);
    }

    /// <summary>
    /// RN-51: marca este token como usado y devuelve el siguiente de la familia, con el mismo vencimiento (RN-50).
    /// </summary>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newTokenHash);
        if (!IsActive(now))
        {
            throw new InvalidOperationException("Solo se puede rotar un refresh token activo.");
        }

        RevokedAt = now;

        return new RefreshToken(UsuarioId, newTokenHash, FamilyId, SecurityStamp, now, ExpiresAt);
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && !IsExpired(now);

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>
    /// Anula el token. Si ya estaba anulado o usado, conserva la fecha original.
    /// </summary>
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
