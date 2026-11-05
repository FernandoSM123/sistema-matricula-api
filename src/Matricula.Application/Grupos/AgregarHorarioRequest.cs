using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// Datos anulables para que un dato omitido responda 400 en lugar de llegar como Lunes o 00:00.
/// </summary>
public record AgregarHorarioRequest(DiaSemana? Dia, TimeOnly? HoraInicio, TimeOnly? HoraFin);
