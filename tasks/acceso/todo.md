# Tareas: acceso

Módulo `acceso` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-acceso.md`](../../specs/SPEC-acceso.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-10-19)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones de cada flujo se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio de cuentas | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia y migración | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Hash y generador de temporales | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Seed del primer administrador | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. JWT y login | Completada | ✅ | ✅ | ✅ | ✅ |
| T6. Autorización y perfil | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T7. Cambio obligatorio y correos | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T8. Cambiar contraseña y logout | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T9. Olvido de contraseña y rate limiting | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T10. `CuentaService` y registrar administrador | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T11. Listar, consultar y editar administradores | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T12. Reenviar temporal, desactivar y reactivar | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T13. README y cierre | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Verificaciones heredadas

Copiadas de `specs/mapa-modulos.md` ("Verificaciones heredadas"):

| Origen | Verificación | Tarea |
|---|---|---|
| `fundacion` T1 | Un dato duplicado que llegue a la BD (el correo de `usuario`) responde `409` con `code: "RN-13"`, sin el nombre de la restricción | T10 |
| `fundacion` T1 | El `400` de validación (primer DTO con validador: el login) incluye `traceId` | T5 ✅ |

Al completarlas se marcan como cumplidas en el mapa.

## Detalle

### T1. Dominio de cuentas
- Acceptance:
  - Enums `Rol` (`Administrador`, `Profesor`, `Estudiante`), `MotivoTemporal` (`CuentaNueva`, `Olvido`, `ReenvioAdministrador`) y `OrigenRegistro` (`RegistroPublico`, `Administrador`).
  - `Usuario : EntidadBase` con las propiedades de la spec; `Rol` con `init`; setters privados para el estado de seguridad. Fábrica o constructor que normaliza el correo.
  - Métodos: `AsignarTemporal(hash, motivo, ahora)` (72 h o 1 h, reemplaza la anterior), `TemporalVigente(ahora)`, `EstablecerContrasena(hash, ahora)` (anula la temporal, fija `FechaCambioContrasena`, regenera el stamp), `RegistrarIngresoConDefinitiva(ahora)` (anula la temporal, fija `UltimoIngreso`), `RegistrarIngresoConTemporal(ahora)`, `CambiarCorreo(correo)`, `Desactivar()` y `Reactivar()` (regeneran el stamp), `CerrarSesiones()` (regenera el stamp).
  - `PoliticaContrasena.Cumple(string)` y su mensaje (RN-26).
- Verify: `dotnet test tests/Matricula.UnitTests` con `UsuarioTests` y `PoliticaContrasenaTests` (casos de la Testing Strategy de la spec).
- Files: `src/Matricula.Domain/Enums/{Rol,MotivoTemporal,OrigenRegistro}.cs`, `src/Matricula.Domain/Entities/Usuario.cs`, `src/Matricula.Domain/Reglas/PoliticaContrasena.cs`, `tests/Matricula.UnitTests/Domain/{UsuarioTests,PoliticaContrasenaTests}.cs`

### T2. Persistencia y migración
- Acceptance:
  - `Administrador : EntidadBase` (`Cedula`, `Nombre`, `Apellidos`, `Telefono`, `UsuarioId`, navegación a `Usuario`), sin correo ni estado.
  - `UsuarioConfiguration`: longitudes, índice único en `correo`, FK `creado_por_id` a `usuario`.
  - `AdministradorConfiguration`: longitudes, índices únicos en `cedula` y `usuario_id`, FK a `usuario`.
  - `DbSet<Usuario>` y `DbSet<Administrador>` en `IAppDbContext` y `AppDbContext`.
  - Migración `AgregaUsuarioYAdministrador`.
- Verify: `dotnet ef database update`; en `psql`, `\d usuario` y `\d administrador` muestran los índices únicos y FK con `ON DELETE RESTRICT`.
- Files: `src/Matricula.Domain/Entities/Administrador.cs`, `src/Matricula.Infrastructure/Persistence/Configurations/{UsuarioConfiguration,AdministradorConfiguration}.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración

### T3. Hash y generador de temporales
- Acceptance:
  - `IHasherContrasenas` (`Hashear`, `Verificar` → resultado con "requiere rehash") implementada con `PasswordHasher<Usuario>`.
  - `IGeneradorContrasenas.Generar()` con `RandomNumberGenerator`: 12 caracteres, al menos una mayúscula, una minúscula y un dígito; cumple `PoliticaContrasena`.
  - Ambos `Singleton` en `AddInfrastructure`.
- Verify: `dotnet build`. El generador se comprueba en T4 (el seed no lo usa) y en T10 al revisar en smtp4dev varias temporales generadas.
- Files: `src/Matricula.Application/Abstractions/{IHasherContrasenas,IGeneradorContrasenas}.cs`, `src/Matricula.Infrastructure/Seguridad/{HasherContrasenas,GeneradorContrasenas}.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`

### T4. Seed del primer administrador
- Acceptance:
  - `AdminSeedOptions` (`Correo`, `Cedula`, `Nombre`, `Apellidos`, `Telefono`, `ContrasenaTemporal`). Datos genéricos en `appsettings.json` ("John Doe", `admin@instituto.local`, `000000000`, `00000000`); `ContrasenaTemporal` solo en user-secrets.
  - `AdminSeedService`, ejecutado al inicio de `Program` (`SembrarAdministradorInicialAsync`): sin administradores → crea `Usuario` + `Administrador` en un solo `SaveChangesAsync` (temporal de 72 h, `CreadoPorId` nulo). Si el inicial existe sin `PasswordHash` → renueva la temporal. Sin administradores y sin `ContrasenaTemporal` → la API no arranca, con mensaje claro. Tabla inexistente → mensaje que pide aplicar las migraciones.
- Verify:
  - Arrancar con la BD vacía: en `usuario` aparece el administrador con `password_hash` nulo y `temp_password_hash` con valor; en `administrador`, "John Doe".
  - Reiniciar: no se duplica y la temporal se renueva (cambia `temp_password_expira`).
  - Quitar `AdminSeed:ContrasenaTemporal` con la BD vacía: la API no arranca y el mensaje lo explica.
- Files: `src/Matricula.Infrastructure/Seed/{AdminSeedOptions,AdminSeedService}.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`, `src/Matricula.Api/appsettings.json`

### T5. JWT y login
- Acceptance:
  - `JwtOptions` (`Issuer`, `Audience`, `Clave` ≥ 32 caracteres, `MinutosVigencia` = 60) con `ValidateOnStart`; `Issuer` y `Audience` en `appsettings.json`, `Clave` en user-secrets.
  - `IGeneradorToken` (Application) y `GeneradorToken` (Api) con los claims de la spec y nombres compartidos.
  - `AddAuthentication().AddJwtBearer(...)` con `MapInboundClaims = false` y `OnTokenValidated` que compara `security_stamp` y `Activo` con la BD.
  - `LoginRequest` + validador; `AuthService.LoginAsync` con los casos de la spec (definitiva, temporal vigente, temporal vencida, inactiva con contraseña correcta → `403 RN-29`, resto → `401` genérico); rehash si hace falta; `UltimoIngreso`.
  - `AuthController` con `POST /api/v1/auth/login` (`[AllowAnonymous]`).
- Verify:
  - Login del administrador inicial con la temporal → `200` con `debeCambiarContrasena: true`.
  - Contraseña incorrecta y correo inexistente → mismo `401`.
  - ✅ **Heredada:** login con el cuerpo vacío → `400` con `traceId` y mensajes en español.
  - Se verifican más adelante, porque necesitan endpoints posteriores: el rechazo de un token por `SecurityStamp` (T6, con `GET /api/v1/me`) y el `403 RN-29` de una cuenta inactiva (T12, al desactivar).
- Files: `src/Matricula.Api/Seguridad/{JwtOptions,GeneradorToken,ValidacionSecurityStamp}.cs`, `src/Matricula.Application/Auth/{AuthService,LoginRequest,LoginRequestValidator,TokenResponse}.cs`, `src/Matricula.Api/Controllers/AuthController.cs`, `src/Matricula.Api/Program.cs`, `appsettings.json`

### T6. Autorización y perfil
- Acceptance:
  - Política por defecto y de respaldo: autenticado y sin `must_change_password`. Política `PasswordChangePending`: solo autenticado.
  - `IAuthorizationMiddlewareResultHandler` que responde `403` `ProblemDetails` con `code: "RN-24"` cuando falla solo por el claim pendiente.
  - `/health`, OpenAPI y Scalar con `.AllowAnonymous()`.
  - Esquema Bearer en el documento OpenAPI (transformador de documento).
  - `CurrentUser` lee los claims propios y expone `DebeCambiarContrasena`.
  - `MeController` con `GET /api/v1/me`: datos de la cuenta y, si es administrador, sus datos personales.
- Verify:
  - Con el token de la temporal, `GET /api/v1/me` → `403 RN-24`.
  - Sin token → `401` `ProblemDetails`; con un token alterado → `401`.
  - `/health` y Scalar siguen abiertos; Scalar permite pegar el token.
  - Los `curl` de CORS de `fundacion` siguen dando lo esperado.
- Files: `src/Matricula.Api/Seguridad/{Politicas,ResultadoAutorizacionHandler}.cs`, `src/Matricula.Api/Services/CurrentUser.cs`, `src/Matricula.Application/Auth/{AuthService (ObtenerPerfilAsync),PerfilResponse}.cs`, `src/Matricula.Api/Controllers/MeController.cs`, `src/Matricula.Api/Program.cs`

### T7. Cambio obligatorio y correos
- Acceptance:
  - Regla reutilizable de "nueva contraseña + confirmación" (RN-26, confirmación igual) en FluentValidation.
  - `CambioObligatorioRequest` + validador; `AuthService.CambioObligatorioAsync`: exige que haya temporal vigente, la nueva distinta de la temporal y de la definitiva (`409 RN-25`), `EstablecerContrasena`, guarda y devuelve un token nuevo.
  - Plantillas de correo (cuenta nueva, olvido, reenvío, aviso de cambio) y helper de envío que no lanza: registra el fallo sin el contenido y devuelve `bool`.
  - Aviso de cambio (RN-33) después de guardar.
  - `POST /api/v1/auth/cambio-obligatorio` con `PasswordChangePending`; `403` si el token no tiene el claim.
- Verify:
  - Confirmación distinta → `400`; nueva igual a la temporal → `409 RN-25`.
  - Cambio válido → `200` con token nuevo sin el claim; aviso en smtp4dev; el token anterior → `401`; `GET /api/v1/me` con el nuevo → `200`.
  - Login con la nueva contraseña → `200` sin cambio pendiente; con la temporal → `401`.
- Files: `src/Matricula.Application/Auth/{CambioObligatorioRequest,CambioObligatorioRequestValidator,NuevaContrasenaRules}.cs`, `src/Matricula.Application/Correos/{PlantillasCorreo,EnvioCorreos}.cs`, `AuthService.cs`, `AuthController.cs`

### T8. Cambiar contraseña y logout
- Acceptance:
  - `CambiarContrasenaRequest` + validador; `AuthService.CambiarContrasenaAsync`: actual incorrecta → `409` con `code: "RF-09"`; nueva igual a la actual → `409 RN-25`; guarda, envía el aviso y devuelve un token nuevo.
  - `AuthService.LogoutAsync`: `CerrarSesiones()` y guarda.
  - `POST /api/v1/auth/cambiar-contrasena` (política por defecto) y `POST /api/v1/auth/logout` (`PasswordChangePending`, `204`).
- Verify:
  - Actual incorrecta → `409 RF-09`; cambio válido → `200`, aviso en smtp4dev, token anterior → `401`.
  - Logout → `204`; el token → `401`. Logout con un token de temporal → `204`.
- Files: `src/Matricula.Application/Auth/{CambiarContrasenaRequest,CambiarContrasenaRequestValidator}.cs`, `AuthService.cs`, `AuthController.cs`

### T9. Olvido de contraseña y rate limiting
- Acceptance:
  - `OlvidoContrasenaRequest` + validador (correo con formato válido).
  - `AuthService.OlvidoContrasenaAsync`: cuenta activa → `AsignarTemporal(..., Olvido, ...)`, guarda y envía; inexistente o inactiva → nada. Siempre el mismo `200` con el mensaje de RF-10.
  - `AddRateLimiter` con políticas `login` (10/min por IP) y `olvido` (5 cada 15 min por IP); `OnRejected` → `429` `ProblemDetails`; `[EnableRateLimiting]` en las dos acciones; `UseRateLimiter()` en el pipeline.
- Verify:
  - Correo existente, inexistente e inactivo → respuestas idénticas; solo el existente recibe la temporal (1 h).
  - Login con la definitiva tras el olvido → `200` sin cambio pendiente; la temporal ya no sirve (RF-11). Login con la temporal → cambio obligatorio (RF-07).
  - Temporal vencida (ajustar `temp_password_expira` en la BD) → `401`.
  - 11 logins en un minuto → el último `429`; 6 olvidos → el último `429`.
- Files: `src/Matricula.Application/Auth/{OlvidoContrasenaRequest,OlvidoContrasenaRequestValidator}.cs`, `AuthService.cs`, `AuthController.cs`, `src/Matricula.Api/Program.cs`

### T10. `CuentaService` y registrar administrador
- Acceptance:
  - `CuentaService` (contrato de la spec, punto 10): `CrearCuentaPublica`, `CrearCuentaConTemporal` (devuelve la cuenta y la temporal en claro solo en memoria para el envío), `CambiarCorreoAsync`, `EnviarTemporalAsync`. Validan el correo duplicado (`409 RN-13`) y no guardan.
  - Reglas reutilizables de datos personales (cédula, nombre, apellidos, teléfono) según el supuesto 6 de la spec.
  - `RegistrarAdministradorRequest` + validador; `AdministradorService.RegistrarAsync`: cédula de un administrador activo → `409 RN-13`; de uno inactivo → `409 RN-39` con su `usuarioId`; guarda cuenta y administrador juntos y envía la temporal. Respuesta `AdministradorResponse` + `correoEnviado`.
  - `AdministradoresController` con `POST /api/v1/administradores` (Administrador), `201` + `Location`.
- Verify:
  - Registro válido → `201`; temporal en smtp4dev con el nombre; la respuesta no trae la contraseña; las temporales cumplen RN-26.
  - Correo existente con otra capitalización → `409 RN-13`; cédula repetida → `409 RN-13`.
  - Con smtp4dev apagado → `201` con `correoEnviado: false`.
  - Token de no administrador → `403`.
  - ⬜ **Heredada:** dos registros simultáneos con el mismo correo → uno `201` y otro `409 RN-13` sin el nombre de la restricción (ver el riesgo correspondiente del plan).
- Files: `src/Matricula.Application/Cuentas/CuentaService.cs`, `src/Matricula.Application/Common/Validacion/DatosPersonalesRules.cs`, `src/Matricula.Application/Administradores/{AdministradorService,RegistrarAdministradorRequest,RegistrarAdministradorRequestValidator,AdministradorResponse}.cs`, `src/Matricula.Api/Controllers/AdministradoresController.cs`, `DependencyInjection.cs`

### T11. Listar, consultar y editar administradores
- Acceptance:
  - `GET /api/v1/administradores`: paginado con `PaginacionRequest`, `search` (nombre, apellidos, cédula o correo) e `incluirInactivos` (RN-41); proyección con `Select` y `AsNoTracking`.
  - `GET /api/v1/administradores/{id}` → `200` o `404`.
  - `PUT /api/v1/administradores/{id}` con `ActualizarAdministradorRequest` + validador: cédula de otro → `409 RN-13`; cambio de correo vía `CuentaService.CambiarCorreoAsync`.
- Verify:
  - El listado oculta los inactivos sin el filtro (usar la desactivación directa en la BD o esperar a T12) y los incluye con él.
  - Editar a "John Doe" con datos reales → `200`; `GET /api/v1/me` los muestra.
  - Cambiar el correo → el login funciona con el nuevo y no con el anterior; correo de otra cuenta → `409 RN-13`.
- Files: `src/Matricula.Application/Administradores/{AdministradorService,ActualizarAdministradorRequest,ActualizarAdministradorRequestValidator,ListarAdministradoresRequest}.cs`, `AdministradoresController.cs`

### T12. Reenviar temporal, desactivar y reactivar
- Acceptance:
  - `UsuarioService.ReenviarTemporalAsync`: cuenta inactiva → `409 RN-29`; temporal de 72 h con motivo `ReenvioAdministrador`; guarda y envía; devuelve `correoEnviado`.
  - `UsuarioService.DesactivarAsync`: último administrador activo → `409 RN-30`; `Desactivar()` y guarda. Es el punto donde `grupos` y `matriculas` añadirán RN-37 y RN-36.
  - `UsuarioService.ReactivarAsync`.
  - `UsuariosController`: `POST /api/v1/usuarios/{id}/reenviar-temporal`, `.../desactivar` y `.../reactivar` (Administrador); `404` si no existe.
- Verify:
  - Reenviar → temporal en smtp4dev; la anterior deja de servir (RN-27). A una cuenta inactiva → `409 RN-29`.
  - Desactivar una cuenta con sesión abierta → su token `401`; su login `403 RN-29`; su olvido no envía correo.
  - Desactivar al último administrador activo (también a sí mismo) → `409 RN-30`.
  - Reactivar → vuelve a iniciar sesión.
- Files: `src/Matricula.Application/Cuentas/{UsuarioService,ReenvioTemporalResponse}.cs`, `src/Matricula.Api/Controllers/UsuariosController.cs`, `DependencyInjection.cs`

### T13. README y cierre
- Acceptance:
  - README: secciones `Jwt` (qué es la clave, cómo generarla y guardarla en user-secrets) y `AdminSeed` (datos genéricos, `ContrasenaTemporal` en user-secrets, cómo entrar la primera vez), y el paso de la migración.
  - `Matricula.Api.http` con el recorrido completo de la spec.
  - Recorrido completo de la tabla de verificación manual de la spec; consola revisada sin contraseñas ni temporales.
  - Verificaciones heredadas marcadas como cumplidas en `specs/mapa-modulos.md`.
- Verify: revisión manual del README siguiéndolo desde cero en una BD vacía.
- Files: `README.md`, `src/Matricula.Api/Matricula.Api.http`, `specs/mapa-modulos.md`

## Cierre del módulo
- ⬜ Criterios de éxito de la spec cumplidos.
- ⬜ Verificaciones heredadas de `fundacion` cumplidas y marcadas en el mapa.
- ⬜ Estados actualizados en la spec, el mapa y esta lista.
