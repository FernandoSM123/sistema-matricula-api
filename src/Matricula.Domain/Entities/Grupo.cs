using Matricula.Domain.Common;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;

namespace Matricula.Domain.Entities;

/// <summary>
/// Apertura de un curso en un periodo, con su profesor, aula, cupo y horarios. No se desactiva: tiene estado propio
/// (RF-47). Las reglas que necesitan otros grupos (RN-07, RN-08) y la capacidad del aula (RN-09) las valida el
/// servicio.
/// </summary>
public class Grupo : EntidadBase
{
    private readonly List<Horario> horarios = [];

    // Para EF Core.
    private Grupo()
    {
        Nrc = string.Empty;
        Curso = null!;
        Profesor = null!;
        Periodo = null!;
    }

    public Grupo(string nrc, int cursoId, int profesorId, int periodoId, int? aulaId, int cupo, Modalidad modalidad)
    {
        Nrc = string.Empty;
        Curso = null!;
        Profesor = null!;
        Periodo = null!;
        Estado = EstadoGrupo.Planificado;
        AsignarDatos(nrc, cursoId, profesorId, periodoId, aulaId, cupo, modalidad);
    }

    public string Nrc { get; private set; }
    public int CursoId { get; private set; }
    public Curso Curso { get; private set; }
    public int ProfesorId { get; private set; }
    public Profesor Profesor { get; private set; }
    public int PeriodoId { get; private set; }
    public Periodo Periodo { get; private set; }
    public int? AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public int Cupo { get; private set; }
    public Modalidad Modalidad { get; private set; }
    public EstadoGrupo Estado { get; private set; }

    /// <summary>Matrículas activas. Aquí solo se lee; lo mantiene el módulo matriculas.</summary>
    public int CuposOcupados { get; private set; }

    public IReadOnlyCollection<Horario> Horarios => horarios.AsReadOnly();

    public int CuposDisponibles => Cupo - CuposOcupados;

    /// <summary>
    /// Forma canónica del NRC: en mayúsculas para que RN-13 no distinga capitalización.
    /// </summary>
    public static string NormalizarNrc(string nrc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nrc);
        return nrc.Trim().ToUpperInvariant();
    }

    /// <summary>Presencial e híbrida se imparten en un aula; virtual no (RN-09).</summary>
    public static bool RequiereAula(Modalidad modalidad) => modalidad != Modalidad.Virtual;

    /// <summary>
    /// RF-45. En Planificado se edita todo. En Abierto, Cerrado y EnCurso solo profesor, aula, modalidad y cupo, para
    /// poder reasignar (RF-26, RN-37). Finalizado y Cancelado no se editan. El cupo nunca baja de los ocupados.
    /// </summary>
    public void ActualizarDatos(
        string nrc, int cursoId, int profesorId, int periodoId, int? aulaId, int cupo, Modalidad modalidad)
    {
        if (Estado is EstadoGrupo.Finalizado or EstadoGrupo.Cancelado)
        {
            throw new BusinessRuleException("RF-45", $"No se puede modificar un grupo en estado {Estado}.");
        }

        if (Estado != EstadoGrupo.Planificado
            && (NormalizarNrc(nrc) != Nrc || cursoId != CursoId || periodoId != PeriodoId))
        {
            throw new BusinessRuleException(
                "RF-45",
                "El NRC, el curso y el periodo solo se pueden cambiar mientras el grupo está Planificado.");
        }

        if (cupo < CuposOcupados)
        {
            throw new BusinessRuleException(
                "RF-45", $"El cupo no puede ser menor que los {CuposOcupados} cupos ya ocupados.");
        }

        AsignarDatos(nrc, cursoId, profesorId, periodoId, aulaId, cupo, modalidad);
    }

    /// <summary>
    /// RF-47: solo se avanza un paso del flujo normal, o se cancela mientras no haya finalizado. Para abrir hace falta
    /// al menos un bloque de horario. Pedir el estado actual no cambia nada.
    /// </summary>
    public void CambiarEstado(EstadoGrupo nuevo)
    {
        if (nuevo == Estado)
        {
            return;
        }

        var permitido = nuevo == EstadoGrupo.Cancelado
            ? Estado != EstadoGrupo.Finalizado
            : nuevo == Siguiente(Estado);

        if (!permitido)
        {
            throw new BusinessRuleException("RF-47", $"Un grupo no puede pasar de {Estado} a {nuevo}.");
        }

        if (nuevo == EstadoGrupo.Abierto && horarios.Count == 0)
        {
            throw new BusinessRuleException("RF-47", "No se puede abrir un grupo sin horarios. Agregue al menos un bloque.");
        }

        Estado = nuevo;
    }

    /// <summary>
    /// RF-48: agrega un bloque si no se traslapa con otro del mismo grupo (RN-14). No en grupos finalizados o
    /// cancelados.
    /// </summary>
    public Horario AgregarHorario(DiaSemana dia, TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (Estado is EstadoGrupo.Finalizado or EstadoGrupo.Cancelado)
        {
            throw new BusinessRuleException("RF-48", $"No se pueden agregar horarios a un grupo en estado {Estado}.");
        }

        var horario = new Horario(dia, horaInicio, horaFin);
        var traslapado = horarios.FirstOrDefault(h => h.SeTraslapaCon(horario));
        if (traslapado is not null)
        {
            throw new BusinessRuleException(
                "RN-14",
                $"El bloque se traslapa con el bloque del {traslapado.Dia} de {traslapado.HoraInicio:HH\\:mm} a "
                + $"{traslapado.HoraFin:HH\\:mm} del mismo grupo.");
        }

        horarios.Add(horario);
        return horario;
    }

    /// <summary>
    /// RF-48: los bloques solo se quitan mientras el grupo está Planificado. Devuelve el bloque quitado, o null si no
    /// es de este grupo. El servicio también debe quitarlo del DbSet: con FK Restrict, EF no borra el huérfano solo.
    /// </summary>
    public Horario? QuitarHorario(int horarioId)
    {
        var horario = horarios.FirstOrDefault(h => h.Id == horarioId);
        if (horario is null)
        {
            return null;
        }

        if (Estado != EstadoGrupo.Planificado)
        {
            throw new BusinessRuleException(
                "RF-48", $"Los bloques solo se pueden quitar mientras el grupo está Planificado; está {Estado}.");
        }

        horarios.Remove(horario);
        return horario;
    }

    private static EstadoGrupo? Siguiente(EstadoGrupo estado) => estado switch
    {
        EstadoGrupo.Planificado => EstadoGrupo.Abierto,
        EstadoGrupo.Abierto => EstadoGrupo.Cerrado,
        EstadoGrupo.Cerrado => EstadoGrupo.EnCurso,
        EstadoGrupo.EnCurso => EstadoGrupo.Finalizado,
        _ => null
    };

    private void AsignarDatos(
        string nrc, int cursoId, int profesorId, int periodoId, int? aulaId, int cupo, Modalidad modalidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cupo);

        // El validador ya responde 400; aquí se comprueba otra vez como invariante.
        if (RequiereAula(modalidad) && aulaId is null)
        {
            throw new BusinessRuleException("RN-09", $"Un grupo con modalidad {modalidad} necesita un aula.");
        }

        if (!RequiereAula(modalidad) && aulaId is not null)
        {
            throw new BusinessRuleException("RN-09", "Un grupo virtual no lleva aula.");
        }

        Nrc = NormalizarNrc(nrc);
        CursoId = cursoId;
        ProfesorId = profesorId;
        PeriodoId = periodoId;
        AulaId = aulaId;
        Cupo = cupo;
        Modalidad = modalidad;
    }
}
