# Tareas: errores

Módulo `errores` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-errores.md`](../../specs/SPEC-errores.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-13)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones nuevas se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio del registro de errores | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia y migración `AgregaRegistroError` | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Registro desde el manejador global y el envío de correos | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Depuración de los errores antiguos | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. Consulta `GET /api/v1/errores` y detalle | Completada | ✅ | ➖ | ✅ | ✅ |
| T6. Cierre del módulo | Completada | ✅ | ➖ | ✅ | ✅ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Verificaciones heredadas

Ninguna.

## Detalle

### T1. Dominio del registro de errores
- Acceptance:
  - Enum `OrigenError` (`Peticion`, `Correo`).
  - `ReglasErrores`:
    - `Truncar(string? texto, int maximo)`:
      - devuelve el texto si cabe;
      - si no, lo corta en `maximo - 1` y agrega `"…"`;
      - `null` → `string.Empty`;
      - `maximo < 1` lanza `ArgumentOutOfRangeException`.
    - `NormalizarTraceId(string? valor)`:
      - acepta 32 caracteres hexadecimales o el W3C `00-<32 hex>-<16 hex>-<2 hex>`;
      - recorta los espacios y devuelve minúsculas;
      - devuelve `null` si el formato no es válido o el trace id es de puros ceros.
    - `LimiteRetencion(DateTimeOffset ahora, int diasRetencion)`: `ahora.AddDays(-diasRetencion)`. Lanza `ArgumentOutOfRangeException` si `diasRetencion < 1`.
  - Entidad `RegistroError`:
    - No hereda de `EntidadBase`.
    - Propiedades: `Id` (`long`), `Fecha`, `Origen`, `Descripcion`, `TipoExcepcion`, `Mensaje`, `Detalle`, `Metodo`, `Ruta`, `UsuarioId`, `UsuarioRol` (`Rol?`) y `TraceId`.
    - Constantes públicas con las longitudes máximas del plan.
    - Constructor completo, sin setters públicos, más un constructor privado para EF:
      - Recibe la `Exception` y obtiene de ella `TipoExcepcion` (`GetType().FullName`), `Mensaje` y `Detalle` (`ToString()`).
      - Trunca todos los textos.
      - Exige una `Descripcion` y un `TraceId` no vacíos y una excepción no nula.
- Verify: `dotnet test tests/Matricula.UnitTests`, con estas pruebas:
  - `ReglasErroresTests`:
    - `Truncar`:
      - un texto que cabe, otro exacto al máximo y otro que lo supera, que mide exactamente el máximo y termina en `"…"`;
      - `null` devuelve vacío y un máximo de 0 lanza la excepción.
    - `NormalizarTraceId`:
      - acepta el formato W3C en mayúsculas y los 32 caracteres con espacios;
      - rechaza vacío, 31 y 33 caracteres, un carácter no hexadecimal, un W3C con segmentos de largo incorrecto y un trace id de ceros.
    - `LimiteRetencion`: 90 días exactos, 1 día, y `0` y `-1` lanzan la excepción.
  - `RegistroErrorTests`:
    - Toma el nombre completo del tipo (`System.InvalidOperationException`).
    - `Detalle` incluye la excepción interna.
    - Trunca un mensaje y un detalle largos a sus máximos.
    - Una descripción vacía o un trace id vacío lanzan la excepción.
  - Resultado (2026-11-13): `dotnet build` sin warnings; 332/332 pruebas unitarias en verde (39 nuevas: 29 de `ReglasErrores` y 10 de `RegistroError`). `RegistroError` todavía no está en el modelo de EF: la registra T2 con su configuración.
  - Detalles que la tarea no fijaba:
    - `Metodo` y `Ruta` vacíos se guardan como `null`, igual que si no vinieran.
    - `TraceId` también se recorta a 32 caracteres, aunque quien lo calcula siempre entrega 32.
    - Una prueba fija la frontera con una fecha concreta: 90 días antes del 13 de noviembre de 2026 es el 15 de agosto.
    - `RegistroErrorTests` también verifica que `Detalle` incluye el stack trace de una excepción lanzada y que el origen `Correo` funciona sin método, ruta ni usuario.
- Files: `src/Matricula.Domain/Enums/OrigenError.cs`, `src/Matricula.Domain/Entities/RegistroError.cs`, `src/Matricula.Domain/Reglas/ReglasErrores.cs`, `tests/Matricula.UnitTests/Domain/{ReglasErroresTests,RegistroErrorTests}.cs`

### T2. Persistencia y migración `AgregaRegistroError`
- Acceptance:
  - `RegistroErrorConfiguration`:
    - Tabla `registro_error` e `Id` con identidad.
    - Las longitudes de las constantes de `RegistroError`; `Detalle` como `text`.
    - Índices `(fecha)` y `(trace_id)`, no únicos. Sin relaciones.
  - `IAppDbContext.RegistrosError` como `IQueryable<RegistroError>`, implementado de forma explícita en `AppDbContext` con `AsNoTracking`. No hay `DbSet` público.
  - Migración `AgregaRegistroError`.
- Verify:
  - `dotnet ef database update` aplica la migración.
  - El script (`dotnet ef migrations script`) crea solo `registro_error`, con `detalle text`, `origen` y `usuario_rol` como texto, los dos índices y ninguna FK.
  - `dotnet ef migrations has-pending-model-changes` → sin cambios pendientes.
  - Regresión: login, crear un curso y consultar la auditoría siguen funcionando, y `registro_error` sigue vacía.
  - Resultado (2026-11-13):
    - `AgregaRegistroError` (`20261113165952`) aplicada a la BD de desarrollo, sin cambios de modelo pendientes.
    - El script crea solo `registro_error`:
      - `id bigint GENERATED BY DEFAULT AS IDENTITY` y `detalle text`;
      - `origen` y `usuario_rol` como `varchar(30)`;
      - las longitudes del dominio;
      - los índices `ix_registro_error_fecha` e `ix_registro_error_trace_id`.
    - En `pg_constraint` solo están la PK y los `NOT NULL`: ninguna FK.
    - Regresión contra la API local, con la cuenta de prueba `humo.adm.742345@correo.local` (usuario 29):
      - olvido de contraseña, login con la temporal, cambio obligatorio y login con la definitiva → `200`;
      - crear el curso 131 (`ERR-T2-31230`) → `201`, con su `Creacion` en la auditoría;
      - `GET /api/v1/auditoria?entidad=Curso` → `200`;
      - `registro_error` con 0 filas.
    - `dotnet build` sin warnings; 332/332 pruebas unitarias en verde.
  - Detalles que la tarea no fijaba:
    - La contraseña de PostgreSQL está en los user-secrets de `Matricula.Api`, no en `appsettings.Development.json`. Para `psql` se toma de ahí sin mostrarla.
    - Python no está instalado en la máquina: las ediciones de documentos se hacen con las herramientas de edición, no con scripts.
- Files: `src/Matricula.Infrastructure/Persistence/Configurations/RegistroErrorConfiguration.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración

### T3. Registro desde el manejador global y el envío de correos
- Acceptance:
  - `IRegistroErrores.RegistrarAsync(ErrorInesperado error)`, que nunca lanza excepciones. `ErrorInesperado` es un record con `Origen`, `Descripcion`, `Excepcion`, `Metodo?` y `Ruta?`.
  - `RegistroErroresEnBd` (scoped):
    - Usuario de `ICurrentUser`, fecha de `TimeProvider` y trace id de `Activity.Current` o uno aleatorio.
    - Guarda con el `AppDbContext` de un scope nuevo y un timeout de 5 segundos.
    - Ante cualquier excepción, `LogWarning` con el trace id y sin relanzar.
  - `GlobalExceptionHandler`:
    - Si recibe una `OperationCanceledException` con `RequestAborted` activo, devuelve `false` sin registrar.
    - En la rama `500`, después del `LogError`, registra `Peticion` con `Request.Method` y `Request.Path`, sin query string. Obtiene `IRegistroErrores` de `RequestServices`.
    - La respuesta no cambia.
  - `EnvioCorreos`: en el `catch`, después de `LogFalloEnvio`, registra `Correo` con `No se pudo enviar el correo de {tipo} a {destinatario}`, y sigue devolviendo `false`.
  - `IRegistroErrores` registrado en `AddInfrastructure`.
- Verify: manual, con la API, smtp4dev y `psql` (`select id, fecha, origen, descripcion, tipo_excepcion, mensaje, metodo, ruta, usuario_id, usuario_rol, trace_id from registro_error order by id desc`):
  - `500` en una lectura:
    - En `psql`, `ALTER TABLE aula RENAME COLUMN nombre TO nombre_x;`.
    - `GET /api/v1/aulas?search=QS-DISTINTIVO` como admin.
    - Respuesta `500` genérica, igual que antes del módulo.
    - Un registro `Peticion` con `GET`, `/api/v1/aulas`, `Npgsql.PostgresException`, el admin como usuario y el trace id contenido en el `traceId` de la respuesta.
    - `QS-DISTINTIVO` no aparece en ninguna columna.
    - Revertir el `ALTER`.
  - `500` en una escritura: con la columna renombrada, `POST /api/v1/aulas`. Hay registro, pero ni el aula ni registros nuevos en `registro_auditoria`.
  - Fallo de correo:
    - Detener smtp4dev y reenviar una temporal.
    - La operación responde igual que hoy y deja un registro `Correo` con la descripción del envío, el admin como usuario y un trace id.
    - `GET /api/v1/auditoria?operacionId=<trace id>` muestra el cambio de la temporal de la misma operación.
  - Registro público con smtp4dev detenido: registro `Correo` sin usuario.
  - Sin registros nuevos en estos casos:
    - login con una contraseña incorrecta (usar una contraseña distintiva y buscarla en `registro_error`: no aparece);
    - un `409`;
    - un `400`;
    - sin token (`401`).
  - Registro que falla:
    - `ALTER TABLE registro_error RENAME TO registro_error_x;` y provocar el `500` de nuevo.
    - La respuesta es la misma. El log tiene el `LogError` original y el `LogWarning` del registro fallido.
    - Revertir el `ALTER`.
  - Cancelación: abortar una petición lenta del cliente (por ejemplo, `curl --max-time` muy corto sobre un listado grande) y anotar si llega al handler. Si no llega, queda anotado que la comprobación es solo defensiva.
  - `registro_auditoria` no tiene filas de `RegistroError`.
  - Resultado (2026-11-13): `dotnet build` sin warnings; 332/332 pruebas unitarias en verde. Verificación manual contra la API local, con la cuenta de prueba `humo.adm.742345@correo.local` (usuario 29):

    | Escenario | Resultado |
    |---|---|
    | `500` en una lectura: `aula.ubicacion` renombrada y `GET /api/v1/aulas?search=QS-DISTINTIVO` | `500` genérico, igual que antes. Registro 1: `Peticion`, `Error no controlado en GET /api/v1/aulas`, `Npgsql.PostgresException`, `42703: no existe la columna a.ubicacion`, `GET`, `/api/v1/aulas`, usuario 29 `Administrador` y trace id `55e6e3b6…`, el mismo del `traceId` de la respuesta. `detalle` de 5623 caracteres. `QS-DISTINTIVO` no aparece en la tabla |
    | `500` en una escritura: `POST /api/v1/aulas` con la columna renombrada | `500`. Registro 2 con `DbUpdateException`. Ni el aula `ERR-T3-W` ni filas nuevas en `registro_auditoria` (61 antes y después): el registro usó su propio contexto y no reintentó el insert |
    | smtp4dev detenido, admin reenvía la temporal del usuario 289 | `200 {"correoEnviado":false}`, igual que antes. Registro 3: `Correo`, `No se pudo enviar el correo de temporal a aud.t3.a.32291@correo.local`, `SocketException`, usuario 29, sin método ni ruta. `GET /auditoria?operacionId=<trace id>` devuelve la `Modificacion` de `Usuario` 289 de la misma operación |
    | smtp4dev detenido, olvido de contraseña del usuario 290, sin sesión | `200` con el mensaje de RN-28. Registro 4: `Correo` sin usuario; su trace id lleva a la `Modificacion` de `Usuario` 290 con autor `null` |
    | Login con la contraseña `ClaveDistintivaZ9q` (`401`), curso duplicado (`409`), curso inválido (`400`), curso inexistente (`404`), sin token (`401`) | Ningún registro nuevo (4 antes y después) |
    | Buscar en `registro_error` la contraseña distintiva, `Bearer` y el prefijo de los hashes (`AQAAAA`) | 0 filas |
    | `registro_error` renombrada y el mismo `500` | La respuesta es el mismo `500`, y `/health` → `200`. El log tiene el `LogError` "Error no controlado procesando /api/v1/aulas" y el `LogWarning` "No se pudo registrar el error c0894250…" con la excepción `42P01` |
    | Cancelación: `aula` bloqueada 8 s en `psql` y `curl --max-time 2` | Ningún registro. Con `Microsoft.AspNetCore.Diagnostics` en `Debug`, el log muestra `ExceptionHandlerMiddleware[4] The request was aborted by the client.`: el middleware ya filtra la cancelación, y la comprobación del handler queda solo como defensa (supuesto 5) |
    | `registro_auditoria` | 0 filas de `RegistroError` |

    Al terminar, el esquema quedó restaurado (`aula.ubicacion` y `registro_error`).
  - Detalles que la tarea no fijaba:
    - `aula` no tiene columna `nombre`: para el `500` se renombró `ubicacion`.
    - El parámetro de `IRegistroErrores.RegistrarAsync` se llama `errorInesperado`: el analizador (CA1716) rechaza `error` porque es palabra reservada en VB. El `ArgumentNullException.ThrowIfNull` va dentro del `try`, así ni un argumento nulo afecta la respuesta.
    - **El registro público no envía correos**, así que la fila de la spec "Registro público con smtp4dev detenido" no se puede reproducir. Se reemplazó por el olvido de contraseña, que también ocurre sin sesión. Se corrigió la tabla de verificación de la spec.
    - Desde Git Bash, `dotnet run` se quedó en "Compilando…" sin escribir nada, quizás por los nodos de MSBuild que deja abiertos el C# Dev Kit de VS Code. Para verificar se ejecuta directamente `bin/Debug/net10.0/Matricula.Api.exe` con `ASPNETCORE_ENVIRONMENT=Development` y `ASPNETCORE_URLS=http://localhost:5022`, después de `dotnet build`.
- Files: `src/Matricula.Application/Abstractions/IRegistroErrores.cs`, `src/Matricula.Application/Errores/ErrorInesperado.cs`, `src/Matricula.Application/Correos/EnvioCorreos.cs`, `src/Matricula.Infrastructure/Persistence/Errores/RegistroErroresEnBd.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`, `src/Matricula.Api/Common/GlobalExceptionHandler.cs`

### T4. Depuración de los errores antiguos
- Acceptance:
  - `ErroresOptions` (sección `Errores`, `DiasRetencion` = 90), validada al arrancar (`≥ 1`).
  - `"Errores": { "DiasRetencion": 90 }` en `appsettings.json`.
  - `DepuracionErrores : BackgroundService`:
    - Depura al arrancar y luego cada 24 horas, con `PeriodicTimer` sobre `TimeProvider`.
    - Usa `ExecuteDeleteAsync` sobre `Fecha < ReglasErrores.LimiteRetencion(ahora, dias)` en un scope propio, y escribe en el log cuántos eliminó.
    - Captura cualquier excepción salvo la cancelación al detenerse, y la escribe con `LogError`.
  - Registrada con `AddHostedService`.
- Verify: manual, con los registros que dejó T3:
  - En `psql`, `UPDATE registro_error SET fecha = now() - interval '91 days'` en un registro y `interval '89 days'` en otro. Reiniciar la API: se elimina solo el de 91 días, y el log indica 1 eliminado.
  - `Errores__DiasRetencion=0` como variable de entorno: la API no arranca y muestra el mensaje de la opción.
  - Con `registro_error` renombrada en `psql`, arrancar la API: el log muestra el `LogError` de la depuración y la API sigue respondiendo (`GET /health` → `200`). Revertir el `ALTER`.
  - `registro_auditoria` no gana filas por la depuración.
  - Resultado (2026-11-13): `dotnet build` sin warnings; 332/332 pruebas unitarias en verde. Verificación manual contra la BD de desarrollo:

    | Escenario | Resultado |
    |---|---|
    | Registro 1 con `fecha` de hace 91 días y registro 2 de hace 89, y arrancar la API | Al arrancar, el log muestra "Depuración de errores: 1 registros anteriores a 08/15/2026 17:24:11 +00:00 eliminados". Queda el registro 2 (89 días), y también el 3 y el 4 |
    | `Errores__DiasRetencion=0` | La API no arranca: `OptionsValidationException: La opción 'Errores:DiasRetencion' debe ser 1 o mayor.` |
    | `registro_error` renombrada y arrancar la API | El log muestra el `LogError` "No se pudo depurar los registros de error" con el `42P01`. La API sigue respondiendo: `/health` y `GET /api/v1/cursos` → `200`. Después se revirtió el `ALTER` |
    | `registro_auditoria` | 63 filas antes y después: la depuración no genera auditoría |

  - Detalles que la tarea no fijaba:
    - **Paquete nuevo, aprobado por el usuario:** `BackgroundService` está en `Microsoft.Extensions.Hosting.Abstractions`, que Infrastructure no tenía, porque solo la API lo recibe con ASP.NET Core. Se agregó en la versión 10.0.12, la misma de los demás paquetes de Microsoft del proyecto, en lugar de mover la depuración a Api. Se corrigieron el Tech Stack de la spec y la decisión 12 del mapa.
    - La cancelación al detener la API se ignora en dos lugares: dentro de cada depuración y alrededor del `PeriodicTimer`.
    - `TaskStop` cierra la terminal pero no el `.exe` de la API, que sigue bloqueando los binarios en el siguiente `dotnet build`. Después de cada verificación hay que detenerlo también con `Stop-Process -Name Matricula.Api`.
- Files: `src/Matricula.Infrastructure/Persistence/Errores/{ErroresOptions,DepuracionErrores}.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`, `src/Matricula.Infrastructure/Matricula.Infrastructure.csproj`, `src/Matricula.Api/appsettings.json`

### T5. Consulta `GET /api/v1/errores` y detalle
- Acceptance:
  - `ListarErroresRequest : PaginacionRequest` con `Desde`, `Hasta`, `Origen` y `TraceId`. `Search` busca en `Mensaje`, `TipoExcepcion` y `Ruta` (supuesto 7).
  - `ListarErroresRequestValidator`: `Desde ≤ Hasta`, y `TraceId` válido según `NormalizarTraceId`, con un mensaje que explique los dos formatos.
  - `ErroresService`:
    - `ListarAsync`: filtros combinados, orden por `Fecha` e `Id` descendentes, proyección sin `Detalle` y paginación.
    - `ObtenerAsync(id)`: el detalle o `NotFoundException`.
  - `RegistroErrorResponse` y `RegistroErrorDetalleResponse` como en el punto 7 de la spec.
  - `ErroresController` con `Politicas.Administrador`, `[HttpGet]` y `[HttpGet("{id:long}")]`, y la documentación `///`.
  - `ErroresService` registrado en `AddApplication`. Peticiones de ejemplo en `Matricula.Api.http`.
- Verify: manual, con los registros de T3:
  - Sin filtros, con `pageSize` pequeño: del más reciente al más antiguo, `totalCount` correcto y sin `detalle`.
  - `traceId` en formato W3C (copiado de un `ProblemDetails`), en 32 caracteres y en mayúsculas: el mismo registro.
  - `origen=Correo`, `desde`/`hasta` y `search=PostgresException` combinados: filtra y pagina.
  - `GET /errores/{id}`: incluye `detalle` con el stack trace. Un id inexistente → `404`.
  - `traceId=abc`, `desde > hasta` y `origen=Otro` → `400` con el campo.
  - Sin token → `401`. Estudiante o profesor → `403`. Con un cambio de contraseña pendiente → `403` (RN-24).
  - Consultar los errores no genera registros en `registro_error` ni en `registro_auditoria`.
  - Resultado (2026-11-13): `dotnet build` sin warnings; 332/332 pruebas unitarias en verde. Verificación manual contra la API local, sobre 4 registros: 2 y 5 de origen `Peticion` (el 2, de hace 89 días, de T4; el 5, un `500` nuevo con `aula.ubicacion` renombrada), y 3 y 4 de origen `Correo` (de T3):

    | Escenario | Resultado |
    |---|---|
    | `pageSize=2`, páginas 1 y 2 | `totalCount=4`; ids 5, 4 y 3, 2, del más reciente al más antiguo |
    | Campos del listado | `id`, `fecha`, `origen`, `descripcion`, `tipoExcepcion`, `mensaje`, `metodo`, `ruta`, `usuarioId`, `usuarioRol`, `traceId`. Sin `detalle` |
    | `traceId` W3C de la respuesta `500`, sus 32 caracteres y en mayúsculas | Los tres devuelven solo el registro 5 |
    | `origen=Correo&desde=2026-11-13&hasta=2026-11-13&search=socket` | 4 y 3 |
    | `search=postgresexception` / `search=/AULAS` | 5 / 5 y 2. El 2 no aparece en el primero: su tipo es `DbUpdateException`, la `PostgresException` está solo en el detalle, y `search` no busca ahí (supuesto 7) |
    | `origen=Peticion&hasta=2026-09-01` / `desde=2026-11-14` | 2 / 0 registros |
    | `GET /errores/5` | `200` con `detalle`: `Npgsql.PostgresException … 42703` y el stack trace hasta `AulasController.Listar` |
    | `GET /errores/999999` / `GET /errores/1` (depurado en T4) | `404` "No existe el error …" |
    | `traceId=abc&desde=2026-11-14&hasta=2026-11-13` · `origen=Otro` | `400` con `TraceId` y `Hasta` · `400` con `Origen` |
    | Sin token / estudiante (289) / profesor (46) / profesor con la temporal de un olvido | `401` / `403` / `403` / `403` con `code: RN-24` |
    | 5 consultas seguidas como admin | `registro_auditoria` 68 → 68 y `registro_error` 4 → 4 |
    | OpenAPI | `/api/v1/errores` y `/api/v1/errores/{id}` |

  - Detalles que la tarea no fijaba:
    - Para los tokens se definieron contraseñas de prueba por olvido y cambio obligatorio en el estudiante `aud.t3.a.32291@correo.local` (289) y el profesor `t10p.955591@correo.local` (46). Esos cambios quedaron en la auditoría, como se espera.
    - `ObtenerAsync` proyecta directo al DTO con `FirstOrDefaultAsync`, sin cargar la entidad.
- Files: `src/Matricula.Application/Errores/{ErroresService,ListarErroresRequest,ListarErroresRequestValidator,RegistroErrorResponse,RegistroErrorDetalleResponse}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/ErroresController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T6. Cierre del módulo
- Acceptance:
  - `AGENTS.md`:
    - Rangos RF-01..RF-59 y RN-01..RN-49.
    - En "Nunca":
      - la excepción acotada: `ExecuteDeleteAsync` solo en la depuración de `registro_error`;
      - guardar en el registro de errores el body, los headers o el query string;
      - registrar errores con el `AppDbContext` de la petición.
  - Toda la tabla de verificación manual de la spec recorrida. Lo que no cubran T3 a T5 se cubre aquí.
  - Criterios de éxito de la spec cumplidos.
  - Estados al día en la spec, este `todo.md` y `specs/mapa-modulos.md` (módulo **Completado**).
- Verify: `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde y la tabla de verificación completa.
  - Resultado (2026-11-13):
    - `AGENTS.md`:
      - Rangos actualizados a RF-01..RF-59 y RN-01..RN-49.
      - La regla de "Nunca" sobre `ExecuteDelete` nombra la única excepción: `ExecuteDeleteAsync` en `DepuracionErrores`, solo para `registro_error`.
      - Nuevo punto en "Nunca": guardar el body, los headers o el query string en el registro de errores; registrar con el `AppDbContext` de la petición; o dejar que una excepción del registro o de la depuración llegue al usuario o detenga la API.
    - Tabla de verificación de la spec: todas las filas quedaron cubiertas en T3 (registro), T4 (depuración) y T5 (consulta), salvo la última, que se recorrió aquí:
      - Crear el curso 132 después de todo lo anterior → `201` y su `Creacion` en la auditoría, con el admin como autor.
      - `registro_auditoria` no tiene filas de `RegistroError`. Solo tiene entidades de negocio: `Curso`, `Estudiante`, `Grupo`, `Horario`, `Matricula` y `Usuario`.
    - Control final de datos sensibles sobre las 4 filas de `registro_error`:
      - Ninguna contiene un token (`Bearer ey…`, `eyJhbGci`), el header `Authorization: `, un hash (`AQAAAA`), un query string ni las contraseñas de prueba.
      - Una búsqueda amplia de "Authorization" coincidió con 2 filas, pero eran nombres de tipos del stack trace (`AuthorizationPolicy`, `PolicyAuthorizationResult`), no el header.
      - Ninguna `ruta` tiene `?`.
    - `Include Error Detail` no aparece en la cadena de conexión de los user-secrets ni en `appsettings*.json`.
    - Criterios de éxito de la spec:
      - 1 (tabla sin FK, solo lectura): T2.
      - 2 y 4 (registro y respuesta sin cambios): T3.
      - 3 (sin `4xx` ni datos sensibles): T3 y este control.
      - 5 (depuración): T4.
      - 6 (consulta): T5.
      - 7 (verificaciones manuales): las tablas de T3 a T6.
      - 8 (build y pruebas): abajo.
    - `dotnet build` sin warnings; 332/332 pruebas unitarias en verde; `has-pending-model-changes` sin cambios pendientes.
- Files: `AGENTS.md`, `specs/SPEC-errores.md`, `specs/mapa-modulos.md`, `tasks/errores/todo.md`
