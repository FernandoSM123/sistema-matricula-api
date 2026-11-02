namespace Matricula.Application.Profesores;

/// <summary>
/// Con <see cref="CorreoEnviado"/> en false la cuenta quedó creada, pero la temporal no llegó: se puede reenviar.
/// </summary>
public record ProfesorRegistradoResponse(ProfesorResponse Profesor, bool CorreoEnviado);
