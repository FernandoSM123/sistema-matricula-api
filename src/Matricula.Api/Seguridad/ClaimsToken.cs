namespace Matricula.Api.Seguridad;

/// <summary>
/// Nombres de los claims del token. Se leen tal cual se emiten (MapInboundClaims = false).
/// </summary>
public static class ClaimsToken
{
    public const string UsuarioId = "sub";
    public const string Correo = "email";
    public const string Rol = "role";
    public const string SecurityStamp = "security_stamp";

    // RN-24: solo existe en los tokens emitidos al ingresar con una temporal.
    public const string DebeCambiarContrasena = "must_change_password";

    // Los añaden los módulos estudiantes y profesores al login.
    public const string EstudianteId = "estudiante_id";
    public const string ProfesorId = "profesor_id";
}