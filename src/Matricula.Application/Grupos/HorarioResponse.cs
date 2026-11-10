using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

public record HorarioResponse(int Id, DiaSemana Dia, TimeOnly HoraInicio, TimeOnly HoraFin);
