# Plan: errores

Módulo `errores` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-errores.md`](../../specs/SPEC-errores.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-11-13)

## Enfoque

Igual que en `auditoria`, el orden es:
1. La base sin endpoints: las reglas puras en el dominio, con pruebas unitarias, y después la persistencia, verificada contra la BD.
2. El registro, que es el corazón del módulo.
3. La depuración.
4. La consulta.

Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

Lo delicado de este módulo es que trabaja justo cuando algo ya falló. Por eso el registro (T3) no se verifica solo en el caso feliz: también se fuerza que el propio registro falle, renombrando la tabla con `psql`, y se comprueba que la API responde igual. La depuración (T4) va antes que la consulta, porque es la otra pieza que escribe en la tabla y porque la consulta necesita registros reales, que T3 ya deja.

1. **Dominio:** la entidad, el truncado, la normalización del trace id y el límite de conservación (RN-47, RN-49). No necesitan BD.
2. **Persistencia:** la tabla, la lectura de solo lectura para Application y la migración. Todavía no se escribe nada en la tabla.
3. **Registro** desde `GlobalExceptionHandler` y `EnvioCorreos`, en un scope aparte y sin lanzar excepciones (RF-58, RN-47, RN-48).
4. **Depuración** con `BackgroundService` y `ExecuteDeleteAsync` (RN-49).
5. **Consulta** `GET /api/v1/errores` y `GET /api/v1/errores/{id}` (RF-59).
6. **Cierre:** `AGENTS.md`, el recorrido completo de la tabla de verificación de la spec y la actualización de los estados.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio:** `OrigenError`, `RegistroError`, `ReglasErrores` | — | Solo pruebas unitarias |
| 2 | **Persistencia:** configuración, `IQueryable` en `IAppDbContext`, migración `AgregaRegistroError` | 1 | La tabla queda vacía |
| 3 | **Registro:** `IRegistroErrores`, `ErrorInesperado`, `RegistroErroresEnBd`, llamadas desde el handler y el envío de correos | 1, 2 | Verificación con `psql`, smtp4dev detenido y una columna renombrada |
| 4 | **Depuración:** `ErroresOptions`, `DepuracionErrores`, `appsettings.json` | 2 | Usa registros de T3 con la fecha retrocedida |
| 5 | **Consulta:** listado y detalle | 2, 3 | Necesita registros reales para verificarse |
| 6 | **Cierre del módulo** | todos | `AGENTS.md`, tabla de verificación completa, estados |

**Paralelismo:** ninguno útil. T4 y T5 solo dependen de T2, pero las dos se verifican con los registros de T3, y T4 y T3 tocan los mismos archivos de Infrastructure (`DependencyInjection.cs` y `Persistence/Errores/`). Se hacen en serie.

## Decisiones de implementación

- **Dónde vive cada cosa:**
  - `Domain/Reglas/ReglasErrores.cs`: lógica pura, sin EF ni ASP.NET: `Truncar`, `NormalizarTraceId` y `LimiteRetencion`.
  - `Domain/Entities/RegistroError.cs`:
    - Recibe la `Exception` en su constructor y obtiene de ella `TipoExcepcion` (`GetType().FullName`), `Mensaje` (`Message`) y `Detalle` (`ToString()`), truncados con `ReglasErrores.Truncar`.
    - Así el truncado y lo que se extrae de la excepción se prueban sin BD.
    - `Exception` es parte de `System`: el dominio no gana dependencias.
  - `Application/Abstractions/IRegistroErrores.cs` y `Application/Errores/ErrorInesperado.cs`: el contrato que usan el handler y `EnvioCorreos`.
  - `Infrastructure/Persistence/Errores/`: `RegistroErroresEnBd`, `DepuracionErrores` y `ErroresOptions`. Son los únicos que escriben en la tabla.
- **Longitudes** (constantes públicas de `RegistroError`, que usa también la configuración EF):
  - `Descripcion` 500, `TipoExcepcion` 300, `Mensaje` 2000, `Detalle` 32 000, `Metodo` 10, `Ruta` 500, `TraceId` 32.
  - `Detalle` es `text`. Su límite lo pone el truncado, no la columna.
- **Truncado:** si el texto supera el máximo, se corta en `maximo - 1` y se agrega `"…"`, así el resultado mide exactamente `maximo`. `Metodo` y `Ruta` también se truncan, porque una ruta anómala no debe hacer fallar el registro.
- **`NormalizarTraceId`:**
  - Recorta los espacios y acepta dos formatos:
    - 32 caracteres hexadecimales.
    - El W3C `00-<32 hex>-<16 hex>-<2 hex>` (55 caracteres), del que toma el segundo segmento.
  - Devuelve minúsculas, o `null` si el formato no es válido.
  - Un trace id de puros ceros también es `null`, porque W3C lo declara inválido.
  - Solo se comparan caracteres; no hace falta una expresión regular.
- **Usuario:** `ICurrentUser.UsuarioId` y `Enum.TryParse<Rol>(currentUser.Rol, …)`, igual que `CapturaAuditoria`. No se consulta la BD (supuesto 3).
- **Trace id:** `Activity.Current?.TraceId.ToHexString()` o, sin `Activity`, `ActivityTraceId.CreateRandom().ToHexString()`. Es el mismo cálculo que `CapturaAuditoria`. Se copia en lugar de compartirlo, porque son dos líneas y moverlo tocaría la auditoría.
- **`RegistroErroresEnBd`** (scoped):
  - Calcula el trace id **antes** del `try`, porque lo necesita el `LogWarning`. La construcción del `RegistroError` y el guardado van **dentro** del `try`, para que también se capture un fallo al construirlo. Es un ajuste menor al ejemplo de código de la spec, que construye el registro fuera del `try`.
  - El scope nuevo sale de `IServiceScopeFactory.CreateAsyncScope()`. Su `AppDbContext` trae su propia `CapturaAuditoria`, que no encuentra entidades de `EntidadBase` y guarda directo, sin transacción.
  - El timeout es un `CancellationTokenSource` de 5 segundos.
  - Log con `[LoggerMessage(Level = Warning)]`: "No se pudo registrar el error {TraceId}".
- **`GlobalExceptionHandler`:**
  - Al inicio: si `exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested`, devuelve `false` para que el middleware siga con su comportamiento por defecto, sin registrar (supuesto 5).
  - En T3 se comprueba si alguna vez llega ahí. Si el middleware ya lo filtra, la comprobación queda como defensa.
  - En la rama `500`, después del `LogError`: `await httpContext.RequestServices.GetRequiredService<IRegistroErrores>().RegistrarAsync(new ErrorInesperado(OrigenError.Peticion, $"Error no controlado en {metodo} {ruta}", exception, metodo, ruta))`, con `Request.Method` y `Request.Path`.
  - La firma del handler no cambia. Sigue registrado como singleton, por eso usa `RequestServices` (spec, punto 3).
- **`EnvioCorreos`:**
  - Recibe `IRegistroErrores` por constructor.
  - En el `catch`, después de `LogFalloEnvio`: `await registroErrores.RegistrarAsync(new ErrorInesperado(OrigenError.Correo, $"No se pudo enviar el correo de {tipo} a {destinatario}", e))`.
  - El `catch` sigue devolviendo `false`.
- **Configuración EF de `RegistroError`:**
  - Tabla `registro_error` e `Id` `long` con identidad.
  - Las longitudes del dominio; `Detalle` como `text`.
  - Los enums como string, por la convención de `AppDbContext`.
  - Índices `(fecha)` y `(trace_id)`, no únicos (supuesto 8). Sin relaciones.
- **Solo lectura en Application:**
  - `IAppDbContext.RegistrosError` es `IQueryable<RegistroError>`, implementado de forma explícita con `Set<RegistroError>().AsNoTracking()`, como `RegistrosAuditoria`.
  - No hay `DbSet` público.
- **`ErroresOptions`:**
  - Sección `Errores`, `DiasRetencion` (`int`, 90 por defecto en la clase y en `appsettings.json`).
  - `.Validate(o => o.DiasRetencion >= 1, "La opción 'Errores:DiasRetencion' debe ser 1 o mayor.").ValidateOnStart()`, como `InstitutoOptions`.
- **`DepuracionErrores`:**
  - `BackgroundService` que recibe `IServiceScopeFactory`, `TimeProvider`, `IOptions<ErroresOptions>` y `ILogger`.
  - `ExecuteAsync`:
    1. Depura una vez.
    2. Luego repite en `while (await timer.WaitForNextTickAsync(stoppingToken))`, con `new PeriodicTimer(TimeSpan.FromHours(24), timeProvider)`.
  - Cada depuración abre un scope y ejecuta `db.Set<RegistroError>().Where(e => e.Fecha < limite).ExecuteDeleteAsync(stoppingToken)`, con `limite = ReglasErrores.LimiteRetencion(timeProvider.GetUtcNow(), dias)`. Escribe en el log cuántos eliminó.
  - Envuelve cada depuración en un `try`:
    - Captura cualquier excepción, salvo la cancelación con `stoppingToken` activo, y la escribe con `LogError`.
    - Así ni una BD caída ni una tabla inexistente detienen la API, y el siguiente tick lo vuelve a intentar.
  - Se registra con `AddHostedService<DepuracionErrores>()`.
  - El seed del administrador corre antes (`Program.cs`, antes de `RunAsync`), así que la depuración no compite con él.
- **Consulta:**
  - `ErroresService.ListarAsync`:
    - Filtra por rango de fechas (`IFechaLocal.RangoUtc`), `Origen`, `TraceId` (normalizado) y `Search`.
    - `Search` busca con `ToLower().Contains()` en `Mensaje`, `TipoExcepcion` y `Ruta`, esta última con chequeo de `null`.
    - Ordena por `Fecha` e `Id` descendentes, proyecta sin `Detalle` y pagina con `ToPagedResultAsync`.
  - `ErroresService.ObtenerAsync(long id)`: proyecta el detalle con `FirstOrDefaultAsync` o lanza `NotFoundException`.
  - `ListarErroresRequestValidator`:
    - `Hasta ≥ Desde` si vienen las dos.
    - `TraceId` válido según `NormalizarTraceId` si viene. El mensaje explica los dos formatos.
- **Controller:** `ErroresController : ApiControllerBase` con `[Authorize(Policy = Politicas.Administrador)]`, `[HttpGet]` y `[HttpGet("{id:long}")]`, con `ProducesResponseType` y la documentación `///`. La ruta queda en `/api/v1/errores` por la convención de nombres.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| El registro vuelve a guardar los cambios que fallaron | Scope y `AppDbContext` nuevos. En T3 se provoca un `500` durante una creación y se verifica que la entidad no aparece en la BD |
| El registro lanza una excepción y cambia la respuesta, o tumba la petición | Todo el método en un `try`. En T3 se renombra `registro_error` y se verifica que la respuesta sigue siendo el mismo `500` y que el log tiene el `LogError` y el `LogWarning` |
| Un fallo de correo pasa a devolver un error al usuario | El `catch` sigue devolviendo `false`. En T3 se verifica con smtp4dev detenido que la operación responde igual que hoy |
| Datos sensibles en la tabla | No se lee el body, los headers ni `Request.QueryString`. En T3 se usa un query string distintivo y una contraseña distintiva, y se buscan con `psql` |
| La auditoría registra los errores o rompe el guardado aparte | `RegistroError` no hereda de `EntidadBase`. En T3 se verifica que `registro_auditoria` no gana filas al registrar un error |
| Una excepción en la depuración detiene la API | `try` por ejecución. En T4 se arranca la API con `registro_error` renombrada y se verifica que sigue respondiendo |
| `ExecuteDeleteAsync` borra de más | El límite sale de una regla pura con pruebas. En T4 se verifican un registro de 91 días (se borra) y uno de 89 (se queda) |
| El `PeriodicTimer` no se puede probar en 24 horas | La primera depuración corre al arrancar, que es lo que se verifica. El intervalo es una constante del framework |
| La migración cambia otras tablas | Se revisa el script en T2: solo `registro_error` y sus índices |
| El `traceId` de los `ProblemDetails` no coincide con el guardado | Los dos salen de `Activity.Current`. En T3 se compara el `traceId` de la respuesta con el de la fila |

## Puntos de verificación

- **Tras 1:** reglas de errores y entidad en verde.
- **Tras 2:** la migración crea solo `registro_error`, sin FK y con sus dos índices. Ninguna operación existente cambia, y la tabla sigue vacía.
- **Tras 3:** un `500` y un correo fallido dejan su registro con el trace id de la respuesta, los `4xx` no dejan ninguno, y un registro que falla no cambia la respuesta.
- **Tras 4:** la depuración elimina solo lo vencido y no detiene la API cuando falla.
- **Tras 5:** los filtros se combinan, el detalle incluye el stack trace y el acceso es solo del administrador.
- **Final:** toda la tabla de verificación manual de la spec, los criterios de éxito cumplidos, `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Lo que la spec deja fuera de alcance: estado y seguimiento, agrupación, errores de arranque y de la propia depuración, `4xx`, cambios en el frontend y servicios externos de monitoreo. Las pruebas de integración siguen diferidas.
