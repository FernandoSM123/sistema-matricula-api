using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

public record CreateGrupoRequest(
    string Nrc,
    int? CursoId,
    int? ProfesorId,
    int? PeriodoId,
    int? AulaId,
    int? Cupo,
    Modalidad? Modalidad) : IGrupoData;
