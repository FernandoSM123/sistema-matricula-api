using Matricula.Domain.Enums;

namespace Matricula.Application.Abstractions;

/// <summary>
/// Lo que viaja en el token. <see cref="DebeCambiarContrasena"/> marca un ingreso con temporal (RN-24).
/// </summary>
public record DatosToken(int UsuarioId, string Correo, Rol Rol, string SecurityStamp, bool DebeCambiarContrasena);