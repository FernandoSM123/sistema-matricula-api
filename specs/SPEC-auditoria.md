# Spec: auditoria

Módulo `auditoria` del [mapa de módulos](mapa-modulos.md). Depende de `acceso` (autor del cambio y autorización del administrador) y audita las entidades de todos los módulos, así que se construye después de `matriculas`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/auditoria/plan.md) | [Aprobadas](../tasks/auditoria/todo.md) | 2/5 |

**Historial:**
- 2026-11-10: v1 en revisión, con doce supuestos y una pregunta abierta (cómo hacer inmutable la tabla, porque `REVOKE` no aplica al superusuario `postgres`).
- 2026-11-10: resuelta la pregunta 1 con la opción (c): la inmutabilidad (RN-43) se garantiza solo en la aplicación, sin `REVOKE` ni triggers en la BD. Se corrigieron el punto 1, los criterios de éxito, las verificaciones manuales y `docs/arquitectura.md` §5.1. v1 aprobada con sus doce supuestos.
- 2026-11-10: aclaración al redactar el plan, sin cambio de alcance: los registros de creación se arman después del primer guardado, porque antes las FK hacia entidades nuevas del mismo guardado tienen ids temporales (punto 2). Plan y lista de 5 tareas en revisión.
- 2026-11-10: plan y lista de tareas aprobados; la implementación empezará por T1.

## Objetivo

Que quede constancia automática de cada creación, modificación y eliminación de datos: quién la hizo, cuándo, sobre qué registro y qué valores cambiaron, y que el administrador pueda consultarlo. La captura no depende de que cada servicio se acuerde de registrar: se hace en un único punto, al guardar, en la misma transacción que el cambio.

Cubre RF-56 y RF-57, las reglas RN-42 a RN-46, la consulta 11 de la sección 6 y la decisión 11 del mapa, con el diseño de `docs/arquitectura.md` §5.1.

**Usuarios:**
- Administrador: consulta la auditoría con filtros.
- Estudiante y profesor: no la consultan (`403`), pero sus cambios quedan registrados con su autoría.
- Visitante: no la consulta (`401`). Sus cambios (registro público, olvido de contraseña) quedan a nombre del Sistema (RN-45).

### Alcance

1. **Entidad `RegistroAuditoria`** (§5.12 de los requerimientos), en `Domain/Entities`. **No** hereda de `EntidadBase`, porque nunca se modifica (RN-43).

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Id` | `long` | Identidad generada por PostgreSQL |
   | `Fecha` | `DateTimeOffset` | UTC, de `TimeProvider`. La misma para todos los registros de un guardado |
   | `Accion` | `AccionAuditoria` | Enum nuevo: `Creacion`, `Modificacion`, `Eliminacion`. Se guarda como string |
   | `Entidad` | `string` (50) | Nombre de la clase, por ejemplo `Matricula` |
   | `EntidadId` | `int` | Id del registro afectado. **Sin FK** |
   | `Cambios` | `string` → `jsonb` | Ver el punto 3 |
   | `UsuarioId` | `int?` | Autor. **Sin FK**. `null` = Sistema (RN-45) |
   | `UsuarioCorreo` | `string?` | Copia del momento (RN-45, supuesto 3) |
   | `UsuarioRol` | `Rol?` | Copia del momento. Se guarda como string |
   | `OperacionId` | `string` (32) | Agrupa los registros de una operación (RN-42, supuesto 4) |

   Sin setters públicos: se crea completa por constructor. La única excepción es `EntidadId`, que la captura completa después del primer guardado en las creaciones (punto 2), mediante un método interno de Infrastructure o equivalente.

   Índices (`docs/arquitectura.md` §5.1): `(entidad, entidad_id)`, `(usuario_id)` y `(fecha)`.

   Migración `AgregaRegistroAuditoria`: tabla `registro_auditoria` con sus índices. Sin protección en la BD: la inmutabilidad la garantiza la aplicación (punto 6, pregunta 1).

2. **Captura en `AppDbContext`** (RF-56, RN-42), en `Infrastructure/Persistence/Auditoria/`:
   - `AppDbContext` sobrescribe `SaveChangesAsync(bool, CancellationToken)`. El `SaveChangesAsync(CancellationToken)` de `IAppDbContext` pasa por ahí.
   - Flujo:
     1. Recorre `ChangeTracker.Entries<EntidadBase>()` en estado `Added`, `Modified` o `Deleted`. Arma ya los registros de modificación y eliminación, con las reglas del punto 3, porque después del guardado se pierden los valores originales. Las creaciones solo se anotan. Las modificaciones sin cambios auditables no generan registro (RN-46).
     2. Si no hay registros ni creaciones, guarda como siempre y termina.
     3. Si no hay una transacción en curso, abre una.
     4. `base.SaveChangesAsync()`: las entidades nuevas reciben su `Id`.
     5. Arma los registros de creación con el `EntidadId` y los valores ya definitivos. Antes del guardado, las FK hacia entidades nuevas del mismo guardado (por ejemplo, `Estudiante.UsuarioId` en el registro público) tienen un id temporal. Agrega todos los registros y llama de nuevo a `base.SaveChangesAsync()`.
     6. Confirma la transacción, si la abrió. Ante cualquier excepción la revierte y la relanza, sin capturarla: `DbUpdateConcurrencyException` sigue llegando a `GlobalExceptionHandler` como hoy.
   - `SaveChanges` síncrono: ningún servicio lo usa. Se sobrescribe para lanzar `InvalidOperationException`, así ninguna escritura se salta la auditoría.
   - La lógica vive en una clase de Infrastructure (por ejemplo, `CapturaAuditoria`), inyectada en `AppDbContext`, que usa `ICurrentUser`, `TimeProvider` y las reglas puras del dominio (punto 3). `AppDbContext` solo la orquesta.

3. **Qué se guarda en `Cambios`** (RN-44, RN-46). Las reglas son puras y viven en el dominio (`Domain/Reglas/ReglasAuditoria.cs`), así se prueban sin BD:
   - Solo las propiedades escalares, incluidas las FK (`GrupoId`, `EstudianteId`, `CreadoPorId`), sin navegaciones. `Id` no aparece: ya está en `EntidadId`.
   - **Creación:** `{ "propiedad": valor, ... }` con todos los valores iniciales.
   - **Eliminación:** el mismo formato, con los últimos valores.
   - **Modificación:** solo las propiedades cuyo valor original es distinto del actual: `{ "estado": { "antes": "Activa", "despues": "Retirada" } }`.
   - **Excluidas siempre** (RN-46): `FechaCreacion`, `FechaModificacion` (las asigna el interceptor, y la auditoría tiene su propia `Fecha`), `Usuario.UltimoIngreso`, `Usuario.SecurityStamp` y `Grupo.Version` (`xmin`).
   - **Enmascaradas** (RN-44): `Usuario.PasswordHash` y `Usuario.TempPasswordHash` se guardan como `"***"` si tienen valor y como `null` si no. Así se ve que la contraseña se definió o cambió, o que la temporal se asignó o se anuló, sin el hash.
   - **Formato de valores:** `System.Text.Json` con los nombres de propiedad en camelCase (supuesto 6) y los enums como string, igual que la API. `DateOnly`, `TimeOnly` y `DateTimeOffset` en ISO 8601.
   - Si una modificación queda sin propiedades después de las exclusiones, no se escribe (RN-46). Por ejemplo, un login con la contraseña definitiva y sin temporal pendiente solo cambia `UltimoIngreso`, y un logout solo cambia `SecurityStamp`.

4. **Autor y operación** (RN-45, RN-42):
   - `UsuarioId` y `UsuarioRol` salen de `ICurrentUser` (claims `sub` y `role`).
   - `UsuarioCorreo` se lee de la cuenta en la BD al guardar (supuesto 3). Es una consulta por clave primaria, sin seguimiento, y solo se hace si hay registros que escribir y hay un usuario.
   - Sin usuario (registro público, login, olvido de contraseña, seed del administrador): los tres datos quedan en `null`, es decir, Sistema.
   - `OperacionId` = `Activity.Current?.TraceId` en hexadecimal: es el mismo trace id que forma parte del `traceId` de los `ProblemDetails`. Sin `Activity`, por ejemplo en el seed, se genera uno nuevo por guardado.

5. **Endpoint de consulta** (RF-57, consulta 11)

   | Método y ruta | Acceso | RF / RN | Respuesta |
   |---|---|---|---|
   | `GET /api/v1/auditoria` | Administrador | RF-57, RN-43 | `200` paginado · `400` · `401` · `403` |

   Parámetros (`ListarAuditoriaRequest : PaginacionRequest`), todos opcionales y combinables:

   | Parámetro | Tipo | Filtro |
   |---|---|---|
   | `entidad` | `string` | Nombre exacto de la entidad, sin distinguir mayúsculas (supuesto 7) |
   | `entidadId` | `int` | Registro concreto. Exige `entidad` |
   | `usuarioId` | `int` | Autor. No se valida que exista: la cuenta puede haberse eliminado (RN-43) |
   | `accion` | `AccionAuditoria` | `Creacion`, `Modificacion` o `Eliminacion` |
   | `desde`, `hasta` | `DateOnly` | Fechas locales del instituto, ambas incluidas (supuesto 8). Para un día, la misma fecha en las dos |
   | `operacionId` | `string` | Todos los registros de una operación (supuesto 9) |
   | `search` | `string` | Busca en el correo del autor (supuesto 10) |

   Respuesta (`RegistroAuditoriaResponse`), ordenada por `Fecha` descendente e `Id` descendente:
   ```json
   {
     "id": 120, "fecha": "2026-11-10T15:04:05Z", "accion": "Modificacion",
     "entidad": "Matricula", "entidadId": 42,
     "usuarioId": 7, "usuarioCorreo": "ana@correo.com", "usuarioRol": "Estudiante",
     "operacionId": "4bf92f3577b34da6a3ce929d0e0e4736",
     "cambios": { "estado": { "antes": "Activa", "despues": "Retirada" }, "fechaRetiro": { "antes": null, "despues": "2026-11-10" } }
   }
   ```
   `cambios` sale como objeto JSON (`JsonElement`), no como texto. Un autor `null` se muestra tal cual; el frontend lo presenta como "Sistema".

6. **Lectura desde Application:** `IAppDbContext` expone `IQueryable<RegistroAuditoria> RegistrosAuditoria` (solo lectura), no un `DbSet`. Así ningún servicio puede agregar, modificar ni eliminar registros (RN-43). `AuditoriaService.ListarAsync` filtra, ordena, proyecta y pagina con `ToPagedResultAsync`, usando `AsNoTracking`.

7. **Validación** (`ListarAuditoriaRequestValidator`), con `400` y el campo:
   - `entidadId` y `usuarioId` mayores que 0.
   - `entidadId` sin `entidad`: es ambiguo, porque los ids se repiten entre entidades.
   - `entidad` debe ser una de las entidades auditadas (supuesto 7). El mensaje lista las válidas.
   - `desde ≤ hasta` si vienen las dos.
   - `accion` inválida: la rechaza el enlace del modelo, como los demás enums.

8. **Ajustes en el código existente:**
   - `AuditoriaInterceptor` se renombra a `FechasInterceptor` (decisión 11 del mapa), sin cambiar su comportamiento.
   - `AppDbContext`: `DbSet<RegistroAuditoria>`, la sobrescritura de `SaveChangesAsync` y la de `SaveChanges`.
   - `Matricula.Api.http`: ejemplos de la consulta.
   - `AGENTS.md`: rangos RF-01..RF-57 y RN-01..RN-46, y en "Nunca" escribir con `ExecuteUpdate`, `ExecuteDelete` o SQL manual (`docs/arquitectura.md` §5).

### Fuera de alcance

- Eventos que no cambian datos: logins fallidos, logout, envío de correos, accesos denegados (§1.3 de los requerimientos, decisión 11 del mapa).
- Depuración o archivado de registros antiguos (§1.3).
- Auditar los datos que ya existen antes de la migración: la auditoría empieza a registrar desde que se despliega (supuesto 11).
- Mensajes de negocio ("matrícula retirada"): la acción y los cambios ya lo muestran.
- Historial por recurso (`/estudiantes/{id}/historial`): el usuario eligió un único endpoint.
- Exportar la auditoría (CSV, PDF).
- Pruebas de integración: diferidas, como en los demás módulos.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3 (`jsonb` sobre `string`), `System.Text.Json`, FluentValidation 12.2, xUnit 2.9.3. **No se añaden paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaRegistroAuditoria --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/
  Entities/RegistroAuditoria.cs              nuevo (no hereda de EntidadBase)
  Enums/AccionAuditoria.cs                   nuevo
  Reglas/ReglasAuditoria.cs                  nuevo: exclusiones, enmascarado, diferencias (RN-44, RN-46)
  Reglas/FechaInstituto.cs                   + rango UTC de un rango de fechas locales (supuesto 8)
src/Matricula.Application/
  Abstractions/IAppDbContext.cs              + IQueryable<RegistroAuditoria> RegistrosAuditoria
  Auditoria/                                 AuditoriaService, ListarAuditoriaRequest (+ validador), RegistroAuditoriaResponse
  DependencyInjection.cs                     + AuditoriaService
src/Matricula.Infrastructure/
  Persistence/AppDbContext.cs                + DbSet, SaveChangesAsync / SaveChanges sobrescritos
  Persistence/Auditoria/CapturaAuditoria.cs  nuevo: arma los registros desde el ChangeTracker
  Persistence/Configurations/RegistroAuditoriaConfiguration.cs   nuevo
  Persistence/Interceptors/FechasInterceptor.cs                  renombrado desde AuditoriaInterceptor
  Persistence/Migrations/                    AgregaRegistroAuditoria
  DependencyInjection.cs                     + CapturaAuditoria, nombre del interceptor
src/Matricula.Api/
  Controllers/AuditoriaController.cs         nuevo
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/{ReglasAuditoriaTests,FechaInstitutoTests}.cs
AGENTS.md                                    rangos RF/RN y prohibición de escrituras fuera del change tracker
```

## Code Style

Igual que los módulos anteriores: constructores primarios, un archivo por clase, setters privados, reglas puras en el dominio, `AsNoTracking` y `Select` en las lecturas, `[Authorize(Policy = Politicas.Administrador)]`, pruebas `Metodo_Escenario_Resultado`.

```csharp
public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
{
    var pendientes = await captura.CapturarAsync(ChangeTracker, cancellationToken);   // RN-44, RN-46
    if (pendientes.Count == 0)
    {
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    await using var transaccion = Database.CurrentTransaction is null
        ? await Database.BeginTransactionAsync(cancellationToken)
        : null;

    // Siempre acepta los cambios: si no, el segundo guardado volvería a insertar las entidades del primero.
    var filas = await base.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    RegistrosAuditoria.AddRange(pendientes.Completar());                              // EntidadId de las creaciones
    await base.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    if (transaccion is not null)
    {
        await transaccion.CommitAsync(cancellationToken);                             // RN-42: todo o nada
    }

    return filas;
}
```

```csharp
[Fact]
public void Diferencias_SoloCambiaUltimoIngreso_NoGeneraRegistro()
{
    var cambios = ReglasAuditoria.Diferencias(nameof(Usuario),
        [new("UltimoIngreso", null, DateTimeOffset.UtcNow)]);

    Assert.Empty(cambios);
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD), sobre `ReglasAuditoria` y `FechaInstituto`:
  - Excluye `FechaCreacion`, `FechaModificacion`, `UltimoIngreso`, `SecurityStamp` y `Version`.
  - Enmascara `PasswordHash` y `TempPasswordHash`: `"***"` con valor y `null` sin valor, en creación, modificación y eliminación.
  - En una modificación solo incluye las propiedades con valor distinto. Una modificación que solo toca propiedades excluidas queda vacía.
  - Nombres en camelCase y enums como string.
  - Rango de fechas: `desde`/`hasta` locales → `[desde 00:00, hasta + 1 día 00:00)` en UTC con `America/Costa_Rica`, incluida la frontera de las 18:00 UTC.
- **Manual**, con la API y PostgreSQL en marcha, usando `Matricula.Api.http`, Scalar y `psql`:

| Qué | Esperado |
|---|---|
| Admin crea un curso | Un registro `Creacion`, `entidad = Curso`, con su `entidadId` real, todos los valores y el admin como autor |
| Admin modifica solo el nombre del curso | Un registro `Modificacion` con solo `nombre` (antes y después) |
| Admin desactiva un estudiante | `Modificacion` de `Usuario` con `activo: true → false`, sin `securityStamp` |
| Estudiante se matricula | `Creacion` de `Matricula` y `Modificacion` de `Grupo` (`cuposOcupados`), con el mismo `operacionId` y el estudiante como autor |
| Cambio de grupo | Cuatro registros (dos matrículas y dos grupos) con el mismo `operacionId` |
| Cancelar un grupo con matrículas | Un registro por matrícula retirada y uno del grupo, con el mismo `operacionId` |
| Matrícula rechazada (por ejemplo, `409 RN-05`) o conflicto de concurrencia | Ningún registro nuevo |
| Registro público | Registros de `Usuario` y `Estudiante` con autor `null` (Sistema); `passwordHash: "***"` |
| Login con la contraseña definitiva, sin temporal pendiente | Ningún registro |
| Login con la definitiva y una temporal pendiente | `Modificacion` de `Usuario` con `tempPasswordHash: "*** → null"`, autor `null` |
| Logout | Ningún registro |
| Cambiar la propia contraseña | `Modificacion` de `Usuario` con `passwordHash: "*** → ***"` y `fechaCambioContrasena`; ningún hash en la tabla |
| Eliminar un estudiante sin matrículas | `Eliminacion` de `Estudiante` y de `Usuario` con sus últimos valores; la eliminación no se bloquea |
| `GET /auditoria?entidad=Matricula&entidadId=42` | Solo los registros de esa matrícula, del más reciente al más antiguo |
| `GET /auditoria?usuarioId=…&desde=…&hasta=…` y `accion=…` combinados | Filtra y pagina |
| `desde` = `hasta` = hoy, con un cambio hecho después de las 18:00 de Costa Rica | El cambio aparece |
| `entidadId` sin `entidad` · `entidad=Inexistente` · `desde > hasta` | `400` con el campo |
| `GET /auditoria` con token de estudiante o profesor / sin token | `403` / `401` |
| Con cambio de contraseña pendiente | `403` (RN-24) |
| Buscar `PasswordHash` o `TempPasswordHash` reales en `cambios` | No aparece ningún hash |

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. La auditoría y el cambio en la misma transacción. Las reglas de exclusión y enmascarado en el dominio, probadas. Las fechas del filtro en hora local del instituto.
- **Preguntar antes:** añadir paquetes NuGet (por ejemplo, Audit.NET). Auditar eventos que no cambian datos. Cambiar el esquema fuera de esta spec. Agregar filtros o endpoints que no estén aquí.
- **Nunca:** guardar hashes, temporales o el `SecurityStamp` en `cambios` (RN-44). FK desde `registro_auditoria`. Endpoints o métodos de servicio que modifiquen o eliminen registros de auditoría (RN-43). Escribir con `ExecuteUpdate`, `ExecuteDelete` o SQL manual. Registrar la auditoría a mano desde un servicio. Capturar `DbUpdateConcurrencyException` en la auditoría para ocultarla.

## Success Criteria

1. La migración crea `registro_auditoria` sin FK, con sus índices y `cambios` como `jsonb`. Application solo puede leerla (`IQueryable`), y ningún endpoint la modifica.
2. Cada creación, modificación y eliminación de las diez entidades deja su registro, con autor, fecha, `entidadId` real y los cambios del formato del punto 3 (RF-56).
3. Una operación que falla no deja registros, y una operación compuesta deja todos los suyos con el mismo `operacionId` (RN-42).
4. Ningún registro contiene hashes, temporales ni el `SecurityStamp` (RN-44). Los logins normales y los logouts no generan registros (RN-46).
5. Eliminar un estudiante o profesor sigue funcionando, y sus registros quedan (RN-43).
6. `GET /api/v1/auditoria` filtra por entidad, registro, usuario, acción, rango de fechas, operación y correo, pagina y solo lo usa el administrador (RF-57).
7. Las verificaciones manuales de la tabla dan el resultado esperado.
8. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **Se auditan las entidades que heredan de `EntidadBase`:** `Usuario`, `Administrador`, `Estudiante`, `Profesor`, `Curso`, `Periodo`, `Aula`, `Grupo`, `Horario` y `Matricula`. Una entidad futura que herede de `EntidadBase` queda auditada sin tocar nada.
2. **Las propiedades derivadas también se auditan.** `Grupo.CuposOcupados` cambia con cada matrícula y retiro; se registra porque explica la variación del cupo y relaciona la matrícula con su grupo dentro de la operación.
3. **El correo del autor se lee de la BD, no del token.** El claim `email` del token puede estar desactualizado hasta una hora después de un cambio de correo (RF-55), porque cambiar el correo no regenera el `SecurityStamp`. La consulta es una por guardado y solo cuando hay registros. Si el autor cambia su propio correo en ese guardado, se guarda el correo anterior: el que tenía al hacer el cambio.
4. **Una operación es una petición HTTP.** `OperacionId` es el trace id de la petición, así que si un servicio guarda dos veces en la misma petición, los dos guardados comparten operación.
5. **En el login y el olvido de contraseña el autor es el Sistema**, aunque el registro afectado sea la cuenta de quien ingresa: todavía no hay sesión (RN-45). En el cambio obligatorio (RF-07) sí hay sesión, así que el autor es el propio usuario.
6. **Las claves de `cambios` van en camelCase** (`cuposOcupados`), igual que el JSON de la API, para que el frontend no tenga que traducirlas. `entidad` conserva el nombre de la clase (`Matricula`), que es el valor que se usa para filtrar.
7. **El filtro `entidad` se valida** contra los nombres de las entidades auditadas, que se obtienen del dominio (las clases que heredan de `EntidadBase`), sin distinguir mayúsculas. Un nombre desconocido responde `400` con la lista válida, en lugar de una lista vacía que parecería "sin cambios".
8. **`desde` y `hasta` son fechas locales del instituto, ambas incluidas** (decisión 9 del mapa). Se convierten al rango UTC `[desde 00:00, hasta + 1 día 00:00)` en `Instituto:ZonaHoraria`. Con solo `desde`, no hay límite superior; con solo `hasta`, no hay límite inferior.
9. **Filtro `operacionId`** (agregado, no pedido explícitamente): permite ver juntos todos los registros de un cambio de grupo o de una cancelación (RN-42), que es la razón de guardar el identificador. Si no se quiere, se quita sin afectar lo demás.
10. **`search` busca en el correo del autor** (contiene, sin distinguir mayúsculas). El administrador conoce los correos, no los ids de las cuentas. `PaginacionRequest` ya trae `search`, así que no es un parámetro nuevo.
11. **Sin registros retroactivos.** Los datos que existen antes de la migración no tienen registro de creación; su primer registro será su próximo cambio.
12. **`SaveChanges` síncrono lanza una excepción** en lugar de auditar: hoy nadie lo usa, y así ningún guardado nuevo se salta la auditoría por error.

## Decisiones

Se aplican las decisiones 1 a 5, 9 y 11 del mapa, y el diseño de `docs/arquitectura.md` §5.1.

## Preguntas abiertas

1. ~~Cómo hacer inmutable la tabla en la BD (RN-43).~~ Resuelta el 2026-11-10 con la opción (c): sin protección en la BD. La aplicación no puede modificar ni eliminar registros (solo los lee por `IQueryable` y no hay endpoints de escritura). Quien tenga acceso directo a la BD sí podría alterarlos, y eso se acepta por ahora.

No quedan preguntas abiertas.

<details>
<summary>Opciones que se evaluaron</summary>

**Cómo hacer inmutable la tabla en la BD (RN-43).** `docs/arquitectura.md` §5.1 dice que la migración hace `REVOKE UPDATE, DELETE`. Pero la API se conecta como `postgres` (`appsettings.Development.json`), que es superusuario, y **los superusuarios ignoran los permisos**: el `REVOKE` no impediría nada. Opciones:
   - **(a) Triggers (recomendada).** En la migración, una función y triggers `BEFORE UPDATE OR DELETE` (por fila) y `BEFORE TRUNCATE` (por sentencia) que lanzan un error. Funcionan con cualquier usuario, incluido el superusuario, salvo que alguien los desactive a propósito. Son unas 15 líneas de SQL en la migración y el `Down` las elimina. Habría que corregir §5.1 de la arquitectura.
   - (b) Mantener el `REVOKE` y crear un usuario de BD propio para la API, que no sea superusuario. Es lo más correcto en producción, pero cambia la configuración de desarrollo y el README.
   - (c) Sin protección en la BD: solo el código impide modificar, porque Application solo ve un `IQueryable`. Es lo más simple, pero `psql` podría alterar los registros. **Elegida.**

</details>
