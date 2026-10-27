using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Cuentas;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Administradores;

public class AdministradorService(IAppDbContext db, CuentaService cuentaService)
{
    /// <summary>
    /// RF-04: crea la cuenta (rol Administrador, temporal de 72 horas) y el registro del administrador en un solo
    /// guardado, y después envía la temporal.
    /// </summary>
    public async Task<AdministradorRegistradoResponse> RegistrarAsync(RegistrarAdministradorRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ValidarCedulaDisponibleAsync(request.Cedula, cancellationToken);
        var cuenta = await cuentaService.CrearCuentaConTemporalAsync(request.Correo, Rol.Administrador, cancellationToken);

        var administrador = new Administrador(cuenta.Usuario, request.Cedula, request.Nombre, request.Apellidos, request.Telefono);
        db.Administradores.Add(administrador);
        await db.SaveChangesAsync(cancellationToken);

        var correoEnviado = await cuentaService.EnviarTemporalAsync(cuenta, cancellationToken);
        return new AdministradorRegistradoResponse(administrador.ToResponse(), correoEnviado);
    }

    /// <summary>
    /// RF-55: listado paginado, solo activos salvo que se pidan los inactivos (RN-41).
    /// </summary>
    public Task<PagedResult<AdministradorResponse>> ListarAsync(ListarAdministradoresRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Administradores.AsNoTracking();

        if (!request.IncluirInactivos)
        {
            query = query.Where(a => a.Usuario.Activo);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(a =>
                a.Nombre.ToLower().Contains(termino)
                || a.Apellidos.ToLower().Contains(termino)
                || a.Cedula.ToLower().Contains(termino)
                || a.Usuario.Correo.Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query
            .OrderBy(a => a.Apellidos)
            .ThenBy(a => a.Nombre)
            .ThenBy(a => a.Id)
            .Select(AdministradorProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// RF-55: datos personales y correo de cualquier administrador, incluido el propio. El correo se cambia en
    /// la cuenta (RN-31).
    /// </summary>
    public async Task<AdministradorResponse> ActualizarAsync(int id, ActualizarAdministradorRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var administrador = await db.Administradores
            .Include(a => a.Usuario)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No existe el administrador {id}.");

        var cedula = request.Cedula.Trim();
        if (cedula != administrador.Cedula
            && await db.Administradores.AnyAsync(a => a.Cedula == cedula && a.Id != id, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un administrador con la cédula {cedula}.");
        }

        await cuentaService.CambiarCorreoAsync(administrador.Usuario, request.Correo, cancellationToken);
        administrador.ActualizarDatos(request.Cedula, request.Nombre, request.Apellidos, request.Telefono);
        await db.SaveChangesAsync(cancellationToken);

        return administrador.ToResponse();
    }

    public async Task<AdministradorResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await db.Administradores.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(AdministradorProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el administrador {id}.");

    // RN-13 (cédula única entre administradores) y RN-39 (un inactivo se reactiva, no se registra de nuevo).
    private async Task ValidarCedulaDisponibleAsync(string cedula, CancellationToken cancellationToken)
    {
        var buscada = cedula.Trim();
        var existente = await db.Administradores.AsNoTracking()
            .Where(a => a.Cedula == buscada)
            .Select(a => new { a.UsuarioId, a.Usuario.Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (existente is null)
        {
            return;
        }

        if (existente.Activo)
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un administrador con la cédula {buscada}.");
        }

        throw new BusinessRuleException(
            "RN-39",
            $"La cédula {buscada} pertenece a un administrador inactivo. Reactive su cuenta en lugar de registrarlo de nuevo.",
            new Dictionary<string, object?> { ["usuarioId"] = existente.UsuarioId });
    }
}