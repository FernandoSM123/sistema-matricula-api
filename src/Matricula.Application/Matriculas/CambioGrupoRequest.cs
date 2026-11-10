namespace Matricula.Application.Matriculas;

/// <summary>
/// Cambio de grupo (RF-53): otro grupo del mismo curso y periodo. Anulable para que un dato omitido responda 400.
/// </summary>
public record CambioGrupoRequest(int? GrupoDestinoId);
