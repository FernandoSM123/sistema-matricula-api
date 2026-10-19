# Plan: acceso

Módulo `acceso` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-acceso.md`](../../specs/SPEC-acceso.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-10-19)

## Enfoque

Primero la base que no tiene endpoints (dominio, persistencia, hash y seed), verificada con pruebas unitarias y con la BD. Después **rebanadas verticales**: cada tarea deja un flujo de la spec funcionando de punta a punta (DTO → validador → servicio → controller), verificable a mano con `Matricula.Api.http`, Scalar y smtp4dev. Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

El login llega pronto (paso 5) porque todo lo demás se prueba con un token. `Matricula.Api.http` crece en cada tarea con las peticiones del flujo que se agrega.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio de cuentas:** enums `Rol`, `MotivoTemporal`, `OrigenRegistro`; `Usuario` con sus reglas; `PoliticaContrasena` (RN-26) | — | Solo pruebas unitarias. Sin EF ni hashes en claro |
| 2 | **Persistencia:** entidad `Administrador`, configuraciones EF de ambas, `DbSet`s y migración `AgregaUsuarioYAdministrador` | 1 | Se verifica con `dotnet ef database update` y `psql` |
| 3 | **Hash y generador de temporales:** `IHasherContrasenas` (`PasswordHasher<Usuario>`) e `IGeneradorContrasenas` (`RandomNumberGenerator`) | 1 | Interfaces en Application, implementaciones en Infrastructure |
| 4 | **Seed del primer administrador** (`AdminSeedService`, `AdminSeedOptions`) | 2, 3 | Crea "John Doe"; renueva la temporal mientras no haya definitiva |
| 5 | **JWT + login:** `JwtOptions`, `IGeneradorToken`, validación del `SecurityStamp` en `OnTokenValidated`, `AuthService.LoginAsync`, `AuthController` | 3, 4 | Primer DTO con validador → verificación heredada del `traceId` |
| 6 | **Autorización + perfil:** políticas por defecto, de respaldo y `PasswordChangePending`, `403 RN-24`, `[AllowAnonymous]` en lo público, esquema Bearer en OpenAPI, `CurrentUser`, `GET /api/v1/me` | 5 | A partir de aquí todo endpoint nuevo exige token |
| 7 | **Cambio obligatorio** (RF-07) + plantillas de correo + aviso de cambio (RN-33) | 6 | Introduce el envío "después de guardar, sin revertir si falla" |
| 8 | **Cambiar contraseña** (RF-09) y **logout** (RF-08) | 7 | Reutilizan el validador de la nueva contraseña y el aviso |
| 9 | **Olvido de contraseña** (RF-10) + **rate limiting** en login y olvido | 7 | Respuesta idéntica exista o no el correo |
| 10 | **`CuentaService` + registrar administrador** (RF-04, RN-13, RN-39) | 7 | Contrato para `estudiantes` y `profesores`. Verificación heredada del `409 RN-13` desde la BD |
| 11 | **Listar, consultar y editar administradores** (RF-55) | 10 | El cambio de correo pasa por `CuentaService` |
| 12 | **Reenviar temporal, desactivar y reactivar** (RF-12, RF-14, RN-29, RN-30) | 10 | `UsuarioService` + `UsuariosController` |
| 13 | **README y cierre del módulo** | todos | Secciones `Jwt` y `AdminSeed`, recorrido completo de la tabla de verificación |

**Paralelismo:** 2 y 3 son independientes. 8, 9 y 10 dependen solo de 7 y pueden hacerse en cualquier orden; 11 y 12 dependen de 10. Se hacen en serie, en el orden de la tabla, porque comparten `Program.cs`, `AuthService` y `Matricula.Api.http`.

## Decisiones de implementación

- **`Usuario`:** setters privados para el estado de seguridad y `Rol` con `init` (decisión 8 del mapa). Constructor o fábrica estática que recibe correo, rol y origen; el correo se normaliza en el dominio (`Trim().ToLowerInvariant()`).
- **`SecurityStamp`:** `Guid.NewGuid().ToString()`, regenerado dentro de los métodos de dominio que lo requieren, así ninguna ruta se lo salta.
- **Token:** se emite con `JsonWebTokenHandler` (incluido en JwtBearer). `MapInboundClaims = false` y nombres de claim explícitos, para que `CurrentUser` lea los mismos nombres con los que se emitió.
- **`403 RN-24`:** un `IAuthorizationMiddlewareResultHandler` propio que, si la política falla solo por el claim `must_change_password`, escribe un `ProblemDetails` con `code: "RN-24"`; en los demás casos delega en el manejador por defecto.
- **Correos:** un helper en Application (`EnviarSinFallarAsync`) que envuelve `IEmailSender` y devuelve `bool`; registra el error sin el cuerpo del correo.
- **Validadores compartidos:** reglas de FluentValidation reutilizables para la nueva contraseña (RN-26 + confirmación) y para los datos personales (cédula, nombres, teléfono), que luego usarán `estudiantes` y `profesores`.
- **Rate limiting:** `AddRateLimiter` con dos políticas de ventana fija particionadas por `RemoteIpAddress`, `[EnableRateLimiting]` en las dos acciones, y `OnRejected` que escribe un `ProblemDetails` `429`.
- **Seed:** `IHostedService` que crea un scope, consulta la BD y guarda. Valida `ContrasenaTemporal` solo cuando la necesita (no hay administradores o el inicial nunca definió su contraseña).

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| Una temporal o contraseña termina en un log (por ejemplo, el log de comandos SQL de EF en Development muestra parámetros) | EF no registra los valores de los parámetros si no se activa `EnableSensitiveDataLogging`, que no se activa. Solo se guardan hashes. En la verificación final se revisa la consola |
| `OnTokenValidated` hace una consulta por petición | Es una consulta por clave primaria con proyección de dos columnas; aceptable para este tamaño. Se anota como posible caché futura |
| El seed corre antes de aplicar la migración y la API cae con un error confuso | El seed captura la falta de tabla y lanza un error claro: "Aplique las migraciones (`dotnet ef database update`)". El README lo indica |
| La política de respaldo bloquea `/health`, OpenAPI o Scalar | `.AllowAnonymous()` en sus `Map...`; se verifica en la tarea 6 |
| El orden del pipeline rompe CORS, la autenticación o el rate limiting | `UseCors` → `UseAuthentication` → `UseAuthorization` → `UseRateLimiter`, antes de `MapControllers`. Se verifica con los `curl` de CORS de `fundacion` tras la tarea 6 |
| Los claims se renombran al leer el token (`sub` → `NameIdentifier`) y `CurrentUser` no encuentra el id | `MapInboundClaims = false` y constantes compartidas para los nombres de claim |
| La verificación heredada del `409 RN-13` desde la BD es difícil de disparar a mano, porque el servicio valida antes | Dos peticiones simultáneas con el mismo correo (dos terminales con `curl`). Si la carrera no se reproduce, se comenta temporalmente la validación previa, se verifica y se restaura sin commitear ese cambio |
| El envío de correo falla después de guardar y el administrador no se entera | `correoEnviado: false` en la respuesta y reenvío disponible en la tarea 12 |
| Normalizar el correo en varios lugares produce comparaciones distintas | Un único punto de normalización en el dominio, usado por login, olvido, registro y edición |

## Puntos de verificación

- **Tras 2:** `\d usuario` y `\d administrador` en `psql` muestran los índices únicos y las FK sin `ON DELETE CASCADE`.
- **Tras 4:** al arrancar se crea "John Doe" con `password_hash` vacío y una temporal hasheada.
- **Tras 6:** el ciclo login (temporal) → `403 RN-24` en `/me` funciona; `/health` y Scalar siguen abiertos; CORS sigue funcionando.
- **Tras 9:** los flujos de §3.2 de los requerimientos funcionan con el administrador inicial.
- **Final:** toda la tabla de verificación manual de la spec, incluidas las dos verificaciones heredadas; los criterios de éxito cumplidos; `dotnet build` sin warnings; `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Registro de estudiantes y profesores, sus claims y su perfil → `estudiantes` y `profesores`. Guardas RN-36 y RN-37 al desactivar → `matriculas` y `grupos`. Pruebas de integración → diferidas.
