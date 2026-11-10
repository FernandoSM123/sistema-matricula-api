using Matricula.Domain.Enums;

namespace Matricula.Application.Matriculas;

/// <summary>
/// Una matrícula con los datos de su estudiante, grupo, curso, periodo y profesor (RF-51, RF-54).
/// </summary>
public record MatriculaResponse(
    int Id,
    int EstudianteId,
    string EstudianteCedula,
    string EstudianteNombre,
    int GrupoId,
    string Nrc,
    Modalidad Modalidad,
    EstadoGrupo EstadoGrupo,
    int CursoId,
    string CursoCodigo,
    string CursoNombre,
    int PeriodoId,
    string PeriodoNombre,
    string ProfesorNombre,
    DateOnly FechaMatricula,
    EstadoMatricula Estado,
    DateOnly? FechaRetiro,
    MotivoRetiro? MotivoRetiro);
