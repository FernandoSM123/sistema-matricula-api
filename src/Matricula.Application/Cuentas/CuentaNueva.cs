using Matricula.Domain.Entities;

namespace Matricula.Application.Cuentas;

/// <summary>
/// Cuenta recién creada con su temporal en claro, solo en memoria y solo para enviarla por correo.
/// Es una clase y no un record a propósito: un record imprimiría la temporal en su ToString.
/// </summary>
public sealed class CuentaNueva(Usuario usuario, string temporal)
{
    public Usuario Usuario { get; } = usuario;
    public string Temporal { get; } = temporal;
}