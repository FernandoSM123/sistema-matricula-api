using Matricula.Domain.Enums;

namespace Matricula.Application.Grupos;

/// <summary>
/// Estado anulable para que un dato omitido responda 400 en lugar de llegar como Planificado.
/// </summary>
public record CambiarEstadoRequest(EstadoGrupo? Estado);
