using Matricula.Domain.Common;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;

namespace Matricula.Domain.Entities;

/// <summary>
/// Inscripción de un estudiante en un grupo (§5.10). Nunca se elimina (RN-35): se retira. Las reglas que necesitan
/// otros datos (RN-01 a RN-06, RN-10, RN-11) las valida el servicio; el cupo lo lleva <see cref="Grupo"/>.
/// </summary>
public class Matricula : EntidadBase
{
    // Para EF Core.
    private Matricula()
    {
        Estudiante = null!;
        Grupo = null!;
    }

    public Matricula(int estudianteId, int grupoId, DateOnly fechaMatricula)
    {
        Estudiante = null!;
        Grupo = null!;
        EstudianteId = estudianteId;
        GrupoId = grupoId;
        FechaMatricula = fechaMatricula;
        Estado = EstadoMatricula.Activa;
    }

    public int EstudianteId { get; private set; }
    public Estudiante Estudiante { get; private set; }
    public int GrupoId { get; private set; }
    public Grupo Grupo { get; private set; }

    /// <summary>Fecha local del instituto en que se matriculó (decisión 9 del mapa).</summary>
    public DateOnly FechaMatricula { get; private set; }

    public EstadoMatricula Estado { get; private set; }
    public DateOnly? FechaRetiro { get; private set; }
    public MotivoRetiro? MotivoRetiro { get; private set; }

    /// <summary>
    /// RF-52, RF-53 y RN-12: pasa a Retirada con la fecha y el motivo. El servicio libera el cupo del grupo.
    /// </summary>
    public void Retirar(DateOnly fecha, MotivoRetiro motivo)
    {
        if (Estado == EstadoMatricula.Retirada)
        {
            throw new BusinessRuleException("RF-52", "La matrícula ya está retirada.");
        }

        Estado = EstadoMatricula.Retirada;
        FechaRetiro = fecha;
        MotivoRetiro = motivo;
    }
}
