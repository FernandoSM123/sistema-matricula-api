namespace Matricula.Application.Abstractions;

/// <summary>
/// Hash de contraseñas definitivas y temporales. Ninguna se guarda en texto plano.
/// </summary>
public interface IPasswordHashService
{
    string Hash(string contrasena);

    PasswordCheckResult Verify(string hash, string contrasena);
}