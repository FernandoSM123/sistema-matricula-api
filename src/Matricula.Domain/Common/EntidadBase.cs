namespace Matricula.Domain.Common;

public abstract class EntidadBase
{
    public int Id { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaModificacion { get; set; }
}
