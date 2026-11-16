using System.Globalization;
using Bogus;
using Matricula.Application.Abstractions;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Siembra datos de prueba al arrancar (spec datos-prueba): solo con el flag activo, en Development y sobre una BD sin
/// datos de negocio. Todo en una transacción y con el dominio: la auditoría registra cada dato con autor Sistema.
/// </summary>
public partial class DatosPruebaSeedService(
    AppDbContext db,
    IOptions<DatosPruebaOptions> options,
    IHasherContrasenas hasher,
    IFechaLocal fechaLocal,
    TimeProvider timeProvider,
    ILogger<DatosPruebaSeedService> logger)
{
    private const double ProporcionEstudiantesDesactivados = 0.1;

    private readonly DatosPruebaOptions _options = options.Value;

    public async Task EjecutarAsync(bool esDesarrollo, CancellationToken cancellationToken)
    {
        if (!_options.Habilitado)
        {
            return;
        }

        if (!esDesarrollo)
        {
            LogFueraDeDesarrollo(logger);
            return;
        }

        var errores = _options.Validar();
        if (errores.Count > 0)
        {
            throw new InvalidOperationException(
                "La configuración de los datos de prueba no es válida:" + Environment.NewLine
                + string.Join(Environment.NewLine, errores.Select(e => "- " + e)));
        }

        if (await HayDatosDeNegocioAsync(cancellationToken))
        {
            LogBdConDatos(logger);
            return;
        }

        var semilla = _options.Semilla ?? Random.Shared.Next();
        var faker = new Faker("es") { Random = new Randomizer(semilla) };
        var hoy = fechaLocal.Hoy();

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        var datos = await SembrarCatalogosYPersonasAsync(faker, hoy, cancellationToken);
        await SembrarGruposAsync(faker, datos, cancellationToken);
        await SembrarMatriculasAsync(faker, datos, hoy, cancellationToken);

        await transaccion.CommitAsync(cancellationToken);

        EscribirResumen(semilla, datos);
    }

    /// <summary>Segundo guardado: los grupos de los tres periodos con sus horarios.</summary>
    private async Task SembrarGruposAsync(Faker faker, DatosSembrados datos, CancellationToken cancellationToken)
    {
        new SembradorGrupos(faker, datos, periodo => LogGrupoSinEspacio(logger, periodo))
            .Sembrar(_options.GruposPorPeriodo);

        db.Grupos.AddRange(datos.Grupos.Select(g => g.Grupo));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Tercer guardado: matrículas, retiros y estados finales de los grupos (ya rastreados por EF).</summary>
    private async Task SembrarMatriculasAsync(
        Faker faker, DatosSembrados datos, DateOnly hoy, CancellationToken cancellationToken)
    {
        new SembradorMatriculas(faker, datos, hoy).Sembrar(_options.MaxMatriculasPorEstudiante);

        db.Matriculas.AddRange(datos.Matriculas.Select(m => m.Matricula));
        await db.SaveChangesAsync(cancellationToken);
    }

    private void EscribirResumen(int semilla, DatosSembrados datos)
    {
        LogCatalogosYPersonas(logger, semilla, datos.Cursos.Count, datos.Aulas.Count, datos.Profesores.Count,
            datos.Estudiantes.Count, datos.EstudiantesDesactivados.Count);

        var gruposPorPeriodo = string.Join(", ", datos.Grupos
            .GroupBy(g => g.Periodo.Nombre)
            .Select(p => $"{p.Key}: {string.Join(" ", p.GroupBy(g => g.Grupo.Estado).Select(e => $"{e.Count()} {e.Key}"))}"));
        LogGrupos(logger, datos.Grupos.Count, gruposPorPeriodo, datos.GruposSinEspacio);

        var activas = datos.Matriculas.Count(m => m.Matricula.Estado == EstadoMatricula.Activa);
        var retiradas = string.Join(", ", datos.Matriculas
            .Where(m => m.Matricula.MotivoRetiro is not null)
            .GroupBy(m => m.Matricula.MotivoRetiro)
            .Select(g => $"{g.Count()} {g.Key}"));
        var lleno = datos.Grupos.FirstOrDefault(g => g.Lleno)?.Grupo;
        var ocupacionLleno = lleno is null ? "sin grupo lleno" : $"{lleno.CuposOcupados}/{lleno.Cupo}";
        LogMatriculas(logger, datos.Matriculas.Count, activas, retiradas, ocupacionLleno, datos.HuboCambioDeGrupo);
    }

    /// <summary>
    /// El administrador inicial no cuenta: lo crea el seed del administrador antes (supuesto 3 de la spec).
    /// </summary>
    private async Task<bool> HayDatosDeNegocioAsync(CancellationToken cancellationToken) =>
        await db.Cursos.AnyAsync(cancellationToken)
        || await db.Aulas.AnyAsync(cancellationToken)
        || await db.Periodos.AnyAsync(cancellationToken)
        || await db.Profesores.AnyAsync(cancellationToken)
        || await db.Estudiantes.AnyAsync(cancellationToken)
        || await db.Grupos.AnyAsync(cancellationToken)
        || await db.Matriculas.AnyAsync(cancellationToken);

    /// <summary>Primer guardado: cursos, aulas, los tres periodos y las personas con sus cuentas.</summary>
    private async Task<DatosSembrados> SembrarCatalogosYPersonasAsync(
        Faker faker, DateOnly hoy, CancellationToken cancellationToken)
    {
        var fechas = FechasPrueba.Calcular(hoy);
        var datos = new DatosSembrados
        {
            Pasado = CrearPeriodo(fechas.Pasado),
            Vigente = CrearPeriodo(fechas.Vigente),
            Futuro = CrearPeriodo(fechas.Futuro),
        };
        db.Periodos.AddRange(datos.Pasado, datos.Vigente, datos.Futuro);

        datos.Cursos.AddRange(CrearCursos(faker));
        db.Cursos.AddRange(datos.Cursos);

        datos.Aulas.AddRange(Enumerable.Range(0, _options.Aulas).Select(i => CrearAula(faker, i)));
        db.Aulas.AddRange(datos.Aulas);

        // Un solo hash para todas las cuentas: incluye su sal y verifica igual, y hashear miles de veces (PBKDF2)
        // tardaría minutos (plan, decisiones de implementación).
        var hash = hasher.Hashear(_options.Contrasena!);
        var ahora = timeProvider.GetUtcNow();

        // El administrador inicial también recibe la contraseña de prueba como definitiva, para usarlo de inmediato
        // (punto 6 de la spec). Anula su temporal, así que el AdminSeed deja de renovarla.
        var administrador = await db.Usuarios
            .FirstAsync(u => u.Rol == Rol.Administrador && u.CreadoPorId == null, cancellationToken);
        administrador.EstablecerContrasena(hash, ahora);
        var administradorId = administrador.Id;
        var personas = new GeneradorPersonas(faker);

        for (var i = 1; i <= _options.Profesores; i++)
        {
            datos.Profesores.Add(CrearProfesor(faker, personas, i, administradorId, hash, ahora, hoy));
        }

        // El último profesor queda desactivado, si hay más que las dos cuentas fijas.
        if (datos.Profesores.Count > 2)
        {
            datos.ProfesorDesactivado = datos.Profesores[^1];
            datos.ProfesorDesactivado.Usuario.Desactivar();
        }

        db.Profesores.AddRange(datos.Profesores);

        var finRegistro = fechas.Pasado.InicioMatricula.AddDays(-1);
        for (var i = 1; i <= _options.Estudiantes; i++)
        {
            datos.Estudiantes.Add(CrearEstudiante(faker, personas, i, administradorId, hash, ahora, hoy, finRegistro));
        }

        // Alrededor de un 10 %, nunca las cuentas fijas.
        var candidatos = datos.Estudiantes.Skip(2).ToList();
        var cantidadDesactivados = Math.Min(
            (int)Math.Round(datos.Estudiantes.Count * ProporcionEstudiantesDesactivados), candidatos.Count);
        foreach (var estudiante in faker.PickRandom(candidatos, cantidadDesactivados))
        {
            estudiante.Usuario.Desactivar();
            datos.EstudiantesDesactivados.Add(estudiante);
        }

        db.Estudiantes.AddRange(datos.Estudiantes);

        await db.SaveChangesAsync(cancellationToken);
        return datos;
    }

    private static Periodo CrearPeriodo(PeriodoPrueba p) =>
        new(p.Nombre, p.InicioClases, p.FinClases, p.InicioMatricula, p.FinMatricula, p.FechaLimiteRetiro);

    /// <summary>
    /// Cursos del catálogo sin repetir y, si se piden más, repetidos con sufijo ("II", "III"…). El código es el prefijo
    /// del área y un número correlativo por área: INF-101, INF-102…
    /// </summary>
    private List<Curso> CrearCursos(Faker faker)
    {
        var catalogo = CatalogosPrueba.Cursos;
        var correlativos = new Dictionary<string, int>();
        var cursos = new List<Curso>();

        for (var vuelta = 0; cursos.Count < _options.Cursos; vuelta++)
        {
            var sufijo = vuelta < CatalogosPrueba.Sufijos.Count ? CatalogosPrueba.Sufijos[vuelta] : $" {vuelta + 1}";

            foreach (var (prefijo, nombreBase) in faker.Random.Shuffle(catalogo))
            {
                if (cursos.Count == _options.Cursos)
                {
                    break;
                }

                var correlativo = correlativos[prefijo] = correlativos.GetValueOrDefault(prefijo, 100) + 1;
                var nombre = nombreBase + sufijo;
                var descripcion = string.Format(
                    CultureInfo.InvariantCulture,
                    faker.PickRandom<string>(CatalogosPrueba.Descripciones),
                    nombre);

                cursos.Add(new Curso(
                    $"{prefijo}-{correlativo}",
                    nombre,
                    descripcion,
                    duracionHoras: faker.Random.Number(5, 20) * 4,
                    costo: faker.Random.Number(5, 30) * 5000m,
                    faker.PickRandom<NivelCurso>()));
            }
        }

        return cursos;
    }

    /// <summary>Código A-101, A-102… cinco aulas por piso, cuatro pisos por edificio.</summary>
    private static Aula CrearAula(Faker faker, int indice)
    {
        var edificio = (char)('A' + (indice / 20));
        var piso = 1 + (indice % 20 / 5);
        var numero = 1 + (indice % 5);

        return new Aula(
            $"{edificio}-{piso}{numero:00}",
            $"Edificio {edificio}, piso {piso}",
            capacidad: faker.Random.Number(15, 40));
    }

    private static Profesor CrearProfesor(
        Faker faker, GeneradorPersonas personas, int indice, int administradorId, string hash, DateTimeOffset ahora,
        DateOnly hoy)
    {
        var nombre = personas.Nombre();
        var apellidos = personas.Apellidos();
        var fijo = indice <= 2 ? $"profesor{indice}" : null;
        var usuario = new Usuario(
            personas.Correo(nombre, apellidos, fijo), Rol.Profesor, OrigenRegistro.Administrador, administradorId);
        AsignarContrasena(usuario, indice, hash, ahora);

        return new Profesor(
            usuario,
            personas.Cedula(),
            nombre,
            apellidos,
            personas.Telefono(),
            faker.PickRandom<string>(CatalogosPrueba.Especialidades),
            hoy.AddDays(-faker.Random.Number(0, 3650)));
    }

    private static Estudiante CrearEstudiante(
        Faker faker, GeneradorPersonas personas, int indice, int administradorId, string hash, DateTimeOffset ahora,
        DateOnly hoy, DateOnly finRegistro)
    {
        var nombre = personas.Nombre();
        var apellidos = personas.Apellidos();
        var fijo = indice <= 2 ? $"estudiante{indice}" : null;
        var origen = faker.Random.Bool() ? OrigenRegistro.RegistroPublico : OrigenRegistro.Administrador;
        var usuario = new Usuario(
            personas.Correo(nombre, apellidos, fijo),
            Rol.Estudiante,
            origen,
            origen == OrigenRegistro.Administrador ? administradorId : null);
        AsignarContrasena(usuario, indice, hash, ahora);

        var nacimiento = hoy.AddYears(-faker.Random.Number(16, 59)).AddDays(-faker.Random.Number(0, 364));
        var registro = finRegistro.AddDays(-faker.Random.Number(0, 164));

        return new Estudiante(
            usuario,
            personas.Cedula(),
            nombre,
            apellidos,
            personas.Telefono(),
            nacimiento,
            personas.Direccion(),
            registro);
    }

    /// <summary>
    /// Las cuentas fijas número 2 quedan con el cambio obligatorio pendiente (RN-24): sin definitiva y con una temporal
    /// igual a la contraseña de prueba. Las demás, con la contraseña como definitiva. Nunca se envían correos.
    /// </summary>
    private static void AsignarContrasena(Usuario usuario, int indice, string hash, DateTimeOffset ahora)
    {
        if (indice == 2)
        {
            usuario.AsignarTemporal(hash, MotivoTemporal.CuentaNueva, ahora);
        }
        else
        {
            usuario.EstablecerContrasena(hash, ahora);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "'DatosPrueba:Habilitado' está activo fuera de Development; no se siembran datos de prueba")]
    private static partial void LogFueraDeDesarrollo(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "La BD ya tiene datos; no se siembran datos de prueba")]
    private static partial void LogBdConDatos(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Datos de prueba sembrados con la semilla {Semilla}: {Cursos} cursos, {Aulas} aulas, 3 periodos, "
            + "{Profesores} profesores y {Estudiantes} estudiantes ({Desactivados} desactivados)")]
    private static partial void LogCatalogosYPersonas(
        ILogger logger, int semilla, int cursos, int aulas, int profesores, int estudiantes, int desactivados);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Grupos de prueba: {Total} ({PorPeriodo}); {SinEspacio} sin crear por falta de franjas")]
    private static partial void LogGrupos(ILogger logger, int total, string porPeriodo, int sinEspacio);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Matrículas de prueba: {Total} ({Activas} activas; retiradas: {Retiradas}); grupo lleno {Lleno}; "
            + "cambio de grupo: {CambioDeGrupo}")]
    private static partial void LogMatriculas(
        ILogger logger, int total, int activas, string retiradas, string lleno, bool cambioDeGrupo);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No quedan franjas con profesor y aula libres en el periodo {Periodo}: un grupo de prueba no se creó")]
    private static partial void LogGrupoSinEspacio(ILogger logger, string periodo);
}
