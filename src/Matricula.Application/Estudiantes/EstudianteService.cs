using Matricula.Application.Abstractions;
using Matricula.Application.Auth;
using Matricula.Application.Common.Exceptions;
using Matricula.Application.Common.Paginacion;
using Matricula.Application.Cuentas;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Application.Estudiantes;

public class EstudianteService(
    IAppDbContext db,
    CuentaService cuentaService,
    UsuarioService usuarioService,
    TimeProvider timeProvider)
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

    /// <summary>
    /// RF-01: la persona define su contraseña y la cuenta siempre es de estudiante (RN-19). Cuenta y estudiante
    /// en un solo guardado; no se envía correo ni se inicia sesión.
    /// </summary>
    public async Task<MensajeResponse> RegistrarPublicoAsync(RegistroPublicoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // RN-21. Se valida antes que CuentaService, que respondería RN-13 por el correo. No se dice si el registro
        // existente está inactivo ni se dan sus ids: solo un administrador reactiva (RN-39).
        var cedula = request.Cedula.Trim();
        var correo = Usuario.NormalizarCorreo(request.Correo);
        if (await db.Estudiantes.AnyAsync(e => e.Cedula == cedula, cancellationToken)
            || await db.Usuarios.AnyAsync(u => u.Correo == correo, cancellationToken))
        {
            throw new BusinessRuleException(
                "RN-21",
                "Ya existe una cuenta con esa cédula o ese correo. Inicie sesión, solicite una contraseña temporal si "
                + "olvidó la suya o contacte a la administración.");
        }

        var usuario = await cuentaService.CrearCuentaPublicaAsync(request.Correo, request.Contrasena, cancellationToken);
        db.Estudiantes.Add(new Estudiante(
            usuario,
            request.Cedula,
            request.Nombre,
            request.Apellidos,
            request.Telefono,
            request.FechaNacimiento,
            request.Direccion,
            Hoy()));
        await db.SaveChangesAsync(cancellationToken);

        return new MensajeResponse("Registro completado. Ya puede iniciar sesión con su correo y contraseña.");
    }

    /// <summary>
    /// RF-16 y consulta 1: listado paginado, solo activos salvo que se pidan los inactivos (RN-41).
    /// </summary>
    public Task<PagedResult<EstudianteResponse>> ListarAsync(ListarEstudiantesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Estudiantes.AsNoTracking();

        if (!request.IncluirInactivos)
        {
            query = query.Where(e => e.Usuario.Activo);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
#pragma warning disable CA1304, CA1311, CA1862 // ToLower se traduce a lower() en SQL; la cultura no aplica.
            var termino = request.Search.Trim().ToLower();
            query = query.Where(e =>
                e.Nombre.ToLower().Contains(termino)
                || e.Apellidos.ToLower().Contains(termino)
                || e.Cedula.ToLower().Contains(termino)
                || e.Usuario.Correo.Contains(termino));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return query
            .OrderBy(e => e.Apellidos)
            .ThenBy(e => e.Nombre)
            .ThenBy(e => e.Id)
            .Select(EstudianteProyecciones.AResponse)
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <summary>
    /// RF-18: datos personales y correo de un estudiante. El correo se cambia en la cuenta (RN-31).
    /// La fecha de registro no se edita.
    /// </summary>
    public async Task<EstudianteResponse> ActualizarAsync(int id, ActualizarEstudianteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var estudiante = await db.Estudiantes
            .Include(e => e.Usuario)
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException($"No existe el estudiante {id}.");

        var cedula = request.Cedula.Trim();
        if (cedula != estudiante.Cedula
            && await db.Estudiantes.AnyAsync(e => e.Cedula == cedula && e.Id != id, cancellationToken))
        {
            throw new BusinessRuleException("RN-13", $"Ya existe un estudiante con la cédula {cedula}.");
        }

        await cuentaService.CambiarCorreoAsync(estudiante.Usuario, request.Correo, cancellationToken);
        estudiante.ActualizarDatos(
            request.Cedula, request.Nombre, request.Apellidos, request.Telefono, request.FechaNacimiento, request.Direccion);
        await db.SaveChangesAsync(cancellationToken);

        return estudiante.ToResponse();
    }

    public async Task<EstudianteResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await db.Estudiantes.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(EstudianteProyecciones.AResponse)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException($"No existe el estudiante {id}.");

    /// <summary>
    /// RF-19 y RN-38: el estado del estudiante es el de su cuenta, así que se desactiva la cuenta. El módulo
    /// matriculas añade RN-36 en UsuarioService, que aplica también a /usuarios/{id}/desactivar.
    /// </summary>
    public async Task DesactivarAsync(int id, CancellationToken cancellationToken) =>
        await usuarioService.DesactivarAsync(await ObtenerUsuarioIdAsync(id, cancellationToken), cancellationToken);

    /// <summary>
    /// RF-19, RN-38 y RN-39: un estudiante que regresa se reactiva, no se registra de nuevo.
    /// </summary>
    public async Task ReactivarAsync(int id, CancellationToken cancellationToken) =>
        await usuarioService.ReactivarAsync(await ObtenerUsuarioIdAsync(id, cancellationToken), cancellationToken);

    private async Task<int> ObtenerUsuarioIdAsync(int id, CancellationToken cancellationToken)
    {
        var usuarioId = await db.Estudiantes.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (int?)e.UsuarioId)
            .SingleOrDefaultAsync(cancellationToken);

        return usuarioId ?? throw new NotFoundException($"No existe el estudiante {id}.");
    }

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
