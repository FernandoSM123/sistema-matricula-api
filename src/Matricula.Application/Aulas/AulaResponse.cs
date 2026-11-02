namespace Matricula.Application.Aulas;

public record AulaResponse(
    int Id,
    string Codigo,
    string? Ubicacion,
    int Capacidad,
    bool Activo,
    DateTimeOffset FechaCreacion);
