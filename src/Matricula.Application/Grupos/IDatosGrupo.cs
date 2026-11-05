using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// Datos que validan la creación y la edición. Los ids, el cupo y la modalidad son anulables para distinguir un dato
/// que falta (400) de 0 o de Presencial.
/// </summary>
public interface IDatosGrupo
{
    string Nrc { get; }
    int? CursoId { get; }
    int? ProfesorId { get; }
    int? PeriodoId { get; }
    int? AulaId { get; }
    int? Cupo { get; }
    Modalidad? Modalidad { get; }
}
