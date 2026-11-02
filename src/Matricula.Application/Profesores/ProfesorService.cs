using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Cuentas;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Profesores;

public class ProfesorService(IAppDbContext db, CuentaService cuentaService)
{
    /// <summary>
    /// RF-03 y RF-22: crea la cuenta (rol Profesor, temporal de 72 horas) y el registro del profesor en un solo
    /// guardado, y después envía la temporal.
    /// </summary>
    public async Task<ProfesorRegistradoResponse> RegistrarAsync(RegistrarProfesorRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ValidarCedulaDisponibleAsync(request.Cedula, cancellationToken);
        var cuenta = await cuentaService.CrearCuentaConTemporalAsync(request.Correo, Rol.Profesor, cancellationToken);

        var profesor = new Profesor(
            cuenta.Usuario,
            request.Cedula,
            request.Nombre,
            request.Apellidos,
            request.Telefono,
            request.Especialidad,
            request.FechaContratacion);
        db.Profesores.Add(profesor);
        await db.SaveChangesAsync(cancellationToken);

        var correoEnviado = await cuentaService.EnviarTemporalAsync(cuenta, cancellationToken);
        return new ProfesorRegistradoResponse(profesor.ToResponse(), correoEnviado);
    }

    public async Task<ProfesorResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await db.Profesores.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ProfesorProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el profesor {id}.");

    // RN-13 (cédula única entre profesores) y RN-39 (un inactivo se reactiva, no se registra de nuevo).
    private async Task ValidarCedulaDisponibleAsync(string cedula, CancellationToken cancellationToken)
    {
        var buscada = cedula.Trim();
        var existente = await db.Profesores.AsNoTracking()
            .Where(p => p.Cedula == buscada)
            .Select(p => new { p.Id, p.UsuarioId, p.Usuario.Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (existente is null)
        {
            return;
        }

        if (existente.Activo)
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un profesor con la cédula {buscada}.");
        }

        throw new BusinessRuleException(
            "RN-39",
            $"La cédula {buscada} pertenece a un profesor inactivo. Reactívelo en lugar de registrarlo de nuevo.",
            new Dictionary<string, object?> { ["profesorId"] = existente.Id, ["usuarioId"] = existente.UsuarioId });
    }
}
