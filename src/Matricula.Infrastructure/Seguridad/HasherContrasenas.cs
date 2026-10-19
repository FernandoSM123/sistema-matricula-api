using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Matricula.Infrastructure.Seguridad;

public class HasherContrasenas : IHasherContrasenas
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    // PasswordHasher no usa el usuario en su implementación por defecto; se pasa null para no depender de él.
    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(contrasena);

        return _hasher.HashPassword(null!, contrasena);
    }

    public ResultadoVerificacion Verificar(string hash, string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(hash);
        ArgumentNullException.ThrowIfNull(contrasena);

        return _hasher.VerifyHashedPassword(null!, hash, contrasena) switch
        {
            PasswordVerificationResult.Success => ResultadoVerificacion.Correcta,
            PasswordVerificationResult.SuccessRehashNeeded => ResultadoVerificacion.CorrectaRequiereRehash,
            _ => ResultadoVerificacion.Incorrecta
        };
    }
}