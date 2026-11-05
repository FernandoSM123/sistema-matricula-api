using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos.Consultas;

/// <summary>
/// Un grupo de la oferta pública (RF-50). Del profesor solo se muestra el nombre (supuesto 8).
/// </summary>
public record OfertaAcademicaResponse(
    int GrupoId,
    string Nrc,
    int CursoId,
    string CursoCodigo,
    string CursoNombre,
    string ProfesorNombre,
    Modalidad Modalidad,
    string? AulaCodigo,
    int Cupo,
    int CuposDisponibles,
    IReadOnlyList<HorarioResponse> Horarios);
