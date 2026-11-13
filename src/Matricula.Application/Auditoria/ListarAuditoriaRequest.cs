using Matricula.Application.Common.Paginacion;
using Matricula.Domain.Enums;

namespace Matricula.Application.Auditoria;

/// <summary>
/// Filtros de la auditoría (RF-57), todos opcionales y combinables. <see cref="PaginacionRequest.Search"/> busca en
/// el correo del autor (supuesto 10 de la spec).
/// </summary>
public class ListarAuditoriaRequest : PaginacionRequest
{
    /// <summary>Nombre de la entidad, sin distinguir mayúsculas; por ejemplo, Matricula.</summary>
    public string? Entidad { get; set; }

    /// <summary>Id del registro dentro de su entidad. Exige <see cref="Entidad"/>.</summary>
    public int? EntidadId { get; set; }

    /// <summary>Autor del cambio. No se valida que exista: la cuenta pudo eliminarse (RN-43).</summary>
    public int? UsuarioId { get; set; }

    public AccionAuditoria? Accion { get; set; }

    /// <summary>Fecha local del instituto, incluida (supuesto 8).</summary>
    public DateOnly? Desde { get; set; }

    /// <summary>Fecha local del instituto, incluida (supuesto 8).</summary>
    public DateOnly? Hasta { get; set; }

    /// <summary>Todos los registros de una misma operación (supuesto 9).</summary>
    public string? OperacionId { get; set; }
}