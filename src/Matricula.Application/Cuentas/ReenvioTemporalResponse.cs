namespace Matricula.Application.Cuentas;

/// <summary>
/// Con <see cref="CorreoEnviado"/> en false la temporal quedó asignada, pero el correo no salió: se puede reintentar.
/// </summary>
public record ReenvioTemporalResponse(bool CorreoEnviado);