using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// Un grupo en los listados (RF-43). El detalle agrega los horarios (<see cref="GrupoDetalleResponse"/>).
/// </summary>
public record GrupoResponse(
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
    EstadoGrupo Estado);
