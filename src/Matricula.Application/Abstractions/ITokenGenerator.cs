namespace Matricula.Application.Abstractions;

/// <summary>
/// Emite el token de acceso. Lo implementa Api, que conoce el formato JWT.
/// </summary>
public interface ITokenGenerator
{
    GeneratedToken Generate(TokenData datos);
}