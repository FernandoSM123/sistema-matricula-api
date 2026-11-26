# Tareas: nombres

Corrección transversal `nombres` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-nombres.md`](../../specs/SPEC-nombres.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-26)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` con **1104** pruebas en verde, la verificación indicada y un commit en la rama `nombres`. Los nombres salen de las tablas de la spec (§2.1 a §2.8); uno que no esté y que la regla no deje claro se pregunta.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. La regla en los documentos y la línea base del OpenAPI | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T2. Domain | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T3. Piezas técnicas de Application e Infrastructure | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T4. Api técnica, métodos de los servicios y acciones | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T5. DTO y validadores | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T6. Configuración y datos de prueba | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T7. Cierre | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |

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

### T1. La regla en los documentos y la línea base del OpenAPI
- Acceptance:
  - Rama `nombres` creada desde `main`.
  - `docs/arquitectura.md` §8 reescrita con la regla de la spec (español / inglés / combinados / verbos / ante la duda), ejemplos y una tabla de piezas (servicio, DTO, opciones, interfaz técnica, prueba).
  - `AGENTS.md`: la regla resumida en *Siempre* y, en *Preguntar antes*, "el nombre o el idioma de algo, cuando la regla no lo deja claro". Los nombres concretos que cita se cambian en T7.
  - `openapi-antes.json` guardado en el scratchpad con la API en marcha.
- Verify: lectura de §8 contra la spec; el archivo de la línea base existe y es JSON válido.
- Files: `docs/arquitectura.md`, `AGENTS.md`.

### T2. Domain
- Acceptance:
  - `Domain/Reglas/` → `Domain/Rules/`, con las clases y constantes de §2.2 (`HorarioRules`, `PeriodoRules`, `AuditRules`, `ErrorLogRules`, `PasswordPolicy`, `AuditedProperty`, `LocalDateRules`, `*Message`).
  - Métodos de `Grupo`, `Horario`, `Matricula`, `Periodo`, `Curso`, `Aula`, `Estudiante`, `Profesor`, `Administrador` y `Usuario` según §2.2, con sus llamadas en todas las capas.
  - `Usuario.TemporaryPasswordLifetime`, `ForgotPasswordLifetime`.
  - `BusinessRuleException.Extensions` (y el parámetro), con sus usos en Application y `GlobalExceptionHandler`.
  - Las clases y los métodos de prueba de `tests/Matricula.UnitTests/Domain` y `Common` según §2.8.
  - Ninguna propiedad de entidad cambia.
- Verify:
  - `dotnet test` con 1104 pruebas.
  - `migrations add VerificaNombres` sin operaciones, y `migrations remove`.
  - El diff no cambia strings ni comentarios (salvo `<see cref>` que nombran lo renombrado).
- Files: `src/Matricula.Domain/**`, sus usos en Application, Infrastructure y Api, `tests/Matricula.UnitTests/**`.

### T3. Piezas técnicas de Application e Infrastructure
- Acceptance:
  - Carpetas de §2.1 en Application e Infrastructure: `Common/Pagination`, `Common/Validation`, `Email`, `Grupos/Queries`, `Persistence/Audit`, `Persistence/ErrorLog`, `Security`, `Time` (`Seed/TestData` va en T6).
  - Abstracciones de §2.3 con sus implementaciones de §2.4: `ITokenGenerator`/`TokenData`/`GeneratedToken`, `IPasswordGenerator`/`PasswordGenerator`, `IPasswordHashService`/`PasswordHashService`/`PasswordCheckResult`, `ILocalClock`/`LocalClock`, `IEmailSender.SendAsync`, `IErrorRecorder`/`DbErrorRecorder`/`UnexpectedError`, `ICurrentUser.IsAuthenticated`.
  - `EmailDispatcher`, `EmailTemplates`, `EmailTemplate`; `PageRequest`, `PaginationExtensions`; `*Projections`; `GruposGuards`, `MatriculasGuards` y sus métodos `Ensure*`/`Find*`; `HorarioConflict`, `IGrupoData`, `IPeriodoData`, `GrupoDataValidator`, `PeriodoDataValidator`, `NewPasswordRules`, `CuentaWithTemporaryPassword`.
  - Infrastructure: `TimestampsInterceptor`, `AuditCapture`, `CapturedChange`, `PendingChanges`, `ChangeAuthor`, `ErrorLogPurgeService`, `ErrorLogOptions` (sin cambiar todavía la clave de configuración: T6), `AdminSeedService.RunAsync`, `SeedExtensions`, los `[LoggerMessage]` en inglés.
  - `JwtTokenGenerator` en `Api/Security` cambia junto con `ITokenGenerator` (solo el nombre de la clase y del método; la carpeta en T4).
- Verify:
  - `dotnet test` con 1104 pruebas; migración de prueba vacía.
  - Con la API en marcha: olvido de contraseña con smtp4dev (llega el correo), un listado paginado y una modificación que aparece en `GET /auditoria`.
- Files: `src/Matricula.Application/**`, `src/Matricula.Infrastructure/**` (sin `Migrations/` ni `Seed/DatosPrueba/`), `src/Matricula.Api/Seguridad/GeneradorToken.cs`, `tests/Matricula.UnitTests/Common/**`.

### T4. Api técnica, métodos de los servicios y acciones
- Acceptance:
  - `Api/Seguridad/` → `Api/Security/` con las clases de §2.5 (`TokenClaims`, `JwtTokenGenerator.CreateSigningKey`, `RateLimitPolicies` con sus valores nuevos, `Policies`, `SecurityExtensions.AddSecurity`, `SecurityStampValidation`, `AuthorizationResultHandler`, `NoPasswordChangePendingRequirement`).
  - `Api/Common`: `JsonSettings.Configure`, `InvalidModelStateResponse.Create`, `NonNullableEnumSchemaTransformer`.
  - Métodos de todos los servicios según la tabla de §2.3, y acciones de todos los controllers según §2.5, con sus `nameof`.
  - Los nombres de los controllers no cambian.
- Verify:
  - `dotnet test` con 1104 pruebas.
  - Con `Matricula.Api.http`: login; crear un curso (`201` con el `Location` correcto); `GET /cursos/{id}`; un endpoint de administrador con token de estudiante (`403`); seis olvidos de contraseña seguidos (`429` con `Retry-After`); matricular y cambiar de grupo.
  - El OpenAPI no cambia todavía: diff vacío contra la línea base.
- Files: `src/Matricula.Api/**`, los `*Service.cs` de Application.

### T5. DTO y validadores
- Acceptance:
  - Requests y responses de §2.6 renombrados, con sus validadores y sus usos en servicios, proyecciones y controllers.
  - `Listar*Request` → `List*Request`.
  - Las propiedades de los DTO no cambian.
- Verify:
  - `dotnet test` con 1104 pruebas.
  - `openapi-despues.json`: el diff contra la línea base solo renombra los esquemas de §2.6 y sus `$ref`; ningún cambio en `paths`, propiedades, enums ni códigos de respuesta.
  - Una petición de cada tipo de body cambiado responde igual (registro público, matricular, cambiar contraseña).
- Files: los `*Request.cs`, `*Response.cs` y `*Validator.cs` de Application, sus usos.

### T6. Configuración y datos de prueba
- Acceptance:
  - Propiedades de las opciones y claves de §2.7: `JwtOptions` (`Key`, `AccessTokenMinutes`, `MinKeyLength`), `SmtpOptions`, `AdminSeedOptions.TemporaryPassword`, `InstituteOptions` (`Institute:TimeZone`), `ErrorLogOptions` (`ErrorLog:RetentionDays`), `TestDataOptions` (`TestData:*`).
  - `Seed/DatosPrueba/` → `Seed/TestData/` con las clases de §2.4, y `TestDataOptionsTests`, `TestDatesTests`.
  - `appsettings.json`, `appsettings.Development.json`, `secrets.example.json` y la tabla de claves, los ejemplos y la sección de datos de prueba del `README.md`, con el paso manual de renombrar las claves de user-secrets.
  - Los mensajes de validación de las opciones nombran las claves nuevas.
- Verify:
  - `dotnet test` con 1104 pruebas.
  - Arrancar sin `Jwt:Key` (renombrada solo en el secrets.json) falla con el mensaje nuevo; con las claves renombradas arranca.
  - BD recreada (`database drop` + `update`) y siembra con `TestData:Enabled = true`: aparecen los datos y las cuentas de prueba entran con `TestData:Password`.
  - `ErrorLog:RetentionDays` e `Institute:TimeZone` con un valor distinto se leen (log de la depuración, `GET /periodos/vigente`).
- Files: las opciones, `src/Matricula.Infrastructure/Seed/**`, `appsettings*.json`, `secrets.example.json`, `README.md`, sus pruebas.

### T7. Cierre
- Acceptance:
  - `docs/arquitectura.md` §1 a §7 y §9 y `AGENTS.md` usan los nombres y claves nuevos (`ILocalClock.Today()`, `Institute:TimeZone`, `IErrorRecorder`, `EmailDispatcher`, `ErrorLogPurgeService`, `JsonSettings`, `TestData:*`, `Seed/TestData/`…).
  - `specs/mapa-modulos.md`: decisión nueva (regla de nombres, que remite a la spec), la nota de que los documentos históricos conservan los nombres viejos y el estado de la corrección.
  - La spec con su historial y el avance; esta lista con todas las columnas.
- Verify:
  - Búsqueda de los nombres viejos de las tablas en `src/` (sin `Migrations/`), `tests/`, `appsettings*.json`, `secrets.example.json`, `AGENTS.md`, `docs/arquitectura.md` y `README.md`: sin resultados.
  - `git diff main -- '*.cs' | grep '^[-+].*"'`: solo `nameof`, claves técnicas y valores de políticas.
  - Diff final del OpenAPI y migración de prueba vacía.
  - La verificación manual de *Testing Strategy* de la spec: login, olvido con smtp4dev, matricular y cambiar de grupo, la auditoría con el mismo `Entidad` que antes y un `500` forzado que aparece en `GET /errores`.
  - Los 7 criterios de éxito de la spec.
- Files: `docs/arquitectura.md`, `AGENTS.md`, `specs/mapa-modulos.md`, `specs/SPEC-nombres.md`, este `todo.md`.
