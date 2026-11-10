namespace Matricula.Application.Matriculas;

/// <summary>
/// Matrícula hecha por un administrador (RF-51). Ids anulables para que un dato omitido responda 400.
/// </summary>
public record MatricularRequest(int? EstudianteId, int? GrupoId);
