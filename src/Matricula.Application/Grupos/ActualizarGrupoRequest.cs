using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

public record ActualizarGrupoRequest(
    string Nrc,
    int? CursoId,
    int? ProfesorId,
    int? PeriodoId,
    int? AulaId,
    int? Cupo,
    Modalidad? Modalidad) : IDatosGrupo;
