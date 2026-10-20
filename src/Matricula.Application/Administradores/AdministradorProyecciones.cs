using System.Linq.Expressions;
using Matricula.Domain.Entities;

namespace Matricula.Application.Administradores;

public static class AdministradorProyecciones
{
    /// <summary>
    /// Para las consultas (se traduce a SQL). El correo y el estado vienen de la cuenta (RN-31, RN-38).
    /// </summary>
    public static readonly Expression<Func<Administrador, AdministradorResponse>> AResponse = a => new AdministradorResponse(
        a.Id,
        a.UsuarioId,
        a.Cedula,
        a.Nombre,
        a.Apellidos,
        a.Telefono,
        a.Usuario.Correo,
        a.Usuario.Activo,
        a.FechaCreacion,
        a.Usuario.UltimoIngreso);

    private static readonly Func<Administrador, AdministradorResponse> AResponseCompilada = AResponse.Compile();

    /// <summary>
    /// Para una entidad ya cargada junto con su cuenta.
    /// </summary>
    public static AdministradorResponse ToResponse(this Administrador administrador) => AResponseCompilada(administrador);
}