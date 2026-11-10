namespace Matricula.Application.Matriculas;

/// <summary>
/// Matrícula del estudiante de la sesión (RF-51). No lleva el id del estudiante: sale del token (RN-18).
/// </summary>
public record MatricularPropioRequest(int? GrupoId);
