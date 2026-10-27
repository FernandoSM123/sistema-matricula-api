namespace Matricula.Application.Administradores;

/// <summary>
/// Con <see cref="CorreoEnviado"/> en false la cuenta quedó creada, pero la temporal no llegó: se puede reenviar.
/// </summary>
public record AdministradorRegistradoResponse(AdministradorResponse Administrador, bool CorreoEnviado);