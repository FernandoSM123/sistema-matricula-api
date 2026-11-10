using Matricula.Domain.Enums;

namespace Matricula.Application.Matriculas;

/// <summary>
/// Un bloque ya ocupado por un estudiante que choca con un bloque nuevo (RN-06).
/// </summary>
public record ChoqueHorario(int EstudianteId, string Nrc, DiaSemana Dia, TimeOnly HoraInicio, TimeOnly HoraFin);
