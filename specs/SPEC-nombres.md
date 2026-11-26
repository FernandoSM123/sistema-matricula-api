# Spec: nombres

Corrección transversal de la API, equivalente a `SPEC-nombres` del SPA. No es un módulo del [mapa](mapa-modulos.md): cambia la regla de nombres de `docs/arquitectura.md` §8 y renombra lo que construyeron los módulos ya completados. Se hace **antes** del módulo [`sesiones`](SPEC-sesiones.md), para que su código nazca con la regla nueva.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/nombres/plan.md) | [Aprobadas](../tasks/nombres/todo.md) | 7/7 tareas |

**Historial:**
- 2026-11-26: v1 en revisión. Parte del análisis de factibilidad hecho a pedido del usuario: el renombre es viable, no toca la BD y solo cambia el contrato en los nombres de los esquemas del OpenAPI (§2.6).
- 2026-11-26: v1 aprobada por el usuario, que resolvió las tres preguntas abiertas como se proponían (decisiones 4 a 6). Plan y tareas en revisión.
- 2026-11-26: plan y tareas aprobados. T1 completada: regla en `docs/arquitectura.md` §8.1 y en `AGENTS.md`; línea base del OpenAPI con 55 rutas y 73 esquemas.
- 2026-11-26: T2 completada, sin cambio de alcance. Los miembros de las reglas, que la spec no listaba, siguen la regla: `HorarioRules.AreValidHours`/`Overlap`; `PeriodoRules.MatriculaInOrder`, `ClasesInOrder`, `MatriculaWithinClases`, `RetiroWithinPeriodo`, `ClasesOverlap`, `AreConsistent` y sus `*Message`; `AuditRules.Mask`, `AuditedEntities`, `FindEntity`, `IsExcluded`, `IsMasked`, `Values`, `Differences`; `ErrorLogRules.Truncate`, `NormalizeTraceId`, `RetentionLimit`; `PasswordPolicy.MinLength`, `Message`, `IsValid`; `LocalDateRules.ToLocalDate`, `ToUtcRange`. Las propiedades `Antes`/`Despues` del cambio auditado no cambian: son las claves del JSON guardado. 1104 pruebas en verde y migración de prueba vacía.
- 2026-11-26: T3 completada, sin cambio de alcance. Detalles que la spec no fijaba:
  - El parámetro `to` de `IEmailSender.SendAsync` pasa a `recipient`: el analizador (CA1716) rechaza `to`, que es palabra reservada en VB.
  - El valor `MotivoTemporal.CuentaNueva` **no cambia**: se llama igual que la clase `CuentaNueva` y el reemplazo lo había tocado; se revirtió antes del commit, porque se guarda como texto en la BD. Ningún otro nombre renombrado coincide con un valor de enum.
  - `EmailTemplate` (`Subject`, `HtmlBody`), `CuentaWithTemporaryPassword.TemporaryPassword`, `PageRequest.MaxPageSize`, `GruposGuards.IsActive`, `ChangeAuthor.System`, `PendingChanges.IsEmpty`/`Complete`/`None`.
  - Las dependencias inyectadas toman el nombre de su tipo nuevo (`localClock`, `tokenGenerator`, `passwordGenerator`, `emailDispatcher`, `errorRecorder`). Los campos privados, los parámetros y las variables locales en español se dejan como están.
  - Se adelantan dos renombres de la tabla: `CuentaService.SendTemporaryPasswordAsync` (T4) y `TestDataSeedService.RunAsync` (T6), porque comparten nombre con los de esta tarea.
  - Verificación con la API: olvido de contraseña con el correo en smtp4dev, `pageSize` limitado a 100 en la oferta y el registro de auditoría del cambio, con autor Sistema y el hash como `***`.
- 2026-11-26: T4 completada, sin cambio de alcance. Detalles que la spec no fijaba:
  - Los nombres internos de las políticas de rate limit pasan a `"forgot-password"` y `"sign-up"` (constantes de `RateLimitPolicies`); no salen en la API ni en el OpenAPI.
  - Métodos privados de los servicios: `Ensure*` para las validaciones que lanzan (`EnsureCedulaAvailableAsync`, `EnsureNoOverlapsAsync`...), `Load*` para las búsquedas que lanzan 404, `CurrentProfesorId`/`CurrentEstudianteId`, `IsAdministrador`, `QueryPageAsync`, `SaveAsync`.
  - Las acciones de los controllers cambian de nombre sin cambiar rutas: el OpenAPI generado es idéntico a la línea base.
  - Verificación con la API: `201` con `Location` al crear un curso (y `GET` de ese `Location`), `403` de un estudiante en `/administradores` y `429` con `Retry-After` en el sexto olvido de contraseña.
- 2026-11-26: T5 completada, sin cambio de alcance. Detalles que la spec no fijaba:
  - Los `Listar*Request` cambian solo el prefijo: `ListEstudiantesGrupoRequest`, `ListGruposCursoRequest`, `ListGruposProfesorRequest`, `ListOfertaRequest`, etc.
  - Las clases de prueba de los validadores siguen el nombre de su request.
  - OpenAPI: 32 esquemas renombrados (73 antes y después, 55 paths). Con esos renombres aplicados a la línea base, el documento es idéntico: sin cambios en paths, propiedades, enums ni códigos de respuesta.
  - Verificación con la API: registro público `201` (y `400` con el campo `Confirmacion`), matrícula que llega al servicio (`404` del grupo, `400` con los dos campos vacíos) y cambio de contraseña `200` con login posterior. El estudiante temporal se borró.
- 2026-11-26: T6 completada, sin cambio de alcance. Detalles que la spec no fijaba:
  - `Cors:OrigenesPermitidos` pasa a `Cors:AllowedOrigins`. No estaba en la tabla de §2.7, pero la regla lo cubre (clave de configuración) y no es un secreto.
  - Las clases de `Seed/TestData` también llevan en inglés sus métodos, constantes y propiedades técnicas (`TimeSlot.All`, `GrupoSeed.IsFull`, `SeededData.Planners`, `HorarioPlanner.TryAssignGrupo`, `TestDates.Compute`/`PeriodoNombre`...). Quedan en español las propiedades de dominio (`Cursos`, `Periodo`, `InicioClases`...) y los métodos de `PersonGenerator` que generan un dato (`Nombre()`, `Cedula()`...). `PlanificadorHorariosTests` pasa a `HorarioPlannerTests`.
  - Los mensajes de log mantienen sus placeholders en español (`{Semilla}`, `{Lleno}`).
  - Las claves de user-secrets del entorno de desarrollo se renombraron con `dotnet user-secrets`, sin mostrar sus valores. El README explica el paso manual.
  - Verificado: sin `Jwt:Key` la API no arranca y el error la nombra. Valores inválidos de `Institute:TimeZone`, `ErrorLog:RetentionDays` y `TestData:Cursos` fallan con la clave nueva. Con las claves renombradas arranca, la siembra lee `TestData:Enabled` (BD con datos: no siembra), las cuentas de prueba entran con `TestData:Password`, la depuración usa `ErrorLog:RetentionDays` = 30 y el correo sale con `Smtp:SenderEmail`.
  - BD recreada (`database drop` + `update`, con la aprobación del usuario): la siembra completa crea el administrador inicial, 8 cursos, 5 aulas, 3 periodos, 6 profesores, 30 estudiantes, 15 grupos en todos sus estados y 74 matrículas con cambio de grupo; el admin y las cuentas de prueba entran con `TestData:Password`.
- 2026-11-26: T7 completada: la corrección queda **completada**, con los 7 criterios de éxito cumplidos.
  - `docs/arquitectura.md`, `AGENTS.md` y los comentarios de `Matricula.Api.http` usan los nombres y claves nuevos. El mapa suma la decisión 15 (regla de nombres) y la nota de que los documentos históricos conservan los nombres viejos. `SPEC-sesiones` y sus tareas ya usaban los nombres nuevos.
  - Búsqueda de los nombres viejos en `src/`, `tests/`, `appsettings*.json`, `secrets.example.json`, `AGENTS.md`, `docs/arquitectura.md` y `README.md`: solo quedan palabras en prosa de comentarios y el aviso del README, que nombra las claves viejas a propósito para el paso manual.
  - `git diff main` de los strings: solo cambian las claves de configuración, los nombres internos de las políticas de rate limit y los nombres de los métodos llamados; los mensajes son los mismos.
  - OpenAPI final: solo los 32 esquemas de §2.6. Migración de prueba vacía. 1104 pruebas en verde.
  - Verificación manual: login, olvido con el correo en smtp4dev, matricular (`201`) y cambiar de grupo (`201`, matrícula 71 → 76), auditoría con las entidades `Matricula` y `Grupo` como antes, y un `500` forzado (columna `aula.ubicacion` renombrada y restaurada) que aparece en `GET /errores` con su `traceId`.
- 2026-11-26: la corrección [COR-04](../correcciones/COR-04-nombres-pruebas.md) reemplaza §2.8 (métodos) y el supuesto 4: los métodos de prueba pasan a inglés (`Rotate_AlreadyRotatedToken_Throws`) y su descripción en español va en el `DisplayName`. §8.1 de la arquitectura ya no remite al SPA.

## Objetivo

Hoy §8 de la arquitectura pide los nombres de dominio en español y solo los sufijos técnicos en inglés (`Service`, `Controller`, `Request`). En la práctica eso tradujo también los conceptos técnicos: `GeneradorToken`, `HasherContrasenas`, `LimitesPeticiones`, `EnvioCorreos`, `Seguridad/`, `ListarAsync`, `ActualizarDatos()`. El SPA ya adoptó una regla más clara (su `SPEC-nombres`): **el dominio del instituto en español y todo lo demás en inglés**. Esta corrección aplica la misma regla en la API, para que los dos proyectos hablen igual.

**Regla nueva** (la misma del SPA, adaptada a .NET):
- **En español:**
  - Las entidades y conceptos del instituto: `Curso`, `Matricula`, `Periodo`, `Estudiante`, `Grupo`, `Aula`, `Horario`, `Rol`, `Usuario`, `Cuenta`, `RegistroAuditoria`, `RegistroError`…, también dentro de un nombre técnico (`CursoService`, `GrupoDetailResponse`, `ChangeGrupoAsync`).
  - Las propiedades de las entidades y de los DTO, es decir, las columnas de la BD y el JSON de la API (`Nombre`, `FechaMatricula`, `debeCambiarContrasena`, `incluirInactivos`).
  - Los enums de dominio y sus valores (`EstadoGrupo.EnCurso`, `MotivoRetiro.CambioGrupo`).
  - Las rutas y sus parámetros (`/api/v1/matriculas/{id}/cambio-grupo`, `?periodoId=`), y por eso el nombre de los controllers, del que sale la ruta (`[Route("api/v1/[controller]")]`).
  - Las carpetas de funcionalidad de Application (`Estudiantes/`, `Matriculas/`, `Auditoria/`…), los ids de módulo y los códigos `RF`/`RN`.
  - Los mensajes al usuario, los mensajes de log, los comentarios, las descripciones de las pruebas y los documentos.
- **En inglés:** todo lo demás: clases e interfaces técnicas, métodos, constantes, carpetas técnicas, opciones de configuración y sus claves, y las piezas de ASP.NET y EF (handlers, filtros, transformers, interceptores, extensiones).
- **Combinados:** primero el nombre de dominio, en español, y después la parte técnica, en inglés: `CursoService`, `GrupoDetailResponse`, `EstudianteCreatedResponse`, `GruposGuards`, `HorarioRules`.
- **Verbos en inglés**, también en las acciones propias del negocio: `List`, `Get`, `Create`, `Update`, `Delete`, `Deactivate`, `Reactivate`, `Enroll`, `Withdraw`, `ChangeGrupo`.
- **Si no está claro qué nombre o qué idioma le toca a algo, se pregunta al usuario.**

**Usuario:** quien construye y revisa los módulos siguientes (empezando por `sesiones`) y el SPA, que consume el OpenAPI.

### Alcance

1. **Documentos que fijan la regla** (para los desarrollos futuros):
   - `docs/arquitectura.md`: reescribir §8 con la regla, ejemplos y la tabla de piezas; cambiar los nombres en §1 a §7 y §9 (por ejemplo `IFechaLocal.Hoy()`, `EnvioCorreos`, `IRegistroErrores`, `OpcionesJson`, las claves de configuración).
   - `AGENTS.md`: la estructura de carpetas, la regla con sus ejemplos, los nombres que cita en *Siempre* y *Nunca* (`IFechaLocal.Hoy()`, `Instituto:ZonaHoraria`, `DepuracionErrores`, `IRegistroErrores`, `EnvioCorreos`, `DatosPrueba:*`) y, en *Preguntar antes*, "el nombre o el idioma de algo, cuando la regla no lo deja claro".
   - `specs/mapa-modulos.md`: una decisión nueva y el estado de esta corrección.
   - `README.md` (tabla de claves y ejemplos de configuración) y `secrets.example.json`.
2. **El código de `src/` y `tests/`**, según las tablas de abajo: carpetas, archivos, tipos, miembros públicos y los internos, que el plan lista por archivo.
3. **La configuración:** `appsettings*.json` y las claves de las opciones (§2.7).

### Lo que no cambia
- **El comportamiento.** Ni una regla, un mensaje, un código de error ni un dato de una respuesta cambian: es un renombre puro.
- **La base de datos.** Las tablas tienen su `ToTable(...)` explícito y las columnas salen de las propiedades de las entidades, que no cambian. **No hay migración.** Las propiedades de `Usuario` que ya mezclan idiomas (`TempPasswordHash`, `TempPasswordExpira`, `TempPasswordMotivo`) se quedan como están, para no cambiar columnas (supuesto 3).
- **Las migraciones existentes** y su `ModelSnapshot`, salvo lo que EF regenere por sí solo. Son historial.
- **El JSON y las rutas.** Las propiedades de los DTO, los valores de los enums, las rutas, los parámetros y los claims del token (`sub`, `email`, `role`, `security_stamp`, `must_change_password`, `estudiante_id`, `profesor_id`) no cambian.
- **Los valores guardados en la auditoría.** `Entidad` es el nombre de la clase de la entidad, y las entidades no cambian; `Cambios` usa los nombres de sus propiedades, que tampoco cambian.
- **Los documentos históricos:** las specs y los `tasks/*` de los módulos cerrados y las correcciones `COR-01` a `COR-03`. Cuentan lo que se hizo con los nombres de entonces; el mapa anota el cambio (supuesto 1).

## Tablas de nombres

Aquí van los nombres que ve el resto del código o que llegan fuera de la API. Los internos (miembros privados, variables locales, métodos `private`) siguen la misma regla, y el plan los lista por archivo.

### 2.1 Carpetas

| Hoy | Nuevo |
|---|---|
| `Domain/Reglas/` | `Domain/Rules/` |
| `Application/Common/Paginacion/` | `Application/Common/Pagination/` |
| `Application/Common/Validacion/` | `Application/Common/Validation/` |
| `Application/Correos/` | `Application/Email/` |
| `Application/Grupos/Consultas/` | `Application/Grupos/Queries/` |
| `Infrastructure/Persistence/Auditoria/` | `Infrastructure/Persistence/Audit/` |
| `Infrastructure/Persistence/Errores/` | `Infrastructure/Persistence/ErrorLog/` |
| `Infrastructure/Seed/DatosPrueba/` | `Infrastructure/Seed/TestData/` |
| `Infrastructure/Seguridad/`, `Api/Seguridad/` | `Infrastructure/Security/`, `Api/Security/` |
| `Infrastructure/Tiempo/` | `Infrastructure/Time/` |
| Sin cambio | Las carpetas de funcionalidad de Application (`Administradores/`, `Auditoria/`, `Aulas/`, `Auth/`, `Cuentas/`, `Cursos/`, `Errores/`, `Estudiantes/`, `Grupos/`, `Matriculas/`, `Periodos/`, `Profesores/`), `Domain/Entities`, `Domain/Enums`, `Abstractions/`, `Common/`, `Controllers/`, `Persistence/` |

### 2.2 Domain

| Hoy | Nuevo |
|---|---|
| `EntidadBase` | Sin cambio (es la base de las entidades de dominio) |
| `Reglas/ReglasHorario`, `ReglasPeriodo`, `ReglasAuditoria`, `ReglasErrores` | `Rules/HorarioRules`, `PeriodoRules`, `AuditRules`, `ErrorLogRules` |
| `Reglas/PoliticaContrasena`, `PropiedadAuditada`, `FechaInstituto` | `Rules/PasswordPolicy`, `AuditedProperty`, `LocalDateRules` |
| `Grupo`: `ActualizarDatos`, `AgregarHorario`, `QuitarHorario`, `CambiarEstado`, `OcuparCupo`, `LiberarCupo`, `RequiereAula`, `ValidarMovimiento`, `NormalizarNrc` | `Update`, `AddHorario`, `RemoveHorario`, `ChangeEstado`, `TakeCupo`, `ReleaseCupo`, `RequiresAula`, `EnsureAllowsMovimiento`, `NormalizeNrc` |
| `Horario.SeTraslapaCon` | `OverlapsWith` |
| `Matricula.Retirar` | `Withdraw` |
| `Periodo`: `EnRangoMatricula`, `PermiteRetiro`, `NormalizarNombre` | `IsInMatriculaRange`, `AllowsRetiro`, `NormalizeNombre` |
| `Curso`, `Aula`: `ActualizarDatos`, `Desactivar`, `Reactivar`, `NormalizarCodigo` | `Update`, `Deactivate`, `Reactivate`, `NormalizeCodigo` |
| `Estudiante`, `Profesor`, `Administrador`: `ActualizarDatos`, `ActualizarContacto` | `Update`, `UpdateContacto` |
| `Usuario`: `AsignarTemporal`, `TemporalVigente`, `EstablecerContrasena`, `ActualizarHashContrasena`, `RegistrarIngresoConDefinitiva`, `RegistrarIngresoConTemporal`, `CambiarCorreo`, `Desactivar`, `Reactivar`, `CerrarSesiones`, `NormalizarCorreo` | `SetTemporaryPassword`, `HasValidTemporaryPassword`, `SetPassword`, `RehashPassword`, `RecordLogin`, `RecordTemporaryPasswordLogin`, `ChangeCorreo`, `Deactivate`, `Reactivate`, `EndSessions`, `NormalizeCorreo` |
| `Usuario.VigenciaTemporal`, `VigenciaTemporalOlvido` | `TemporaryPasswordLifetime`, `ForgotPasswordLifetime` |
| `BusinessRuleException.Datos` (y el parámetro `datos`) | `Extensions`. `Data` choca con `Exception.Data` |
| Constantes `Mensaje*` de las reglas (`MensajeHorasValidas`…) y los `Max*` de `RegistroError` | `*Message` (`ValidHoursMessage`…); los `Max*` sin cambio |

### 2.3 Application

| Hoy | Nuevo |
|---|---|
| `IGeneradorToken.Generar`, `DatosToken`, `TokenGenerado` | `ITokenGenerator.Generate`, `TokenData`, `GeneratedToken` |
| `IGeneradorContrasenas.Generar` | `IPasswordGenerator.Generate` |
| `IHasherContrasenas` (`Hashear`, `Verificar`), `ResultadoVerificacion` (`Incorrecta`, `Correcta`, `CorrectaRequiereRehash`) | `IPasswordHashService` (`Hash`, `Verify`), `PasswordCheckResult` (`Failed`, `Success`, `SuccessRehashNeeded`). `IPasswordHasher<T>` y `PasswordVerificationResult` ya existen en Identity |
| `IFechaLocal` (`Hoy`, `RangoUtc`) | `ILocalClock` (`Today`, `ToUtcRange`) |
| `IEmailSender.EnviarAsync(destinatario, asunto, cuerpoHtml)` | `SendAsync(to, subject, htmlBody)` |
| `IRegistroErrores.RegistrarAsync`, `ErrorInesperado` | `IErrorRecorder.RecordAsync`, `UnexpectedError` |
| `ICurrentUser.EstaAutenticado` | `IsAuthenticated`. `UsuarioId`, `Rol`, `EstudianteId`, `ProfesorId` y `DebeCambiarContrasena` no cambian (datos) |
| `IAppDbContext.RegistrosAuditoria`, `RegistrosError` y los `DbSet` | Sin cambio (colecciones de entidades) |
| `Correos/EnvioCorreos` (`EnviarTemporalAsync`, `EnviarAvisoCambioContrasenaAsync`) | `Email/EmailDispatcher` (`SendTemporaryPasswordAsync`, `SendPasswordChangedAsync`) |
| `Correos/PlantillasCorreo` (`Temporal`, `AvisoCambioContrasena`), `CorreoPlantilla` | `Email/EmailTemplates` (`TemporaryPassword`, `PasswordChanged`), `EmailTemplate` |
| `Common/Paginacion/PaginacionRequest`, `PaginacionExtensions` | `Common/Pagination/PageRequest`, `PaginationExtensions`. `PagedResult` sin cambio |
| `Common/Validacion/DatosPersonalesRules` | `Common/Validation/DatosPersonalesRules` (decisión 4) |
| `*Proyecciones` (`AdministradorProyecciones`…) y sus miembros `AResponse`, `ADetalle` | `*Projections`, `ToResponseExpression`, `ToDetailExpression` |
| `GuardasGrupos`, `GuardasMatriculas` | `GruposGuards`, `MatriculasGuards` |
| `ChoqueHorario`, `IDatosGrupo`, `IDatosPeriodo`, `DatosGrupoValidator`, `DatosPeriodoValidator` | `HorarioConflict`, `IGrupoData`, `IPeriodoData`, `GrupoDataValidator`, `PeriodoDataValidator` |
| `NuevaContrasenaRules` (`NuevaContrasenaValida`, `ConfirmacionDe`) | `NewPasswordRules` (`ValidNewPassword`, `ConfirmationOf`) |
| `Cuentas/CuentaNueva` | `Cuentas/CuentaWithTemporaryPassword` |
| `AuthService.MensajeOlvido` | `ForgotPasswordMessage` |
| `HorarioSemanalService`, `OfertaAcademicaService` y los demás `*Service` | Sin cambio (dominio + sufijo) |

**Métodos de los servicios** (verbo en inglés; la entidad, si aparece, en español):

| Hoy | Nuevo |
|---|---|
| `ListarAsync`, `ObtenerAsync`, `RegistrarAsync`, `CrearAsync`, `ActualizarAsync`, `EliminarAsync`, `DesactivarAsync`, `ReactivarAsync` | `ListAsync`, `GetAsync`, `CreateAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `DeactivateAsync`, `ReactivateAsync` |
| `EstudianteService.RegistrarPublicoAsync`, `ActualizarContactoAsync` | `SignUpAsync`, `UpdateContactoAsync` |
| `GrupoService`: `AgregarHorarioAsync`, `QuitarHorarioAsync`, `CambiarEstadoAsync`, `ListarDeProfesorAsync`, `ListarPorCursoAsync`, `ListarMisGruposAsync` | `AddHorarioAsync`, `RemoveHorarioAsync`, `ChangeEstadoAsync`, `ListByProfesorAsync`, `ListByCursoAsync`, `ListMineAsync` |
| `MatriculaService`: `MatricularAsync`, `MatricularPropioAsync`, `RetirarAsync`, `CambiarGrupoAsync`, `ListarDeEstudianteAsync`, `ListarEstudiantesGrupoAsync`, `ListarMiasAsync` | `EnrollAsync`, `EnrollMeAsync`, `WithdrawAsync`, `ChangeGrupoAsync`, `ListByEstudianteAsync`, `ListEstudiantesAsync`, `ListMineAsync` |
| `HorarioSemanalService`: `HorarioEstudianteAsync`, `HorarioProfesorAsync`, `MiHorarioAsync`, `OcupacionAulaAsync` | `GetEstudianteHorarioAsync`, `GetProfesorHorarioAsync`, `GetMyHorarioAsync`, `GetAulaOcupacionAsync` |
| `PeriodoService.ObtenerVigenteAsync`, `ObtenerIdVigenteAsync` | `GetVigenteAsync`, `GetVigenteIdAsync` |
| `AuthService`: `OlvidoContrasenaAsync`, `CambioObligatorioAsync`, `CambiarContrasenaAsync`, `ObtenerPerfilAsync`, `SesionValidaAsync`. `LoginAsync` y `LogoutAsync` sin cambio | `ForgotPasswordAsync`, `RequiredPasswordChangeAsync`, `ChangePasswordAsync`, `GetProfileAsync`, `IsSessionValidAsync` |
| `CuentaService`: `CrearCuentaPublicaAsync`, `CrearCuentaConTemporalAsync`, `CambiarCorreoAsync`, `EnviarTemporalAsync`; `UsuarioService.ReenviarTemporalAsync` | `CreatePublicAsync`, `CreateWithTemporaryPasswordAsync`, `ChangeCorreoAsync`, `SendTemporaryPasswordAsync`; `ResendTemporaryPasswordAsync` |
| Guardas: `ValidarSinGruposActivosAsync`, `ValidarCursoNoRepetidoAsync`, `ValidarSinChoquesAsync`, `ValidarBloquesSinChoquesAsync`, `BuscarChoquesAsync` | `EnsureNoActiveGruposAsync`, `EnsureCursoNotRepeatedAsync`, `EnsureNoConflictsAsync`, `EnsureBloquesWithoutConflictsAsync`, `FindConflictsAsync` |

### 2.4 Infrastructure

| Hoy | Nuevo |
|---|---|
| `Security/GeneradorContrasenas`, `HasherContrasenas` | `Security/PasswordGenerator`, `PasswordHashService` |
| `Time/FechaLocal`, `InstitutoOptions` (`TieneZonaValida`) | `Time/LocalClock`, `InstituteOptions` (`HasValidTimeZone`) |
| `Persistence/Interceptors/FechasInterceptor` | `TimestampsInterceptor` |
| `Persistence/Audit/CapturaAuditoria` (`CapturarAsync`, `NombreEntidad`, `Valores`), `CambioCapturado`, `CambiosPendientes` (`Completar`, `Ninguno`), `AutorCambio` (`Sistema`) | `AuditCapture` (`CaptureAsync`, `EntityName`, `Values`), `CapturedChange`, `PendingChanges` (`Complete`, `None`), `ChangeAuthor` (`System`) |
| `Persistence/ErrorLog/RegistroErroresEnBd`, `DepuracionErrores`, `ErroresOptions` | `DbErrorRecorder`, `ErrorLogPurgeService`, `ErrorLogOptions` |
| `Seed/AdminSeedService.EjecutarAsync`, `SeedExtensions` (`SembrarAdministradorInicialAsync`, `SembrarDatosPruebaAsync`) | `RunAsync`, `SeedInitialAdministradorAsync`, `SeedTestDataAsync` |
| `Seed/TestData/DatosPruebaSeedService`, `DatosPruebaOptions`, `DatosSembrados`, `CatalogosPrueba`, `FechasPrueba`, `PeriodoPrueba`, `PeriodosPrueba`, `GeneradorPersonas`, `Franja`, `AsignacionGrupo`, `GrupoSembrado`, `MatriculaSembrada`, `PlanificadorHorarios`, `SembradorGrupos`, `SembradorMatriculas` | `TestDataSeedService`, `TestDataOptions`, `SeededData`, `SampleCatalogs`, `TestDates`, `PeriodoSample`, `PeriodoSamples`, `PersonGenerator`, `TimeSlot`, `GrupoAssignment`, `GrupoSeed`, `MatriculaSeed`, `HorarioPlanner`, `GruposSeeder`, `MatriculasSeeder` |
| `Email/SmtpEmailSender`, `SmtpOptions`, `AppDbContext`, `*Configuration`, `DependencyInjection` | Sin cambio (salvo las propiedades de las opciones, §2.7) |
| Métodos `Log*` de `[LoggerMessage]` (`LogFalloEnvio`, `LogDepuracion`…) | En inglés (`LogSendFailed`, `LogPurge`…). Sus mensajes siguen en español |

### 2.5 Api

| Hoy | Nuevo |
|---|---|
| `Security/ClaimsToken` | `TokenClaims`. Sus constantes de dato (`UsuarioId`, `Correo`, `Rol`, `DebeCambiarContrasena`, `EstudianteId`, `ProfesorId`) y los valores de los claims no cambian |
| `Security/GeneradorToken` (`CrearClave`) | `JwtTokenGenerator` (`CreateSigningKey`) |
| `Security/LimitesPeticiones` (`AddLimitesPeticiones`; políticas `Login`, `OlvidoContrasena`, `RegistroEstudiante`) | `RateLimitPolicies` (`AddRateLimitPolicies`; `Login`, `ForgotPassword`, `SignUp`, con valores `"login"`, `"forgot-password"`, `"sign-up"`) |
| `Security/Politicas` (`AddPoliticas`; `AdministradorOEstudiante`, `AdministradorOProfesor`, `ProfesorOEstudiante`) | `Policies` (`AddPolicies`; `AdministradorOrEstudiante`, `AdministradorOrProfesor`, `ProfesorOrEstudiante`). `Administrador`, `Profesor`, `Estudiante`, `PasswordChangePending` sin cambio |
| `Security/SeguridadExtensions.AddSeguridad`, `ValidacionSecurityStamp`, `ResultadoAutorizacionHandler`, `SinCambioPendienteRequirement` | `SecurityExtensions.AddSecurity`, `SecurityStampValidation`, `AuthorizationResultHandler`, `NoPasswordChangePendingRequirement` |
| `Common/OpcionesJson.Configurar`, `RespuestaModeloInvalido.Crear`, `EnumSinNullSchemaTransformer` | `JsonSettings.Configure` (`JsonOptions` choca con el de ASP.NET), `InvalidModelStateResponse.Create`, `NonNullableEnumSchemaTransformer` |
| Acciones de los controllers: `Listar`, `Obtener`, `Registrar`, `Crear`, `Actualizar`, `Eliminar`, `Desactivar`, `Reactivar`, `Matricular`, `Retirar`, `CambiarGrupo`, `CambiarEstado`, `AgregarHorario`, `QuitarHorario`, `ListarGrupos`, `ObtenerVigente`, `RegistroPublico`, `OlvidoContrasena`, `CambioObligatorio`, `CambiarContrasena`, `ReenviarTemporal`, `Ocupacion`, `Horario`, `Estudiantes`, `Matriculas` | Los verbos de §2.3: `List`, `Get`, `Create`, `Update`, `Delete`, `Deactivate`, `Reactivate`, `Enroll`, `Withdraw`, `ChangeGrupo`, `ChangeEstado`, `AddHorario`, `RemoveHorario`, `ListGrupos`, `GetVigente`, `SignUp`, `ForgotPassword`, `RequiredPasswordChange`, `ChangePassword`, `ResendTemporaryPassword`, `GetOcupacion`, `GetHorario`, `ListEstudiantes`, `ListMatriculas`. No cambian las rutas: todas tienen plantilla explícita |
| `MeController`: `Perfil`, `ActualizarDatosContacto`, `Matricularme`, `MisMatriculas`, `MiHorario`, `MisGrupos` | `GetProfile`, `UpdateContacto`, `EnrollMe`, `ListMyMatriculas`, `GetMyHorario`, `ListMyGrupos` |
| Nombres de los controllers | Sin cambio: de ellos sale la ruta |

### 2.6 DTO (cambian el contrato del OpenAPI)

El nombre de cada clase es el nombre de su esquema en `openapi/v1.json`, y el SPA lo usa en `schema.d.ts` y `models.ts`. **Las propiedades no cambian**, así que el JSON de las peticiones y respuestas es el mismo.

| Hoy | Nuevo |
|---|---|
| `RegistrarAdministradorRequest`, `RegistrarAulaRequest`, `RegistrarCursoRequest`, `RegistrarEstudianteRequest`, `RegistrarPeriodoRequest`, `RegistrarProfesorRequest`, `CrearGrupoRequest` | `CreateAdministradorRequest`, `CreateAulaRequest`, `CreateCursoRequest`, `CreateEstudianteRequest`, `CreatePeriodoRequest`, `CreateProfesorRequest`, `CreateGrupoRequest` |
| `ActualizarAdministradorRequest`, `ActualizarAulaRequest`, `ActualizarCursoRequest`, `ActualizarEstudianteRequest`, `ActualizarGrupoRequest`, `ActualizarPeriodoRequest`, `ActualizarProfesorRequest`, `ActualizarContactoRequest` | `UpdateAdministradorRequest`, `UpdateAulaRequest`, `UpdateCursoRequest`, `UpdateEstudianteRequest`, `UpdateGrupoRequest`, `UpdatePeriodoRequest`, `UpdateProfesorRequest`, `UpdateContactoRequest` |
| `RegistroPublicoRequest` | `SignUpRequest` |
| `AgregarHorarioRequest`, `CambiarEstadoRequest` | `AddHorarioRequest`, `ChangeEstadoRequest` |
| `MatricularRequest`, `MatricularPropioRequest`, `CambioGrupoRequest` | `EnrollRequest`, `EnrollMeRequest`, `ChangeGrupoRequest` |
| `OlvidoContrasenaRequest`, `CambioObligatorioRequest`, `CambiarContrasenaRequest` | `ForgotPasswordRequest`, `RequiredPasswordChangeRequest`, `ChangePasswordRequest` |
| `AdministradorRegistradoResponse`, `EstudianteRegistradoResponse`, `ProfesorRegistradoResponse` | `AdministradorCreatedResponse`, `EstudianteCreatedResponse`, `ProfesorCreatedResponse` |
| `GrupoDetalleResponse`, `RegistroErrorDetalleResponse` | `GrupoDetailResponse`, `RegistroErrorDetailResponse` |
| `MensajeResponse`, `PerfilResponse`, `ReenvioTemporalResponse` | `MessageResponse`, `ProfileResponse`, `ResendTemporaryPasswordResponse` |
| `Listar*Request` (`ListarCursosRequest`…) | `List*Request` (`ListCursosRequest`…). Van por query: no generan esquema |
| Los validadores `*RequestValidator` | Siguen el nombre de su request |
| `LoginRequest`, `TokenResponse`, `CursoResponse` y demás `<Entidad>Response`, `BloqueHorarioResponse`, `HorarioResponse`, `EstudianteGrupoResponse`, `OfertaAcademicaResponse`, `RegistroAuditoriaResponse`, `RegistroErrorResponse`, `DatosPersonalesResponse`, `PagedResult<T>` | Sin cambio (decisión 4) |

**Efecto en el SPA:** después de esta corrección, el SPA ejecuta `pnpm api:types`. Hoy sus alias solo usan `TokenResponse`, `PerfilResponse`, `DatosPersonalesResponse` y `Rol`, así que solo `PerfilResponse` → `ProfileResponse` cambia en `models.ts`. Es un cambio del SPA y se hace allá.

### 2.7 Configuración (claves de `appsettings` y user-secrets)

Las secciones y propiedades de las opciones son técnicas y pasan a inglés. Los datos de una entidad (`AdminSeed:Nombre`…) y los nombres de entidad dentro de una clave (`TestData:Cursos`) se quedan en español.

| Hoy | Nuevo |
|---|---|
| `Jwt:Clave`, `Jwt:MinutosVigencia` (`LongitudMinimaClave`) | `Jwt:Key`, `Jwt:AccessTokenMinutes` (`MinKeyLength`). `Issuer` y `Audience` sin cambio |
| `Smtp:Usuario`, `Smtp:Contrasena`, `Smtp:Remitente`, `Smtp:NombreRemitente` | `Smtp:User`, `Smtp:Password`, `Smtp:SenderEmail`, `Smtp:SenderName`. `Host`, `Port`, `UseSsl` sin cambio |
| `AdminSeed:ContrasenaTemporal` | `AdminSeed:TemporaryPassword`. `Cedula`, `Nombre`, `Apellidos`, `Telefono`, `Correo` sin cambio |
| `Instituto:ZonaHoraria` | `Institute:TimeZone` |
| `Errores:DiasRetencion` | `ErrorLog:RetentionDays` |
| `DatosPrueba:Habilitado`, `Contrasena`, `Semilla`, `GruposPorPeriodo`, `MaxMatriculasPorEstudiante` | `TestData:Enabled`, `Password`, `Seed`, `GruposPerPeriodo`, `MaxMatriculasPerEstudiante`. `Cursos`, `Aulas`, `Profesores`, `Estudiantes` sin cambio |

**Paso manual para quien ya tiene user-secrets:** renombrar las claves en su `secrets.json` (`Jwt:Clave` → `Jwt:Key`, `AdminSeed:ContrasenaTemporal` → `AdminSeed:TemporaryPassword`, `Smtp:*`, `DatosPrueba:*` → `TestData:*`). `Jwt:Key` tiene `ValidateOnStart`, así que olvidarla impide arrancar con un mensaje claro. Las de `TestData` se ignorarían sin avisar: el README lo advierte.

### 2.8 Pruebas

| Hoy | Nuevo |
|---|---|
| Clases: `ReglasHorarioTests`, `ReglasPeriodoTests`, `ReglasAuditoriaTests`, `ReglasErroresTests`, `PoliticaContrasenaTests`, `FechaInstitutoTests`, `FechasPruebaTests`, `DatosPruebaOptionsTests`, `PaginacionRequestTests` | `HorarioRulesTests`, `PeriodoRulesTests`, `AuditRulesTests`, `ErrorLogRulesTests`, `PasswordPolicyTests`, `LocalDateRulesTests`, `TestDatesTests`, `TestDataOptionsTests`, `PageRequestTests`. Las de entidades (`GrupoTests`…) sin cambio |
| Métodos `Metodo_Escenario_Resultado` (`SeTraslapaCon_TraslapeParcial_DevuelveTrue`) | Solo cambia el primer segmento, que es el nombre del método probado: `OverlapsWith_TraslapeParcial_DevuelveTrue`. El escenario y el resultado son la descripción y siguen en español (supuesto 4) |

## Tech Stack

Sin cambios. No se agrega ni se quita ninguna dependencia.

## Commands

```bash
dotnet build                                   # warnings = errores
dotnet test tests/Matricula.UnitTests          # 1104 pruebas antes y después
dotnet run --project src/Matricula.Api

# Contrato: guardar el OpenAPI antes de empezar y compararlo al final
curl -s http://localhost:5022/openapi/v1.json -o openapi-antes.json   # en el scratchpad, no en el repo
curl -s http://localhost:5022/openapi/v1.json -o openapi-despues.json
git diff --no-index openapi-antes.json openapi-despues.json

# Sin migración: este comando no debe generar operaciones
dotnet ef migrations add VerificaNombres --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef migrations remove --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
```

## Project Structure

```
src/Matricula.Domain/        Entities/, Enums/, Exceptions/, Common/, Rules/
src/Matricula.Application/   Abstractions/, Common/{Exceptions,Pagination,Validation}/, Email/,
                             <Funcionalidad>/ (Auth/, Cuentas/, Estudiantes/, Grupos/Queries/, Matriculas/…)
src/Matricula.Infrastructure/ Email/, Persistence/{Audit,ErrorLog,Interceptors,Configurations,Migrations}/,
                             Security/, Seed/{TestData}/, Time/
src/Matricula.Api/           Common/, Controllers/, Security/, Services/
tests/Matricula.UnitTests/   Common/, Domain/, Seed/
```

## Code Style

La regla, en un ejemplo:

```csharp
// Application/Matriculas/MatriculaService.cs
/// <summary>
/// RF-53: mueve al estudiante a otro grupo del mismo curso y periodo, en un solo guardado.
/// </summary>
public async Task<MatriculaResponse> ChangeGrupoAsync(int id, ChangeGrupoRequest request, CancellationToken cancellationToken)
{
    var matricula = await FindOwnAsync(id, cancellationToken);          // verbo en inglés
    var destino = await LoadGrupoAsync(request.GrupoId, cancellationToken); // variable local: español o inglés, lo que sea más claro
    await guards.EnsureNoConflictsAsync(matricula.EstudianteId, destino, cancellationToken);

    matricula.Withdraw(localClock.Today(), MotivoRetiro.CambioGrupo); // enum y valor de dominio: español
    destino.TakeCupo();
    // …
}
```

```json
// appsettings.json: claves técnicas en inglés; los datos, en español
"Institute": { "TimeZone": "America/Costa_Rica" },
"AdminSeed": { "Correo": "admin@instituto.test", "Nombre": "John", "Apellidos": "Doe" }
```

Los renombres se hacen con `git mv` (archivos y carpetas) y con el renombre del editor o del compilador, para conservar la historia y no dejar referencias rotas.

## Testing Strategy

- No se escriben pruebas nuevas: un renombre no agrega comportamiento. Las existentes se renombran según §2.8 y **todas siguen pasando**: 1104 pruebas antes y después.
- **Contrato:** el diff del OpenAPI de antes y de después solo muestra los esquemas de §2.6 renombrados y sus `$ref`. Ninguna ruta, propiedad, enum ni código de respuesta cambia.
- **Base de datos:** `migrations add` de prueba no genera operaciones (se descarta con `migrations remove`).
- **Búsqueda final:** un `grep` de los nombres viejos de las tablas no da resultados en `src/`, `tests/`, `appsettings*.json`, `secrets.example.json`, `AGENTS.md`, `docs/arquitectura.md` y `README.md` (fuera de los documentos históricos y del historial del mapa).
- **Manual**, con la API en marcha, smtp4dev y los datos de prueba sembrados con las claves nuevas: login, olvido de contraseña (llega el correo), matricular y cambiar de grupo con `Matricula.Api.http`, la consulta de auditoría (los registros nuevos tienen el mismo `Entidad` que los viejos) y un `500` forzado que queda en `GET /errores`.

## Boundaries

- **Siempre:**
  - Commit después de cada tarea, en la rama `nombres`.
  - Cada tarea deja el proyecto compilando sin warnings y con las 1104 pruebas en verde.
- **Preguntar antes:**
  - Un nombre que no esté en las tablas y que la regla no deje claro.
  - Cualquier cambio que no sea un renombre.
- **Nunca:**
  - Cambiar una propiedad de una entidad o de un DTO, una ruta, un valor de enum, un claim, un mensaje o un código de error.
  - Generar una migración.
  - Reescribir los documentos históricos.

## Success Criteria

1. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` con 1104 pruebas en verde.
2. Los nombres de las tablas 2.1 a 2.8 están aplicados, y la búsqueda de los nombres viejos no da resultados fuera de los documentos históricos y del historial del mapa.
3. El diff del OpenAPI solo contiene los esquemas renombrados de §2.6.
4. No hay cambios en el modelo de EF: `migrations add` de prueba sale vacía.
5. `docs/arquitectura.md` §8 tiene la regla nueva; el resto de la arquitectura, `AGENTS.md`, `README.md`, `secrets.example.json` y `appsettings*.json` usan los nombres y las claves nuevas.
6. `AGENTS.md` pide preguntar el nombre o el idioma de algo cuando la regla no lo deja claro.
7. La verificación manual de *Testing Strategy* se comporta igual que antes del cambio.

## Supuestos

1. **Los documentos históricos no se reescriben.** Las specs, las tareas cerradas y las correcciones `COR-*` cuentan lo que se hizo; el mapa anota cuándo cambiaron los nombres y remite a esta spec.
2. **Las migraciones nuevas siguen con nombre descriptivo en español** (`AgregaRefreshToken`), como los commits: describen un cambio, no son una pieza que el código use.
3. **Las columnas no se tocan.** `TempPasswordExpira` y `TempPasswordMotivo` mezclan idiomas, pero arreglarlo exige una migración y no aporta.
4. **Las pruebas conservan su descripción en español**, como en el SPA. Solo el primer segmento del nombre sigue al método probado.
5. **La versión de la API no sube.** El JSON no cambia; solo los nombres de los esquemas, que el SPA regenera.

## Decisiones

1. **Verbos en inglés**, también en las acciones del negocio (decisión 1 del `SPEC-nombres` del SPA).
2. **Los datos de las entidades se quedan en español**, también dentro de una pieza con nombre en inglés (decisión 3 del SPA).
3. **Ante la duda, preguntar** (decisión 5 del SPA).
4. **"Datos personales" y "bloque de horario" son conceptos del instituto** (2026-11-26, decidido por el usuario): `DatosPersonalesResponse`, `DatosPersonalesRules` y `BloqueHorarioResponse` no cambian.
5. **Auditoría y registro de errores** (2026-11-26, decidido por el usuario): las entidades (`RegistroAuditoria`, `RegistroError`), sus carpetas de funcionalidad, sus servicios y sus controllers se quedan en español; la maquinaria técnica pasa a inglés (`AuditCapture`, `IErrorRecorder`, `ErrorLogPurgeService`, `Persistence/Audit/`, `Persistence/ErrorLog/`, `ErrorLog:RetentionDays`).
6. **Claves de configuración con entidades** (2026-11-26, decidido por el usuario): se aplica la regla combinada, `TestData:GruposPerPeriodo` y `TestData:MaxMatriculasPerEstudiante`.

## Preguntas abiertas

Ninguna.
