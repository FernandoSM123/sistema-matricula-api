using Matricula.Application.Abstractions;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
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

    /// <summary>
    /// RF-23: listado paginado, solo activos salvo que se pidan los inactivos (RN-41).
    /// </summary>
    public Task<PagedResult<ProfesorResponse>> ListarAsync(ListarProfesoresRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Profesores.AsNoTracking();

        if (!request.IncluirInactivos)
        {
            query = query.Where(p => p.Usuario.Activo);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(termino)
                || p.Apellidos.ToLower().Contains(termino)
                || p.Cedula.ToLower().Contains(termino)
                || (p.Especialidad != null && p.Especialidad.ToLower().Contains(termino))
                || p.Usuario.Correo.Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query
            .OrderBy(p => p.Apellidos)
            .ThenBy(p => p.Nombre)
            .ThenBy(p => p.Id)
            .Select(ProfesorProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// RF-25: datos personales y correo de un profesor. El correo se cambia en la cuenta (RN-31).
    /// </summary>
    public async Task<ProfesorResponse> ActualizarAsync(int id, ActualizarProfesorRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profesor = await db.Profesores
            .Include(p => p.Usuario)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No existe el profesor {id}.");

        var cedula = request.Cedula.Trim();
        if (cedula != profesor.Cedula
            && await db.Profesores.AnyAsync(p => p.Cedula == cedula && p.Id != id, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un profesor con la cédula {cedula}.");
        }

        await cuentaService.CambiarCorreoAsync(profesor.Usuario, request.Correo, cancellationToken);
        profesor.ActualizarDatos(
            request.Cedula, request.Nombre, request.Apellidos, request.Telefono, request.Especialidad, request.FechaContratacion);
        await db.SaveChangesAsync(cancellationToken);

        return profesor.ToResponse();
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
