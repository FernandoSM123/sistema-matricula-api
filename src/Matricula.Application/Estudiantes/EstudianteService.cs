using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Cuentas;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Estudiantes;

public class EstudianteService(IAppDbContext db, CuentaService cuentaService, TimeProvider timeProvider)
{
    /// <summary>
    /// RF-02: crea la cuenta (rol Estudiante, temporal de 72 horas) y el registro del estudiante en un solo
    /// guardado, y después envía la temporal.
    /// </summary>
    public async Task<EstudianteRegistradoResponse> RegistrarAsync(RegistrarEstudianteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await ValidarCedulaDisponibleAsync(request.Cedula, cancellationToken);
        var cuenta = await cuentaService.CrearCuentaConTemporalAsync(request.Correo, Rol.Estudiante, cancellationToken);

        var estudiante = new Estudiante(
            cuenta.Usuario,
            request.Cedula,
            request.Nombre,
            request.Apellidos,
            request.Telefono,
            request.FechaNacimiento,
            request.Direccion,
            Hoy());
        db.Estudiantes.Add(estudiante);
        await db.SaveChangesAsync(cancellationToken);

        var correoEnviado = await cuentaService.EnviarTemporalAsync(cuenta, cancellationToken);
        return new EstudianteRegistradoResponse(estudiante.ToResponse(), correoEnviado);
    }

    public async Task<EstudianteResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await db.Estudiantes.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(EstudianteProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el estudiante {id}.");

    private DateOnly Hoy() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    // RN-13 (cédula única entre estudiantes) y RN-39 (un inactivo se reactiva, no se registra de nuevo).
    private async Task ValidarCedulaDisponibleAsync(string cedula, CancellationToken cancellationToken)
    {
        var buscada = cedula.Trim();
        var existente = await db.Estudiantes.AsNoTracking()
            .Where(e => e.Cedula == buscada)
            .Select(e => new { e.Id, e.UsuarioId, e.Usuario.Activo })
            .FirstOrDefaultAsync(cancellationToken);

        if (existente is null)
        {
            return;
        }

        if (existente.Activo)
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un estudiante con la cédula {buscada}.");
        }

        throw new BusinessRuleException(
            "RN-39",
            $"La cédula {buscada} pertenece a un estudiante inactivo. Reactívelo en lugar de registrarlo de nuevo.",
            new Dictionary<string, object?> { ["estudianteId"] = existente.Id, ["usuarioId"] = existente.UsuarioId });
    }
}
