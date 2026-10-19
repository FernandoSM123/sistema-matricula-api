namespace Matricula.Domain.Enums;

/// <summary>
/// Rol de una cuenta. Se asigna al crearla y nunca cambia (RN-31).
/// </summary>
public enum Rol
{
    Administrador,
    Profesor,
    Estudiante
}