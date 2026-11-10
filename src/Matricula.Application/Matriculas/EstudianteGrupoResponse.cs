using Matricula.Domain.Enums;

namespace Matricula.Application.Matriculas;

/// <summary>
/// Un estudiante en la lista de un grupo (supuesto 8): datos de identificación y contacto institucional, sin teléfono
/// ni dirección.
/// </summary>
public record EstudianteGrupoResponse(
    int MatriculaId,
    int EstudianteId,
    string Cedula,
    string Nombre,
    string Apellidos,
    string Correo,
    DateOnly FechaMatricula,
    EstadoMatricula Estado,
    DateOnly? FechaRetiro,
    MotivoRetiro? MotivoRetiro);
