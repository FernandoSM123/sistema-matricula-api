using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
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