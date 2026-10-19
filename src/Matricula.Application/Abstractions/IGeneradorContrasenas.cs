namespace Matricula.Application.Abstractions;

/// <summary>
/// Genera contraseñas temporales aleatorias que cumplen RN-26.
/// </summary>
public interface IGeneradorContrasenas
{
    string Generar();
}