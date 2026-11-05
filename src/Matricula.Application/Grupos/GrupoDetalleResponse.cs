using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// Detalle de un grupo (RF-44): sus horarios, el cupo total, los ocupados y los disponibles.
/// </summary>
public record GrupoDetalleResponse(
    int Id,
    string Nrc,
    int CursoId,
    string CursoCodigo,
    string CursoNombre,
    int ProfesorId,
    string ProfesorNombre,
    int PeriodoId,
    string PeriodoNombre,
    int? AulaId,
    string? AulaCodigo,
    int Cupo,
    int CuposOcupados,
    int CuposDisponibles,
    Modalidad Modalidad,
    EstadoGrupo Estado,
    IReadOnlyList<HorarioResponse> Horarios,
    DateTimeOffset FechaCreacion);
