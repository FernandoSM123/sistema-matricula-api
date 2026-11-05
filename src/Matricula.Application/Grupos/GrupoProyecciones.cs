using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Grupos;

public static class GrupoProyecciones
{
    // CuposDisponibles no está mapeado: en SQL se calcula desde sus columnas.

    /// <summary>
    /// Para los listados (se traduce a SQL).
    /// </summary>
    public static readonly Expression<Func<Grupo, GrupoResponse>> AResponse = g => new GrupoResponse(
        g.Id,
        g.Nrc,
        g.CursoId,
        g.Curso.Codigo,
        g.Curso.Nombre,
        g.ProfesorId,
        g.Profesor.Nombre + " " + g.Profesor.Apellidos,
        g.PeriodoId,
        g.Periodo.Nombre,
        g.AulaId,
        g.Aula != null ? g.Aula.Codigo : null,
        g.Cupo,
        g.CuposOcupados,
        g.Cupo - g.CuposOcupados,
        g.Modalidad,
        g.Estado);

    /// <summary>
    /// Para el detalle (se traduce a SQL). Los horarios llegan sin ordenar: el día se guarda como texto y en SQL se
    /// ordenaría alfabéticamente. Se ordenan con <see cref="OrdenarHorarios"/>.
    /// </summary>
    public static readonly Expression<Func<Grupo, GrupoDetalleResponse>> ADetalle = g => new GrupoDetalleResponse(
        g.Id,
        g.Nrc,
        g.CursoId,
        g.Curso.Codigo,
        g.Curso.Nombre,
        g.ProfesorId,
        g.Profesor.Nombre + " " + g.Profesor.Apellidos,
        g.PeriodoId,
        g.Periodo.Nombre,
        g.AulaId,
        g.Aula != null ? g.Aula.Codigo : null,
        g.Cupo,
        g.CuposOcupados,
        g.Cupo - g.CuposOcupados,
        g.Modalidad,
        g.Estado,
        g.Horarios.Select(h => new HorarioResponse(h.Id, h.Dia, h.HoraInicio, h.HoraFin)).ToList(),
        g.FechaCreacion);

    /// <summary>
    /// Ordena los bloques de lunes a domingo y por hora de inicio.
    /// </summary>
    public static GrupoDetalleResponse OrdenarHorarios(this GrupoDetalleResponse detalle)
    {
        ArgumentNullException.ThrowIfNull(detalle);
        return detalle with
        {
            Horarios = [.. detalle.Horarios.OrderBy(h => h.Dia).ThenBy(h => h.HoraInicio)]
        };
    }
}
