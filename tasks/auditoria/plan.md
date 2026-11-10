# Plan: auditoria

Módulo `auditoria` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-auditoria.md`](../../specs/SPEC-auditoria.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** En revisión

## Enfoque

Igual que los módulos anteriores: primero la base sin endpoints. Las reglas puras van en el dominio, cubiertas con pruebas unitarias. Después la persistencia, verificada contra la BD. Luego la captura, que es el corazón del módulo, y por último la consulta. Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

Este módulo no agrega flujos de negocio, sino un efecto de todos los existentes. Por eso la captura (T3) se verifica recorriendo operaciones que ya existen en los otros módulos y mirando la tabla con `psql`. La consulta (T4) llega después, cuando ya hay registros reales para filtrar.

1. **Dominio:** qué se guarda y cómo (RN-44, RN-46), el formato JSON y el rango de fechas. Son las reglas con más casos y no necesitan BD.
2. **Persistencia:** la tabla, el `DbSet`, la lectura de solo lectura para Application y el renombre del interceptor. Todavía no se escribe nada en la tabla.
3. **Captura** en `SaveChangesAsync`, con la transacción, el autor y la operación (RF-56, RN-42, RN-45).
4. **Consulta** `GET /api/v1/auditoria` con sus filtros (RF-57).
5. **Cierre:** `AGENTS.md`, el recorrido completo de la tabla de verificación de la spec y la actualización de los estados.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio:** `AccionAuditoria`, `RegistroAuditoria`, `ReglasAuditoria`, rango UTC en `FechaInstituto` | — | Solo pruebas unitarias |
| 2 | **Persistencia:** configuración, `DbSet`, `IQueryable` en `IAppDbContext`, migración `AgregaRegistroAuditoria`, `FechasInterceptor` | 1 | Sin captura todavía: la tabla queda vacía |
| 3 | **Captura** en `AppDbContext.SaveChangesAsync` (`CapturaAuditoria`) | 1, 2 | Verificación con `psql` sobre operaciones de todos los módulos |
| 4 | **Consulta** `GET /api/v1/auditoria` | 2, 3 | Necesita registros reales para verificarse |
| 5 | **Cierre del módulo** | todos | `AGENTS.md`, tabla de verificación completa, estados |

**Paralelismo:** ninguno útil. 3 y 4 solo dependen de 2, pero la consulta no se puede verificar sin registros, y las dos tocan `AppDbContext`/`IAppDbContext` o los usan. Se hacen en serie.

## Decisiones de implementación

- **Dónde vive cada cosa:**
  - `Domain/Reglas/ReglasAuditoria.cs`, con lógica pura y sin EF:
    - Qué propiedades se excluyen y cuáles se enmascaran.
    - Cómo se arman los valores de una creación o una eliminación y las diferencias de una modificación.
    - La serialización a JSON.
    - La lista de entidades auditadas.
  - `Infrastructure/Persistence/Auditoria/CapturaAuditoria.cs`: lo único que conoce EF. Lee el `ChangeTracker` y convierte cada `EntityEntry` en una lista de `(nombre, antes, después)`, que pasa a `ReglasAuditoria`. Resuelve el autor y la operación.
  - `AppDbContext`: solo orquesta la transacción y los dos guardados (spec, punto 2).
- **Formato de entrada a las reglas:** un `record PropiedadAuditada(string Nombre, object? Antes, object? Despues)`. En las creaciones `Antes` es `null` y en las eliminaciones `Despues` es `null`. Las reglas no distinguen de dónde viene el valor, así las pruebas las arman a mano.
- **JSON en el dominio:** `ReglasAuditoria` serializa con `System.Text.Json`, que es parte de .NET, no un paquete nuevo. Usa `JsonNamingPolicy.CamelCase` para las claves y `JsonStringEnumConverter` para los valores. Las claves se convierten con la misma política que usa la API, así coinciden con el JSON de los DTOs.
- **Lista de entidades auditadas** (supuesto 7): `ReglasAuditoria.EntidadesAuditadas` se calcula una vez, por reflexión, con las clases no abstractas del ensamblado del dominio que heredan de `EntidadBase`. Hoy son 10. La usan el validador del filtro `entidad` y una prueba unitaria que fija las 10, para que una entidad nueva se note.
- **Exclusiones:** una tabla `(entidad o *, propiedad)` en `ReglasAuditoria`:
  - Para todas las entidades: `FechaCreacion` y `FechaModificacion`. `Id` tampoco se incluye.
  - Para `Usuario`: `UltimoIngreso` y `SecurityStamp`, más `PasswordHash` y `TempPasswordHash` como enmascaradas.
  - Para `Grupo`: `Version`.
- **Momento de la captura** (spec, punto 2):
  - Las modificaciones y eliminaciones se arman **antes** del primer guardado, porque después `OriginalValues` se reinicia y las entidades eliminadas se desacoplan.
  - Las creaciones se arman **después**, con `Id` y FK definitivas.
  - `ChangeTracker.Entries()` llama a `DetectChanges` por defecto, así que ve también los cambios hechos sobre las entidades sin marcarlas.
- **Diferencias de una modificación:** solo las propiedades con `IsModified` cuyo valor original es distinto del actual (`Equals`). Así una asignación al mismo valor no genera ruido.
- **Autor:**
  - `ICurrentUser.UsuarioId` y `ICurrentUser.Rol`, este último convertido a `Rol`.
  - El correo se lee con `Usuarios.AsNoTracking().Where(u => u.Id == id).Select(u => u.Correo)`, solo si hay registros y un usuario (supuesto 3).
  - `ICurrentUser` no cambia.
- **Operación:** `Activity.Current?.TraceId.ToHexString()`. Si no hay `Activity`, `ActivityTraceId.CreateRandom().ToHexString()`. En los dos casos son 32 caracteres.
- **Fecha:** un solo `TimeProvider.GetUtcNow()` por guardado, compartido por todos sus registros.
- **Transacción:**
  - `Database.CurrentTransaction is null`: abre una con `BeginTransactionAsync` y la confirma al final. `await using` la revierte si algo lanza una excepción.
  - Si ya hay una transacción, no la toca: la confirma quien la abrió. Hoy ningún servicio abre transacciones.
  - Los dos guardados usan `acceptAllChangesOnSuccess: true`. Si no, el segundo volvería a insertar las entidades del primero.
- **Guardados sin auditoría:** si no hay registros ni creaciones, se llama a `base.SaveChangesAsync` directamente, sin transacción. Es el caso de un login normal o un logout.
- **`SaveChanges` síncrono:** se sobrescriben `SaveChanges()` y `SaveChanges(bool)` para lanzar `InvalidOperationException` (supuesto 12).
- **Interceptor:** `AuditoriaInterceptor` se renombra a `FechasInterceptor`, con su archivo y su registro en `DependencyInjection`, sin cambiar su lógica. Como la captura corre antes que el interceptor, las fechas no llegan a la auditoría; además están excluidas.
- **Configuración EF de `RegistroAuditoria`:**
  - Tabla `registro_auditoria` e `Id` `long` con identidad.
  - `Cambios` es `string` con `.HasColumnType("jsonb")`.
  - `Entidad` (50), `UsuarioCorreo` con la misma longitud que `Usuario.Correo` y `OperacionId` (32).
  - Los enums como string, por la convención de `AppDbContext`.
  - Índices `(entidad, entidad_id)`, `(usuario_id)` y `(fecha)`. Sin relaciones, así que no hay FK.
- **Solo lectura en Application:** `IAppDbContext.RegistrosAuditoria` es `IQueryable<RegistroAuditoria>`. `AppDbContext` lo implementa de forma explícita, `IQueryable<RegistroAuditoria> IAppDbContext.RegistrosAuditoria => Set<RegistroAuditoria>().AsNoTracking();`, y solo su propio `SaveChangesAsync` agrega registros, con `Set<RegistroAuditoria>().AddRange(...)`. No hay un `DbSet` público.
- **Consulta:**
  - `AuditoriaService.ListarAsync` aplica los filtros y ordena por `Fecha` descendente e `Id` descendente.
  - Proyecta a un tipo intermedio con `Cambios` como texto, pagina, y después convierte `Cambios` a `JsonElement` en memoria (`JsonDocument.Parse(...).RootElement.Clone()`).
  - `RegistroAuditoriaResponse.Cambios` es `JsonElement`, así OpenAPI y la respuesta lo muestran como objeto.
- **Filtros de la consulta:**
  - `entidad`: se normaliza al nombre exacto de la lista (supuesto 7) y se compara por igualdad, así usa el índice.
  - `search`: `EF.Functions.ILike(r.UsuarioCorreo, $"%{search}%")`, como los demás listados.
  - Fechas: la regla pura `FechaInstituto.RangoUtc(desde, hasta, zona)` devuelve `(DateTimeOffset? Inicio, DateTimeOffset? Fin)` con el fin exclusivo. `IFechaLocal` gana el método `RangoUtc(desde, hasta)`, que `FechaLocal` delega a esa regla con la zona configurada. Así `AuditoriaService` no conoce la zona horaria, y "hoy" y el rango salen del mismo punto.
- **Controller:** `AuditoriaController : ApiControllerBase` con `[Authorize(Policy = Politicas.Administrador)]` y un `[HttpGet]`. La ruta queda en `/api/v1/auditoria` por la convención de nombres.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| Las FK de una creación quedan con ids temporales negativos | Las creaciones se arman después del primer guardado. En T3 se verifica el registro público: el `usuarioId` del `Estudiante` en `cambios` es el id real de la cuenta |
| El segundo guardado vuelve a insertar las entidades o deja la auditoría sin guardar | `acceptAllChangesOnSuccess: true` en los dos. En T3 se verifica que cada operación deja una sola fila de negocio y sus registros |
| Una operación que falla deja registros huérfanos | Transacción alrededor de los dos guardados. En T3 se fuerza un `409 RN-05` y un conflicto de concurrencia, y se comprueba que no aparecen registros |
| Se filtra un hash a `cambios` | El enmascarado está en el dominio, con pruebas por acción. En T3 se busca con `psql` el valor real de `password_hash` dentro de `cambios` y no debe aparecer |
| La captura vuelve lentos los guardados | Solo una consulta extra (el correo), y solo cuando hay registros. Los guardados sin cambios auditables no abren transacción |
| Los logins llenan la tabla | RN-46 excluye `UltimoIngreso` y `SecurityStamp`. En T3 se verifica que un login y un logout no generan registros |
| Una modificación a una entidad no rastreada directamente (por ejemplo, un bloque quitado desde la colección de `Grupo`) no se ve | `Entries()` detecta los cambios antes de leerlos. En T3 se quita un bloque y se verifica la `Eliminacion` de `Horario` |
| Activar más adelante la estrategia de reintentos de Npgsql hace fallar la transacción manual | Hoy no está activada. Queda anotado en la decisión de la transacción: si se activa, el guardado se envuelve en `Database.CreateExecutionStrategy().ExecuteAsync` |
| La migración intenta crear FK o cambia otras tablas | Se revisa el script de la migración en T2: solo `registro_auditoria` y sus índices |
| El filtro de fechas corta mal el día local | Pruebas unitarias del rango con la frontera de las 18:00 UTC. En T4 se verifica con un cambio hecho después de esa hora |

## Puntos de verificación

- **Tras 1:** reglas de auditoría y rango de fechas en verde.
- **Tras 2:** la migración crea solo `registro_auditoria`, sin FK, con `cambios` como `jsonb` y sus tres índices. Ninguna operación existente cambia de comportamiento, y la tabla sigue vacía.
- **Tras 3:** cada operación de la tabla de verificación de la spec deja exactamente los registros esperados, y las que fallan no dejan ninguno. Sin hashes en `cambios`.
- **Tras 4:** los filtros se combinan y el acceso es solo del administrador.
- **Final:** toda la tabla de verificación manual de la spec, los criterios de éxito cumplidos, `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Lo que la spec deja fuera de alcance: eventos que no cambian datos, depuración, registros retroactivos, protección en la BD, historial por recurso y exportación. Las pruebas de integración siguen diferidas.
