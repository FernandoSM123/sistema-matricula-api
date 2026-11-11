using Matricula.Domain.Enums;

namespace Matricula.Infrastructure.Persistence.Auditoria;

/// <summary>
/// Una modificación o eliminación armada antes del guardado, cuando todavía se conocen los valores originales.
/// </summary>
internal sealed record CambioCapturado(AccionAuditoria Accion, string Entidad, int EntidadId, string Cambios);