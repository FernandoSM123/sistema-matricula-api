using Matricula.Domain.Enums;

namespace Matricula.Application.Cursos;

public record CursoResponse(
    int Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    int DuracionHoras,
    decimal Costo,
    NivelCurso Nivel,
    bool Activo,
    DateTimeOffset FechaCreacion);
