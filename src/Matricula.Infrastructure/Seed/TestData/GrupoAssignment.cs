namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>Profesor, aula (nula si es virtual) y franjas que el planificador asignó a un grupo.</summary>
public sealed record GrupoAssignment(int ProfesorId, int? AulaId, IReadOnlyList<TimeSlot> TimeSlots);
