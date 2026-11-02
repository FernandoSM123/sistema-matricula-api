using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Profesores;

public static class ProfesorProyecciones
{
    /// <summary>
    /// Para las consultas (se traduce a SQL). El correo y el estado vienen de la cuenta (RN-31, RN-38).
    /// </summary>
    public static readonly Expression<Func<Profesor, ProfesorResponse>> AResponse = p => new ProfesorResponse(
        p.Id,
        p.UsuarioId,
        p.Cedula,
        p.Nombre,
        p.Apellidos,
        p.Telefono,
        p.Usuario.Correo,
        p.Especialidad,
        p.FechaContratacion,
        p.Usuario.Activo,
        p.FechaCreacion,
        p.Usuario.UltimoIngreso);

    private static readonly Func<Profesor, ProfesorResponse> AResponseCompilada = AResponse.Compile();

    /// <summary>
    /// Para una entidad ya cargada junto con su cuenta.
    /// </summary>
    public static ProfesorResponse ToResponse(this Profesor profesor) => AResponseCompilada(profesor);
}
