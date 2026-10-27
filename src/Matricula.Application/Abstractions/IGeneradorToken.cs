namespace Matricula.Application.Abstractions;

/// <summary>
/// Emite el token de acceso. Lo implementa Api, que conoce el formato JWT.
/// </summary>
public interface IGeneradorToken
{
    TokenGenerado Generar(DatosToken datos);
}