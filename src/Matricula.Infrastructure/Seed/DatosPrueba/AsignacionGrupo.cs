namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>Profesor, aula (nula si es virtual) y franjas que el planificador asignó a un grupo.</summary>
public sealed record AsignacionGrupo(int ProfesorId, int? AulaId, IReadOnlyList<Franja> Franjas);
