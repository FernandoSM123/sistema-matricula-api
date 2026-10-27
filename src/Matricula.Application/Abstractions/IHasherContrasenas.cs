namespace Matricula.Application.Abstractions;

/// <summary>
/// Hash de contraseñas definitivas y temporales. Ninguna se guarda en texto plano.
/// </summary>
public interface IHasherContrasenas
{
    string Hashear(string contrasena);

    ResultadoVerificacion Verificar(string hash, string contrasena);
}