# Tareas: errores

Módulo `errores` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-errores.md`](../../specs/SPEC-errores.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-13)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones nuevas se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio del registro de errores | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia y migración `AgregaRegistroError` | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Registro desde el manejador global y el envío de correos | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T4. Depuración de los errores antiguos | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T5. Consulta `GET /api/v1/errores` y detalle | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T6. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

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
- Files: `src/Matricula.Infrastructure/Persistence/Errores/{ErroresOptions,DepuracionErrores}.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`, `src/Matricula.Api/appsettings.json`

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
- Files: `AGENTS.md`, `specs/SPEC-errores.md`, `specs/mapa-modulos.md`, `tasks/errores/todo.md`
