using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Matricula.Infrastructure.Security;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    // PasswordHasher no usa el usuario en su implementación por defecto; se pasa null para no depender de él.
    public string Hash(string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(contrasena);

        return _hasher.HashPassword(null!, contrasena);
    }

    public PasswordCheckResult Verify(string hash, string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(hash);
        ArgumentNullException.ThrowIfNull(contrasena);

        return _hasher.VerifyHashedPassword(null!, hash, contrasena) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
    }
}