using Matricula.Domain.Enums;

namespace Matricula.Application.Cursos;

/// <summary>
/// Costo y Nivel son anulables para distinguir un dato que falta de 0 y de Basico, que son valores válidos.
/// </summary>
public record RegistrarCursoRequest(
    string Codigo,
    string Nombre,
    string? Descripcion,
    int DuracionHoras,
    decimal? Costo,
    NivelCurso? Nivel);
