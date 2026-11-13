# Spec: errores

Módulo `errores` del [mapa de módulos](mapa-modulos.md). Depende de `acceso` (usuario de la petición y autorización del administrador) y de `auditoria` (comparte el identificador de operación), así que se construye después de `auditoria`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/errores/plan.md) | [Aprobadas](../tasks/errores/todo.md) | 4/6 tareas completadas |

**Historial:**
- 2026-11-13: v1 en revisión, con diez supuestos y sin preguntas abiertas. Las decisiones de alcance (qué se registra, un registro por ocurrencia, sin estado, 90 días de conservación con `ExecuteDeleteAsync`, sin errores de arranque) se tomaron antes de redactarla (decisión 12 del mapa).
- 2026-11-13: v1 aprobada con sus diez supuestos. Plan y lista de 6 tareas en revisión. Ajuste menor, sin cambio de alcance: el registro se construye dentro del `try` y no fuera, como muestra el ejemplo de Code Style, para que un fallo al construirlo tampoco afecte la respuesta (RN-48).
- 2026-11-13: plan y lista de tareas aprobados. T1 (dominio) y T2 (persistencia y migración) completadas.
- 2026-11-13: T3 (registro) completada, sin cambio de alcance. Dos correcciones a la verificación:
  - El registro público no envía correos, así que la fila "Registro público con smtp4dev detenido" se reemplazó por el olvido de contraseña, que también ocurre sin sesión.
  - Se confirmó el supuesto 5: el `ExceptionHandlerMiddleware` ya responde las peticiones canceladas por el cliente sin llamar al handler, y la comprobación del handler queda como defensa.
- 2026-11-13: T4 (depuración) completada. Cambio en el Tech Stack, aprobado por el usuario: `BackgroundService` no estaba disponible en Infrastructure, así que se agregó `Microsoft.Extensions.Hosting.Abstractions` 10.0.12 en lugar de mover la depuración a Api. El diseño no cambia. Se corrigió también la decisión 12 del mapa.

## Objetivo

Que los errores inesperados del sistema queden registrados de forma automática, consultable y relacionada con la auditoría. Hoy un `500` o un correo que no se pudo enviar solo se escriben en el log de consola, que se pierde al reiniciar y no se consulta desde la API. Con este módulo, cuando un usuario reporta "me salió un error", el administrador busca el código que vio, encuentra el error con su detalle técnico y, con el mismo código, los cambios que sí se guardaron en esa operación.

Cubre RF-58 y RF-59, las reglas RN-47 a RN-49, la consulta 12 de la sección 6 y la decisión 12 del mapa, con el diseño de `docs/arquitectura.md` §5.2.

**Usuarios:**
- Administrador: consulta el listado y el detalle de los errores.
- Estudiante y profesor: no los consultan (`403`), pero los errores que les ocurren quedan registrados con su usuario.
- Visitante: no los consulta (`401`). Sus errores quedan registrados sin usuario.

### Alcance

1. **Entidad `RegistroError`** (§5.13 de los requerimientos), en `Domain/Entities`. **No** hereda de `EntidadBase`: nunca se modifica, solo se elimina al vencer (RN-49).

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Id` | `long` | Identidad generada por PostgreSQL |
   | `Fecha` | `DateTimeOffset` | UTC, de `TimeProvider` |
   | `Origen` | `OrigenError` | Enum nuevo: `Peticion`, `Correo`. Se guarda como string |
   | `Descripcion` | `string` (500) | Resumen legible (punto 3) |
   | `TipoExcepcion` | `string` (300) | Nombre completo de la clase, por ejemplo `Npgsql.PostgresException` |
   | `Mensaje` | `string` (2000) | `exception.Message`, truncado |
   | `Detalle` | `string` (`text`) | `exception.ToString()`: tipo, mensaje, excepciones internas y stack trace. Truncado a 32 000 caracteres (supuesto 2) |
   | `Metodo` | `string?` (10) | Solo en `Peticion`. Por ejemplo `POST` |
   | `Ruta` | `string?` (500) | Solo en `Peticion`. `Request.Path`, **sin query string** (RN-47) |
   | `UsuarioId` | `int?` | De `ICurrentUser`. **Sin FK**. `null` = sin sesión |
   | `UsuarioRol` | `Rol?` | De `ICurrentUser`. Se guarda como string |
   | `TraceId` | `string` (32) | Hexadecimal en minúsculas. El mismo valor que `OperacionId` en la auditoría (punto 4) |

   Sin setters públicos: se crea completa por constructor, que aplica los truncados con las reglas puras del punto 6.

   Índices: `(fecha)` y `(trace_id)`.

   Migración `AgregaRegistroError`: tabla `registro_error` con sus índices, sin FK.

2. **Registro** (RF-58, RN-48):
   - Interfaz `IRegistroErrores` en `Application/Abstractions`:
     ```csharp
     Task RegistrarAsync(ErrorInesperado error);   // nunca lanza excepciones
     ```
     `ErrorInesperado` (record de Application) lleva `Origen`, `Descripcion`, la `Exception`, y `Metodo` y `Ruta` opcionales. No recibe `CancellationToken`: el de la petición puede venir cancelado y el registro no debe depender de él.
   - Implementación `RegistroErroresEnBd` en `Infrastructure/Persistence/Errores/`, registrada como `Scoped`:
     1. Toma el usuario de `ICurrentUser`, la fecha de `TimeProvider` y el `TraceId` de `Activity.Current`. Si no hay `Activity`, genera uno nuevo, como la auditoría.
     2. Abre un **scope nuevo** (`IServiceScopeFactory.CreateAsyncScope()`) y obtiene de él un `AppDbContext` propio. Nunca usa el contexto de la petición: tiene los cambios que fallaron y volvería a intentar guardarlos.
     3. Agrega el `RegistroError` y llama a `SaveChangesAsync` con un timeout de 5 segundos (`CancellationTokenSource`). Como `RegistroError` no hereda de `EntidadBase`, la captura de la auditoría no genera registros y el guardado es directo.
     4. Ante **cualquier** excepción, escribe un `LogWarning` ("No se pudo registrar el error {TraceId}") con la excepción del registro y termina sin relanzar. El error original ya está en el log desde antes (punto 3).

3. **Puntos de captura:** son dos. Cada uno escribe primero en el log, como hoy, y después registra el error.

   | Punto | Cuándo | `Origen` | `Descripcion` |
   |---|---|---|---|
   | `GlobalExceptionHandler`, rama `500` | Excepción no controlada, después del `LogError` existente | `Peticion` | `Error no controlado en {Metodo} {Ruta}` |
   | `EnvioCorreos.EnviarAsync`, `catch` | Fallo del proveedor de correo, después de `LogFalloEnvio` | `Correo` | `No se pudo enviar el correo de {tipo} a {destinatario}` (supuesto 4) |

   - `GlobalExceptionHandler` se registra como singleton (`AddExceptionHandler`), así que obtiene `IRegistroErrores` de `httpContext.RequestServices`, no por constructor.
   - **No se registran** (supuesto 5):
     - los `4xx`: validación, reglas, `404`, `401`, `403`, duplicados y concurrencia;
     - las peticiones canceladas por el cliente (`OperationCanceledException` con `RequestAborted` activo). El middleware de ASP.NET Core ya las responde con `499` sin llamar al handler; la spec deja una comprobación defensiva por si alguna llegara;
     - los errores al arrancar.
   - La respuesta al usuario no cambia: sigue siendo `500` con "Ocurrió un error inesperado." y el `traceId`. `EnvioCorreos` sigue devolviendo `false`.

4. **Identificador de operación:**
   - `TraceId` = `Activity.Current.TraceId.ToHexString()`, el mismo cálculo que `OperacionId` en `CapturaAuditoria`.
   - El `traceId` de los `ProblemDetails` tiene el formato W3C `00-<trace id>-<span id>-<flags>`. El filtro de la consulta acepta el valor completo, tal como lo ve el usuario, o solo los 32 caracteres del trace id (punto 6).
   - Así un error de correo se relaciona con los cambios que sí se guardaron en esa operación, por ejemplo con `GET /api/v1/auditoria?operacionId=<traceId>`.

5. **Depuración** (RN-49):
   - `ErroresOptions` (sección `Errores`) con `DiasRetencion`, 90 por defecto en `appsettings.json`. Se valida al arrancar que sea mayor o igual que 1 (`ValidateOnStart`).
   - `DepuracionErrores : BackgroundService` en `Infrastructure/Persistence/Errores/`:
     - Se ejecuta al arrancar y después cada 24 horas, con `PeriodicTimer` sobre el `TimeProvider` inyectado.
     - En cada ejecución abre un scope y elimina con `ExecuteDeleteAsync` los registros con `Fecha` anterior al límite que calcula `ReglasErrores.LimiteRetencion(ahora, dias)` (punto 6). Escribe en el log cuántos eliminó (`LogInformation`).
     - **Captura sus propias excepciones** (salvo la cancelación al detenerse) y solo las escribe en el log con `LogError`. En .NET 10 una excepción no capturada en un `BackgroundService` detiene la aplicación, y una BD caída no debe tumbar la API. Estos fallos no se registran en la tabla.
   - Es la **única** escritura fuera del change tracker que se permite (decisión 12 del mapa, `docs/arquitectura.md` §5).

6. **Reglas puras** en `Domain/Reglas/ReglasErrores.cs`, probadas sin BD:
   - `Truncar(texto, maximo)`: devuelve el texto tal cual si cabe. Si no, lo corta y termina en `"…"`, sin pasarse del máximo. `null` → `string.Empty`.
   - `NormalizarTraceId(valor)`:
     - Acepta 32 caracteres hexadecimales, o el formato W3C `00-<32 hex>-<16 hex>-<2 hex>`, del que extrae el trace id.
     - Devuelve el trace id en minúsculas, o `null` si el formato no es válido.
     - Ignora los espacios al inicio y al final.
   - `LimiteRetencion(ahora, diasRetencion)`: `ahora - diasRetencion días`. Lanza `ArgumentOutOfRangeException` si `diasRetencion < 1`.
   - Las longitudes máximas (`MaxDescripcion`, `MaxMensaje`, `MaxDetalle`, …) son constantes de `RegistroError`, que también usa la configuración EF.

7. **Endpoints de consulta** (RF-59, consulta 12), en `ErroresController`, con `[Authorize(Policy = Politicas.Administrador)]`:

   | Método y ruta | RF / RN | Respuesta |
   |---|---|---|
   | `GET /api/v1/errores` | RF-59 | `200` paginado · `400` · `401` · `403` |
   | `GET /api/v1/errores/{id}` | RF-59 | `200` · `401` · `403` · `404` (no existe o ya se depuró) |

   Parámetros del listado (`ListarErroresRequest : PaginacionRequest`), todos opcionales y combinables:

   | Parámetro | Tipo | Filtro |
   |---|---|---|
   | `desde`, `hasta` | `DateOnly` | Fechas locales del instituto, ambas incluidas, con `IFechaLocal.RangoUtc` (como la auditoría) |
   | `origen` | `OrigenError` | `Peticion` o `Correo` |
   | `traceId` | `string` | Normalizado con `ReglasErrores.NormalizarTraceId` (punto 6) |
   | `search` | `string` | Contiene, sin distinguir mayúsculas, en `Mensaje`, `TipoExcepcion` o `Ruta` (supuesto 7) |

   Respuesta del listado (`RegistroErrorResponse`), ordenada por `Fecha` descendente e `Id` descendente, **sin `detalle`**, para que la página pese poco:
   ```json
   {
     "id": 15, "fecha": "2026-11-13T21:04:05Z", "origen": "Peticion",
     "descripcion": "Error no controlado en GET /api/v1/aulas",
     "tipoExcepcion": "Npgsql.PostgresException",
     "mensaje": "42703: column a.nombre does not exist",
     "metodo": "GET", "ruta": "/api/v1/aulas",
     "usuarioId": 1, "usuarioRol": "Administrador",
     "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
   }
   ```
   Respuesta del detalle (`RegistroErrorDetalleResponse`): los mismos campos más `detalle`.

8. **Lectura desde Application:**
   - `IAppDbContext` expone `IQueryable<RegistroError> RegistrosError` (solo lectura, `AsNoTracking`), no un `DbSet`, con la misma implementación explícita que `RegistrosAuditoria`. Así ningún servicio puede agregar, modificar ni eliminar registros (RN-49): solo `RegistroErroresEnBd` y `DepuracionErrores`, en Infrastructure, escriben con `Set<RegistroError>()`.
   - `ErroresService` tiene dos métodos:
     - `ListarAsync`: filtra, ordena, proyecta y pagina con `ToPagedResultAsync`.
     - `ObtenerAsync(id)`: proyecta el detalle o lanza `NotFoundException`.

9. **Validación** (`ListarErroresRequestValidator`), con `400` y el campo:
   - `desde ≤ hasta` si vienen las dos.
   - `traceId` con un formato que `NormalizarTraceId` no reconoce. El mensaje explica los dos formatos válidos.
   - `origen` inválido: lo rechaza el enlace del modelo, como los demás enums.

10. **Ajustes en el código existente:**
    - `GlobalExceptionHandler` y `EnvioCorreos`: la llamada a `IRegistroErrores` (punto 3).
    - `AppDbContext`: la propiedad `RegistrosError` de `IAppDbContext`. La entidad entra al modelo por su configuración, como `RegistroAuditoria`.
    - `Infrastructure/DependencyInjection.cs`: `IRegistroErrores`, `ErroresOptions` y `DepuracionErrores` (`AddHostedService`).
    - `appsettings.json`: sección `"Errores": { "DiasRetencion": 90 }`.
    - `Matricula.Api.http`: ejemplos del listado y del detalle.
    - `AGENTS.md`:
      - Rangos RF-01..RF-59 y RN-01..RN-49.
      - En "Nunca": la excepción acotada de `ExecuteDeleteAsync` para la depuración de `registro_error`, y guardar en el registro de errores el body, los headers o el query string.

### Fuera de alcance

- Estado o seguimiento de los errores (revisado, resuelto, asignado), alertas y notificaciones (decisión 12).
- Agrupar errores iguales o contar ocurrencias (decisión 12).
- Errores al arrancar la aplicación y fallos de la propia depuración: quedan solo en el log.
- Las respuestas `4xx` y las peticiones canceladas por el cliente.
- Cambios en el frontend, por ejemplo mostrar el `traceId` al usuario como "código de error". La API ya lo envía en los `ProblemDetails`.
- Serilog, OpenTelemetry o servicios externos de monitoreo.
- Pruebas de integración: diferidas, como en los demás módulos.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, xUnit 2.9.3, `PeriodicTimer` del framework. **Un paquete nuevo:** `Microsoft.Extensions.Hosting.Abstractions` 10.0.12 en Infrastructure, para `BackgroundService`, que Infrastructure no tenía (aprobado en T4).

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaRegistroError --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
smtp4dev --smtpport=2525 --urls=http://localhost:5080   # detenerlo para provocar fallos de correo
```

## Project Structure

```
src/Matricula.Domain/
  Entities/RegistroError.cs                   nuevo (no hereda de EntidadBase)
  Enums/OrigenError.cs                        nuevo
  Reglas/ReglasErrores.cs                     nuevo: truncado, trace id, límite de conservación
src/Matricula.Application/
  Abstractions/IAppDbContext.cs               + IQueryable<RegistroError> RegistrosError
  Abstractions/IRegistroErrores.cs            nuevo
  Errores/                                    ErrorInesperado, ErroresService, ListarErroresRequest (+ validador),
                                              RegistroErrorResponse, RegistroErrorDetalleResponse
  Correos/EnvioCorreos.cs                     + registro del fallo de envío
  DependencyInjection.cs                      + ErroresService
src/Matricula.Infrastructure/
  Persistence/AppDbContext.cs                 + IAppDbContext.RegistrosError (IQueryable)
  Persistence/Errores/RegistroErroresEnBd.cs  nuevo: guardado en un scope aparte, sin lanzar
  Persistence/Errores/DepuracionErrores.cs    nuevo: BackgroundService con ExecuteDeleteAsync
  Persistence/Errores/ErroresOptions.cs       nuevo
  Persistence/Configurations/RegistroErrorConfiguration.cs   nuevo
  Persistence/Migrations/                     AgregaRegistroError
  DependencyInjection.cs                      + IRegistroErrores, opciones y hosted service
src/Matricula.Api/
  Common/GlobalExceptionHandler.cs            + registro en la rama 500
  Controllers/ErroresController.cs            nuevo
  appsettings.json                            + Errores:DiasRetencion
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/{ReglasErroresTests,RegistroErrorTests}.cs
AGENTS.md                                     rangos RF/RN, excepción de ExecuteDelete y prohibición de body/headers
```

## Code Style

Igual que los módulos anteriores: constructores primarios, un archivo por clase, setters privados, reglas puras en el dominio, `AsNoTracking` y `Select` en las lecturas, `[LoggerMessage]` para los logs, pruebas `Metodo_Escenario_Resultado`.

```csharp
public async Task RegistrarAsync(ErrorInesperado error)
{
    var registro = new RegistroError(timeProvider.GetUtcNow(), error.Origen, error.Descripcion, error.Excepcion,
        error.Metodo, error.Ruta, currentUser.UsuarioId, LeerRol(), TraceIdActual());
    try
    {
        // Contexto propio: el de la petición tiene los cambios que fallaron (docs/arquitectura.md §5.2).
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        db.Set<RegistroError>().Add(registro);
        await db.SaveChangesAsync(timeout.Token);
    }
#pragma warning disable CA1031 // RN-48: registrar un error nunca puede afectar la respuesta.
    catch (Exception e)
#pragma warning restore CA1031
    {
        LogFalloRegistro(logger, e, registro.TraceId);
    }
}
```

```csharp
[Theory]
[InlineData("00-4BF92F3577B34DA6A3CE929D0E0E4736-00f067aa0ba902b7-01")]
[InlineData(" 4bf92f3577b34da6a3ce929d0e0e4736 ")]
public void NormalizarTraceId_FormatoValido_DevuelveTraceIdEnMinusculas(string valor)
{
    Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", ReglasErrores.NormalizarTraceId(valor));
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD):
  - `ReglasErrores.Truncar`:
    - devuelve igual un texto que cabe;
    - corta en el máximo con `"…"` sin pasarse;
    - `null` → vacío.
  - `ReglasErrores.NormalizarTraceId`:
    - acepta los 32 caracteres hexadecimales y el formato W3C;
    - devuelve minúsculas y quita espacios;
    - rechaza longitudes incorrectas, caracteres no hexadecimales y un W3C mal formado.
  - `ReglasErrores.LimiteRetencion`: resta los días exactos y rechaza `0` y valores negativos.
  - `RegistroError`:
    - el constructor trunca `Descripcion`, `Mensaje` y `Detalle` a sus máximos;
    - guarda el nombre completo del tipo de la excepción;
    - exige un `TraceId` no vacío.
- **Manual**, con la API y PostgreSQL en marcha, usando `Matricula.Api.http`, Scalar, smtp4dev y `psql`:

| Qué | Esperado |
|---|---|
| Provocar un `500`: `ALTER TABLE aula RENAME COLUMN nombre TO nombre_x;` en `psql`, `GET /api/v1/aulas` como admin y revertir el `ALTER` | La respuesta sigue siendo `500` genérica con `traceId`. Aparece un registro `Peticion` con método, ruta, `Npgsql.PostgresException`, el admin como usuario y el trace id de la respuesta |
| El mismo `500` con un query string (`/api/v1/aulas?search=algo`) | `ruta` sin `?search=algo` |
| Detener smtp4dev y reenviar una temporal (`POST /api/v1/usuarios/{id}/reenviar-temporal`) | La operación responde igual que hoy. Aparece un registro `Correo` con la descripción del envío. `GET /auditoria?operacionId=<traceId>` muestra el cambio de la temporal de la misma operación |
| Olvido de contraseña con smtp4dev detenido (sin sesión) | Respuesta `200` de RN-28. Registro `Correo` sin usuario |
| Login con contraseña incorrecta, matrícula rechazada (`409`), validación (`400`), sin token (`401`) | Ningún registro nuevo |
| Buscar en `registro_error` la contraseña usada, `Bearer`, un hash o un valor del query string | No aparece |
| Con la tabla `registro_error` renombrada en `psql`, provocar un `500` | La respuesta sigue siendo `500` normal y el log muestra el `LogError` original y el `LogWarning` del registro fallido; la API no se cae |
| `UPDATE registro_error SET fecha = now() - interval '91 days' WHERE id = …` en `psql` y reiniciar la API | El registro se elimina al arrancar; el log indica cuántos se eliminaron. Uno de 89 días se conserva |
| `Errores:DiasRetencion = 0` | La API no arranca, con un mensaje claro |
| `GET /errores?traceId=` con el `traceId` completo de los `ProblemDetails` y con solo los 32 caracteres | Ambos encuentran el mismo registro |
| `GET /errores?origen=Correo`, `desde`/`hasta` y `search` combinados | Filtra y pagina, del más reciente al más antiguo, sin `detalle` |
| `GET /errores/{id}` | Incluye `detalle` con el stack trace; `404` si el id no existe |
| `traceId=abc` · `desde > hasta` · `origen=Otro` | `400` con el campo |
| Con token de estudiante o profesor / sin token / con cambio de contraseña pendiente | `403` / `401` / `403` (RN-24) |
| Crear un curso después de todo lo anterior | La auditoría no muestra registros de `RegistroError` |

## Boundaries

- **Siempre:**
  - `dotnet build` sin warnings y pruebas unitarias en verde.
  - Primero el log y después el registro, en un scope aparte.
  - Si el registro falla, no se lanza la excepción (RN-48).
  - Las reglas de truncado y del trace id en el dominio, probadas.
  - Las fechas del filtro en hora local del instituto.
- **Preguntar antes:**
  - Añadir paquetes (Serilog, OpenTelemetry, Sentry).
  - Registrar otros orígenes, como los `4xx`, el arranque o tareas en segundo plano.
  - Agregar estado o endpoints de escritura.
  - Cambiar el esquema fuera de esta spec.
  - Usar `ExecuteUpdate` o `ExecuteDelete` en cualquier otro lugar.
- **Nunca:**
  - Guardar el body, los headers (en especial `Authorization`), el query string, contraseñas, temporales o tokens (RN-47).
  - Registrar con el `AppDbContext` de la petición.
  - Relanzar o propagar una excepción del registro o de la depuración.
  - Mostrar el `detalle` al usuario en los `ProblemDetails`.
  - Activar `Include Error Detail` en la cadena de conexión.
  - FK desde `registro_error`.
  - Endpoints o métodos de servicio que creen, modifiquen o eliminen registros de errores.

## Success Criteria

1. La migración crea `registro_error` sin FK y con sus índices. Application solo puede leerla (`IQueryable`), y ningún endpoint la modifica.
2. Cada `500` no controlado y cada envío de correo fallido deja su registro, con fecha, origen, descripción, tipo, mensaje, detalle, usuario y el `TraceId` de la petición (RF-58).
3. Ninguna respuesta `4xx` ni petición cancelada deja registro, y ningún registro contiene body, headers, query string ni secretos (RN-47).
4. La respuesta al usuario y el resultado de la operación no cambian con el registro. Si el registro falla, la API responde igual y el error queda en el log (RN-48).
5. Los registros más antiguos que `Errores:DiasRetencion` se eliminan al arrancar y cada 24 horas, y un fallo de la depuración no detiene la API (RN-49).
6. `GET /api/v1/errores` filtra por fechas, origen, `traceId` (en sus dos formatos) y texto, y pagina. `GET /api/v1/errores/{id}` devuelve el detalle. Ambos solo los usa el administrador (RF-59).
7. Las verificaciones manuales de la tabla dan el resultado esperado.
8. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **El registro es síncrono dentro de la petición que falló** (con un timeout de 5 segundos), no en una cola en segundo plano. Solo agrega latencia a las respuestas que ya fallaron, y una cola perdería los errores si la API se detiene.
2. **El `Detalle` se trunca a 32 000 caracteres.** Un stack trace normal ocupa unos pocos miles; el límite evita que una excepción anómala llene la tabla.
3. **No se guarda el correo del usuario,** solo su id y rol. A diferencia de la auditoría, no se consulta la BD, que puede ser la causa del error. El administrador puede buscar la cuenta por id.
4. **El fallo de correo guarda el destinatario en la descripción,** igual que el log actual. Es un dato personal, pero solo lo ve el administrador y le permite saber a quién reenviar la temporal. Nunca se guarda el contenido del correo, que puede incluir la temporal.
5. **Una cancelación del cliente no es un error.** Si al handler llegara una `OperationCanceledException` con `RequestAborted` activo, responde como hoy, sin registrar. Se verifica al implementar si el middleware ya las filtra; si es así, la comprobación queda solo como defensa.
6. **Los errores dentro de la depuración no se registran en la tabla,** solo en el log, para no escribir en una BD que acaba de fallar.
7. **`search` busca en `Mensaje`, `TipoExcepcion` y `Ruta`,** con `ToLower().Contains()` como los demás listados. No busca en `Detalle`, que es largo y no tiene índice.
8. **Los `TraceId` pueden repetirse:** un error de correo y un `500` posterior de la misma petición comparten el trace id. Por eso `trace_id` tiene un índice no único.
9. **La depuración se ejecuta al arrancar** además de cada 24 horas, para no esperar un día después de cada reinicio, que en desarrollo es frecuente.
10. **El primer registro es el primer error después del despliegue.** No se importan errores anteriores del log.

## Decisiones

Se aplican las decisiones 1, 5, 9 y 12 del mapa, y el diseño de `docs/arquitectura.md` §5.2.

## Preguntas abiertas

No quedan preguntas abiertas: las de alcance se resolvieron antes de redactar la spec (decisión 12 del mapa).
