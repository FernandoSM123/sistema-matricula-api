namespace Matricula.Application.Estudiantes;

/// <summary>
/// Con <see cref="CorreoEnviado"/> en false la cuenta quedó creada, pero la temporal no llegó: se puede reenviar.
/// </summary>
public record EstudianteRegistradoResponse(EstudianteResponse Estudiante, bool CorreoEnviado);
