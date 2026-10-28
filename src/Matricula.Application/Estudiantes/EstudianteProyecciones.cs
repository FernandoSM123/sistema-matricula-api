using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Estudiantes;

public static class EstudianteProyecciones
{
    /// <summary>
    /// Para las consultas (se traduce a SQL). El correo y el estado vienen de la cuenta (RN-31, RN-38).
    /// </summary>
    public static readonly Expression<Func<Estudiante, EstudianteResponse>> AResponse = e => new EstudianteResponse(
        e.Id,
        e.UsuarioId,
        e.Cedula,
        e.Nombre,
        e.Apellidos,
        e.Telefono,
        e.Usuario.Correo,
        e.FechaNacimiento,
        e.Direccion,
        e.FechaRegistro,
        e.Usuario.Activo,
        e.FechaCreacion,
        e.Usuario.UltimoIngreso);

    private static readonly Func<Estudiante, EstudianteResponse> AResponseCompilada = AResponse.Compile();

    /// <summary>
    /// Para una entidad ya cargada junto con su cuenta.
    /// </summary>
    public static EstudianteResponse ToResponse(this Estudiante estudiante) => AResponseCompilada(estudiante);
}
