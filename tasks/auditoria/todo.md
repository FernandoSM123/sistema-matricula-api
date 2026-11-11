# Tareas: auditoria

Módulo `auditoria` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-auditoria.md`](../../specs/SPEC-auditoria.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-10)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones nuevas se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio de la auditoría | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia, migración `AgregaRegistroAuditoria` y `FechasInterceptor` | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Captura en `SaveChangesAsync` | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Consulta `GET /api/v1/auditoria` | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

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

### T1. Dominio de la auditoría
- Acceptance:
  - Enum `AccionAuditoria` (`Creacion`, `Modificacion`, `Eliminacion`).
  - Entidad `RegistroAuditoria`:
    - No hereda de `EntidadBase`.
    - Propiedades: `Id` (`long`), `Fecha`, `Accion`, `Entidad`, `EntidadId`, `Cambios` (JSON en texto), `UsuarioId`, `UsuarioCorreo`, `UsuarioRol` (`Rol?`) y `OperacionId`.
    - Constructor completo, sin setters públicos, más un constructor privado para EF.
  - `ReglasAuditoria`:
    - `record PropiedadAuditada(string Nombre, object? Antes, object? Despues)`.
    - `Valores(entidad, propiedades)` para una creación o eliminación y `Diferencias(entidad, propiedades)` para una modificación. Las dos devuelven el JSON, o `null` si no queda nada auditable.
    - Excluye `Id`, `FechaCreacion` y `FechaModificacion` en todas las entidades; `UltimoIngreso` y `SecurityStamp` en `Usuario`; `Version` en `Grupo` (RN-46).
    - Enmascara `PasswordHash` y `TempPasswordHash` de `Usuario` como `"***"` si tienen valor, o `null` si no (RN-44).
    - En `Diferencias`, solo las propiedades con un valor distinto en `Antes` y `Despues`.
    - Claves en camelCase y enums como string; `DateOnly`, `TimeOnly` y `DateTimeOffset` en ISO 8601.
    - `EntidadesAuditadas`: los nombres de las clases no abstractas del dominio que heredan de `EntidadBase` (supuesto 7).
  - `FechaInstituto.RangoUtc(DateOnly? desde, DateOnly? hasta, TimeZoneInfo zona)` → `(DateTimeOffset? Inicio, DateTimeOffset? Fin)`, con el fin exclusivo en `hasta + 1 día 00:00` local (supuesto 8).
- Verify: `dotnet test tests/Matricula.UnitTests`, con estas pruebas:
  - `ReglasAuditoriaTests`:
    - Cada exclusión.
    - El enmascarado con valor y sin valor, en las tres acciones.
    - Una modificación de solo `UltimoIngreso` → `null`. Una de solo `SecurityStamp` → `null`.
    - Una asignación al mismo valor no aparece.
    - Formato `{ "estado": { "antes": "Activa", "despues": "Retirada" } }`, con camelCase y enums como string.
    - Fechas en ISO 8601.
    - `EntidadesAuditadas` contiene exactamente las 10 entidades.
  - `FechaInstitutoTests` (ampliado):
    - El rango de un día en `America/Costa_Rica` va de 06:00 UTC a 06:00 UTC del día siguiente.
    - Un instante a las 23:30 locales (05:30 UTC del día siguiente) queda dentro.
    - Con solo `desde` o solo `hasta`, el otro límite es `null`.
  - Resultado (2026-11-11): `dotnet build` sin warnings; 293/293 pruebas unitarias en verde (40 nuevas: 32 de `ReglasAuditoria` y 8 de `FechaInstituto.RangoUtc`, incluidas las fronteras de la medianoche local). `RegistroAuditoria` todavía no está en el modelo de EF: la registra T2 con su configuración.
  - Detalles que la tarea no fijaba:
    - `Valores` recibe pares `(nombre, valor)` (`KeyValuePair<string, object?>`) en lugar de `PropiedadAuditada`: una creación o eliminación tiene un solo valor por propiedad, y así no hay que saber si va en `Antes` o en `Despues`. `PropiedadAuditada` queda solo para `Diferencias`, en su propio archivo (`Reglas/PropiedadAuditada.cs`).
    - `Valores` nunca devuelve `null`: una creación o eliminación siempre se registra, aunque quede `{}`. Solo `Diferencias` devuelve `null` (RN-46).
    - Se agregó `ReglasAuditoria.BuscarEntidad(nombre)`, que devuelve el nombre exacto de una entidad auditada sin distinguir mayúsculas, o `null`. La usarán el validador y el servicio de T4 (supuesto 7).
    - También se exponen `Excluida(entidad, propiedad)` y `Enmascarada(entidad, propiedad)`, que T3 puede usar para no leer propiedades que no se guardan.
    - Las diferencias comparan el valor real antes de enmascarar: un hash que cambia aparece como `"***" → "***"`, y uno que no cambia no aparece.
    - El JSON usa el codificador por defecto de `System.Text.Json`, que escapa los caracteres no ASCII (`á`). No importa: `jsonb` guarda el texto ya decodificado.
- Files: `src/Matricula.Domain/Enums/AccionAuditoria.cs`, `src/Matricula.Domain/Entities/RegistroAuditoria.cs`, `src/Matricula.Domain/Reglas/{ReglasAuditoria,PropiedadAuditada,FechaInstituto}.cs`, `tests/Matricula.UnitTests/Domain/{ReglasAuditoriaTests,FechaInstitutoTests}.cs`

### T2. Persistencia, migración `AgregaRegistroAuditoria` y `FechasInterceptor`
- Acceptance:
  - `RegistroAuditoriaConfiguration`:
    - Tabla `registro_auditoria` e `Id` con identidad.
    - `Cambios` con `HasColumnType("jsonb")`.
    - Longitudes: `Entidad` 50, `UsuarioCorreo` la de `Usuario.Correo`, `OperacionId` 32.
    - Índices `(entidad, entidad_id)`, `(usuario_id)` y `(fecha)`. Sin relaciones.
  - `IAppDbContext.RegistrosAuditoria` como `IQueryable<RegistroAuditoria>`, implementado de forma explícita en `AppDbContext` con `AsNoTracking`. No hay `DbSet` público. La entidad se registra en el modelo mediante su configuración.
  - Migración `AgregaRegistroAuditoria`.
  - `AuditoriaInterceptor` renombrado a `FechasInterceptor`, con su archivo y su registro en `DependencyInjection`, sin cambios de lógica.
- Verify:
  - `dotnet ef database update` aplica la migración. El script (`dotnet ef migrations script`) crea solo `registro_auditoria`, con `cambios jsonb`, los tres índices y ninguna FK.
  - `dotnet ef migrations has-pending-model-changes` → sin cambios pendientes.
  - Regresión: crear y editar un curso, e iniciar sesión, siguen funcionando. `fecha_creacion` y `fecha_modificacion` se siguen asignando, y `registro_auditoria` sigue vacía.
  - Resultado (2026-11-11):
    - `AgregaRegistroAuditoria` (`20261111213631`) aplicada a la BD de desarrollo, sin cambios de modelo pendientes.
    - El script crea solo `registro_auditoria`: `id bigint GENERATED BY DEFAULT AS IDENTITY`, `cambios jsonb`, `accion` y `usuario_rol` como texto, e índices `ix_registro_auditoria_entidad_entidad_id`, `ix_registro_auditoria_fecha` e `ix_registro_auditoria_usuario_id`. En `pg_constraint` solo están la PK y los `NOT NULL`: ninguna FK.
    - Regresión contra la API local: olvido de contraseña, login con la temporal, cambio obligatorio y login con la definitiva → `200`. Crear el curso 129 (`AUD-T2-9087`) → `201`, y editar su nombre → `200`. En la BD, el curso tiene `fecha_creacion` y `fecha_modificacion`, y `registro_auditoria` tiene 0 filas.
    - `dotnet build` sin warnings; 293/293 pruebas unitarias en verde.
  - Detalles que la tarea no fijaba:
    - Para verificar se usa la misma cuenta de prueba de `matriculas`, `humo.adm.742345@correo.local`, con una nueva contraseña de prueba definida por olvido y cambio obligatorio. La cuenta real `admin@instituto.local` no se tocó.
    - smtp4dev se inicia con `%USERPROFILE%\.dotnet\tools\smtp4dev.cmd`, porque desde Git Bash el comando `smtp4dev` no se encuentra. `psql` está en `C:\Program Files\PostgreSQL\18\bin`.
    - `explicaciones/entityFrameworkCore.md` y `SPEC-fundacion.md` todavía nombran `AuditoriaInterceptor`. No se cambiaron: los apuntes son del usuario y la spec de `fundacion` es historial.
- Files: `src/Matricula.Infrastructure/Persistence/Configurations/RegistroAuditoriaConfiguration.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/Interceptors/FechasInterceptor.cs` (renombrado), `src/Matricula.Infrastructure/DependencyInjection.cs`, migración

### T3. Captura en `SaveChangesAsync`
- Acceptance:
  - `CapturaAuditoria` (scoped):
    - Antes del guardado, arma los registros de modificación y eliminación y anota las entradas creadas. Después del guardado, arma los de creación con `Id` y FK definitivos.
    - Todos los registros de un guardado comparten la misma `Fecha` (`TimeProvider`), el mismo autor y el mismo `OperacionId`.
    - Autor: `ICurrentUser.UsuarioId` y `Rol`, y el correo leído de la BD por id, solo si hay registros (supuesto 3). Sin usuario, los tres en `null`.
    - `OperacionId`: `Activity.Current?.TraceId` en hexadecimal, o uno aleatorio si no hay `Activity`.
  - `AppDbContext` recibe `CapturaAuditoria` y sobrescribe `SaveChangesAsync(bool, CancellationToken)` como en el plan:
    - Una transacción propia si no hay una en curso.
    - Dos guardados, los dos con `acceptAllChangesOnSuccess: true`.
    - Sin transacción si no hay nada que auditar.
  - `SaveChanges()` y `SaveChanges(bool)` lanzan `InvalidOperationException` (supuesto 12).
- Verify: manual, con la API y `psql` (`select accion, entidad, entidad_id, usuario_correo, operacion_id, cambios from registro_auditoria order by id desc`):
  - Crear un curso: `Creacion` con el id real y el admin como autor. Editar solo su nombre: `Modificacion` con solo `nombre`.
  - Desactivar un estudiante: `Modificacion` de `Usuario` con `activo`, sin `securityStamp`.
  - Registro público: `Creacion` de `Usuario` y de `Estudiante` con autor `null`. El `usuarioId` del estudiante en `cambios` es el id real de la cuenta, no uno negativo. `passwordHash: "***"`.
  - Matricular: `Creacion` de `Matricula` y `Modificacion` de `Grupo` (`cuposOcupados`) con el mismo `operacion_id`. Cambio de grupo: 4 registros con el mismo `operacion_id`.
  - `409 RN-05` y un conflicto de concurrencia: ningún registro nuevo.
  - Login con la contraseña definitiva y logout: ningún registro. Login con una temporal pendiente: `tempPasswordHash` de `"***"` a `null`.
  - Cambiar la propia contraseña: `passwordHash` de `"***"` a `"***"`. `select count(*) from registro_auditoria where cambios::text like '%AQAAAA%'` → 0. Es el prefijo de los hashes de `PasswordHasher`; se confirma antes con un `password_hash` real.
  - Quitar un bloque de un grupo `Planificado`: `Eliminacion` de `Horario`. Eliminar un estudiante sin matrículas: `Eliminacion` de `Estudiante` y de `Usuario`, con sus últimos valores, y la eliminación responde `204`.
  - El seed del administrador, si corre, deja autor `null`.
  - Resultado (2026-11-11): `dotnet build` sin warnings, sin cambios de modelo pendientes y 293/293 pruebas unitarias en verde. Verificación manual contra la API local, con la cuenta de prueba `humo.adm.742345@correo.local` (usuario 29) y tres estudiantes nuevos de registro público (A = 90, B = 91, C = 92):

    | Escenario | Resultado |
    |---|---|
    | Login del admin con la definitiva | 0 registros |
    | Crear el curso 130 / editar solo su nombre | `Creacion` con todos los valores y el admin como autor / `Modificacion` con solo `nombre` |
    | Registro público de A, B y C | Por cada uno, `Creacion` de `Usuario` y de `Estudiante` con autor `null` y el mismo `operacion_id`. `passwordHash: "***"`. El `usuarioId` del estudiante es el id real de la cuenta (por ejemplo, 289), no uno temporal |
    | Admin matricula a A en el grupo 123 | `Creacion` de `Matricula` 113 y `Modificacion` de `Grupo` 123 (`cuposOcupados` 0 → 1), con el mismo `operacion_id` |
    | Cambio de grupo de 123 a 124 | 4 registros con el mismo `operacion_id`: matrícula 113 retirada (estado, fecha y motivo `CambioGrupo`), los dos grupos con `cuposOcupados` y la `Creacion` de la matrícula 114 |
    | Matricular a A en el grupo 128 (mismo curso y periodo) | `409 RN-05`, 0 registros |
    | B y C en paralelo por el único cupo del grupo 128 | B `201`, C `409 RN-03`: solo los 2 registros de B |
    | 7 ediciones en paralelo del grupo 128 | 6 `200` y 1 `409 CONCURRENCIA`: 6 registros, ninguno de la que falló |
    | Olvido de contraseña de A | `Modificacion` de `Usuario` con `tempPasswordHash` `null` → `"***"`, el vencimiento y el motivo `Olvido`; autor `null` |
    | Login de A con la definitiva y la temporal pendiente | `tempPasswordHash` `"***"` → `null` y el vencimiento y el motivo a `null`; autor `null` |
    | Login normal y logout de A | 0 registros |
    | A cambia su contraseña | `passwordHash` `"***"` → `"***"` y `fechaCambioContrasena`; autor A (`Estudiante`), con su correo |
    | Hashes en `cambios` | El `password_hash` real empieza con `AQAAAA`; 0 registros lo contienen |
    | Agregar el bloque 205 al grupo 126 (`Planificado`) y quitarlo | `Creacion` y `Eliminacion` de `Horario` con día, horas y `grupoId` |
    | Desactivar a C y eliminarlo | `Modificacion` de `Usuario` (`activo` true → false, sin `securityStamp`). La eliminación responde `204` y deja la `Eliminacion` de `Estudiante` y de `Usuario` con sus últimos valores, con el mismo `operacion_id` |
    | Seed del administrador | No aplica: ya existe un administrador y el seed no cambia nada al arrancar (0 registros al iniciar la API) |

    Los 49 registros tienen un `operacion_id` de 32 caracteres. El grupo 128 volvió a su cupo de 20.
  - Detalles que la tarea no fijaba:
    - Además de `CapturaAuditoria`, la captura usa tres tipos de apoyo en `Persistence/Auditoria/`: `CambiosPendientes` (lo encontrado antes del guardado, que arma los registros con `Completar()`), `CambioCapturado` y `AutorCambio`. `CambiosPendientes` es público porque `AppDbContext` es público; sus constructores y los otros dos tipos son internos.
    - Solo se sobrescribe `SaveChanges(bool)`: `SaveChanges()` lo llama, así que los dos lanzan la excepción.
    - Las creaciones se registran después de las modificaciones y eliminaciones del mismo guardado, así que dentro de una operación sus ids de auditoría quedan al final.
    - `jsonb` reordena las claves de `cambios` (primero las más cortas). El orden no tiene significado.
    - Para forzar un conflicto real de concurrencia hicieron falta ediciones en paralelo del mismo grupo. Dos matrículas en paralelo se serializaron y la segunda falló por `RN-03`, no por `xmin`.
- Files: `src/Matricula.Infrastructure/Persistence/Auditoria/{CapturaAuditoria,CambiosPendientes,CambioCapturado,AutorCambio}.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`

### T4. Consulta `GET /api/v1/auditoria`
- Acceptance:
  - `ListarAuditoriaRequest : PaginacionRequest` con `Entidad`, `EntidadId`, `UsuarioId`, `Accion`, `Desde`, `Hasta` y `OperacionId`. `Search` busca en el correo del autor (supuesto 10).
  - `ListarAuditoriaRequestValidator`:
    - `EntidadId` y `UsuarioId` mayores que 0.
    - `EntidadId` exige `Entidad`.
    - `Entidad` debe estar en `ReglasAuditoria.EntidadesAuditadas`, sin distinguir mayúsculas. El mensaje lista las válidas.
    - `Desde ≤ Hasta`.
  - `IFechaLocal.RangoUtc(desde, hasta)`, implementado en `FechaLocal` con `FechaInstituto.RangoUtc`.
  - `AuditoriaService.ListarAsync`:
    - Aplica los filtros combinados; `Entidad` se normaliza al nombre exacto.
    - Ordena por `Fecha` descendente e `Id` descendente y pagina.
    - Convierte `Cambios` a `JsonElement` después de paginar.
  - `RegistroAuditoriaResponse` como en el punto 5 de la spec.
  - `AuditoriaController` con `Politicas.Administrador`, un `[HttpGet]` y la documentación `///`.
  - `AuditoriaService` registrado en `AddApplication`. Peticiones de ejemplo en `Matricula.Api.http`.
- Verify: manual, con los registros que dejó T3:
  - `?entidad=Matricula&entidadId=…`: solo esa matrícula, del más reciente al más antiguo.
  - `?entidad=matricula`: igual que con `Matricula`.
  - `usuarioId`, `accion`, `desde`/`hasta`, `operacionId` y `search` combinados: filtra y pagina, con `totalCount` correcto.
  - `desde` = `hasta` = hoy, con un cambio hecho después de las 18:00 de Costa Rica: el cambio aparece.
  - `cambios` sale como objeto en la respuesta y en Scalar.
  - `entidadId` sin `entidad`, `entidad=Inexistente` y `desde > hasta` → `400` con el campo.
  - Token de estudiante o profesor → `403`; sin token → `401`; con un cambio de contraseña pendiente → `403` (RN-24).
  - Resultado (2026-11-11): `dotnet build` sin warnings; 293/293 pruebas unitarias en verde. Verificación manual contra la API local, sobre los 49 registros de T3:

    | Escenario | Resultado |
    |---|---|
    | Sin filtros, `pageSize=5`, páginas 1 y 2 | `totalCount=49`; ids 49…45 y 44…40, del más reciente al más antiguo |
    | `entidad=Matricula&entidadId=113` | Sus 2 registros (modificación y creación) |
    | `entidad=matricula` | Los 4 registros de `Matricula`, igual que con `Matricula` |
    | `usuarioId=29&accion=Modificacion&desde=2026-11-11&hasta=2026-11-11&pageSize=3` | `totalCount=32`, 3 por página, solo modificaciones del admin |
    | `operacionId` del cambio de grupo, en minúsculas y en mayúsculas | Los mismos 4 registros |
    | `search=AUD.T3.A` | El cambio de contraseña del estudiante A (su correo, sin distinguir mayúsculas) |
    | `desde=2026-11-12` / `hasta=2026-11-10` | 0 registros |
    | `cambios` | Sale como objeto en la respuesta; en OpenAPI, como `JsonElement` |
    | `entidadId` sin `entidad` · `entidad=Inexistente` · `entidad=RegistroAuditoria` · `desde > hasta` · `accion=Borrado` · `usuarioId=0` y `entidadId=-1` | `400` con el campo: `EntidadId`, `Entidad` con la lista de las 10 válidas, `Hasta`, `Accion` y los dos ids |
    | Sin token / estudiante / profesor | `401` / `403` / `403` |
    | Profesor con la temporal de un olvido (cambio pendiente) | `403` (RN-24) |
    | Consultar la auditoría | No genera registros |

  - Pendiente para T5: `desde` = `hasta` = hoy con un cambio hecho después de las 18:00 de Costa Rica (00:00 UTC). La verificación se hizo antes de esa hora, así que el caso de punta a punta no se pudo reproducir. La frontera está cubierta por las pruebas unitarias de `FechaInstituto.RangoUtc` (las 23:30 locales quedan dentro del día), y el filtro usa ese mismo rango.
  - Detalles que la tarea no fijaba:
    - `search` usa `ToLower().Contains()`, igual que los demás listados, en lugar de `ILike`: Application no depende de Npgsql.
    - `operacionId` se compara en minúsculas, porque el trace id se guarda así, y el validador limita su largo a 32.
    - `RegistroAuditoria` no es un nombre válido para el filtro `entidad`: la auditoría no se audita a sí misma (supuesto 1).
    - Al verificar se cambió la contraseña de la cuenta de prueba de profesor `t10p.955591@correo.local` (usuario 46), por olvido y cambio obligatorio. Esos cambios quedaron en la auditoría: el olvido a nombre del Sistema y el cambio obligatorio a nombre del profesor (supuesto 5).
    - Para leer la temporal de smtp4dev, el HTML se aplana con `sed 's/<[^>]*>/ /g' | tr -s ' \n'` antes de buscar "temporal es:".
- Files: `src/Matricula.Application/Auditoria/{AuditoriaService,ListarAuditoriaRequest,ListarAuditoriaRequestValidator,RegistroAuditoriaResponse}.cs`, `src/Matricula.Application/Abstractions/IFechaLocal.cs`, `src/Matricula.Infrastructure/Tiempo/FechaLocal.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/AuditoriaController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T5. Cierre del módulo
- Acceptance:
  - `AGENTS.md`:
    - Rangos RF-01..RF-57 y RN-01..RN-46.
    - En "Nunca": escribir con `ExecuteUpdate`, `ExecuteDelete` o SQL manual, y registrar la auditoría a mano desde un servicio.
  - Toda la tabla de verificación manual de la spec recorrida, incluida la cancelación de un grupo con matrículas, que T3 no cubre, y el filtro de un día con un cambio hecho después de las 18:00 de Costa Rica, pendiente de T4.
  - Criterios de éxito de la spec cumplidos.
  - Estados al día en la spec, este `todo.md` y `specs/mapa-modulos.md` (módulo **Completado**).
- Verify: `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde y la tabla de verificación completa.
- Files: `AGENTS.md`, `specs/SPEC-auditoria.md`, `specs/mapa-modulos.md`, `tasks/auditoria/todo.md`
