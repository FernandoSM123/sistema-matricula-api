namespace Matricula.Application.Estudiantes;

/// <summary>
/// Lo único que el propio estudiante edita (matriz §2.2): el correo y los demás datos los cambia un
/// administrador (decisiones 1 y 2 de specs/SPEC-estudiantes.md).
/// </summary>
public record ActualizarContactoRequest(string Telefono, string? Direccion);
