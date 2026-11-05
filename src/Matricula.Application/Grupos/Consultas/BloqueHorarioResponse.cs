using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos.Consultas;

/// <summary>
/// Un bloque del horario semanal de un profesor (RF-28) o de la ocupación de un aula (RF-41), con su grupo.
/// </summary>
public record BloqueHorarioResponse(
    DiaSemana Dia,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    int GrupoId,
    string Nrc,
    string CursoCodigo,
    string CursoNombre,
    int ProfesorId,
    string ProfesorNombre,
    string? AulaCodigo,
    Modalidad Modalidad,
    EstadoGrupo Estado);
