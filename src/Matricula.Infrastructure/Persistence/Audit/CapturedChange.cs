using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Persistence.Audit;

/// <summary>
/// Una modificación o eliminación armada antes del guardado, cuando todavía se conocen los valores originales.
/// </summary>
internal sealed record CapturedChange(AccionAuditoria Accion, string Entidad, int EntidadId, string Cambios);