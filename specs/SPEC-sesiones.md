# Spec: sesiones

Módulo `sesiones` del [mapa de módulos](mapa-modulos.md): renovación de la sesión con refresh token. Depende de `acceso` (login, `SecurityStamp`, emisión del token), de `estudiantes` y `profesores` (su eliminación borra las sesiones, RN-35) y de la corrección [`nombres`](SPEC-nombres.md), que va antes: el código de este módulo usa la regla nueva.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/sesiones/plan.md) | [Aprobadas](../tasks/sesiones/todo.md) | 1/6 tareas |

**Historial:**
- 2026-11-26: v1 en revisión. El refresh token estaba fuera del alcance de `acceso` (`SPEC-acceso`, "Fuera de alcance"). El usuario lo pidió para que el SPA renueve la sesión al vencer el token, y antes de redactar decidió: el refresh token viaja en el body y el SPA lo guarda en `sessionStorage`; el access token dura 15 minutos y la sesión 8 horas desde el ingreso, sin extenderse; al eliminar a un estudiante o profesor se borran sus sesiones en el mismo guardado; las sesiones vencidas se limpian al iniciar sesión; el logout sigue cerrando todas las sesiones de la cuenta.
- 2026-11-26: v1 aprobada por el usuario, que resolvió las dos preguntas abiertas como se proponían (decisiones 1 y 2). Plan y tareas en revisión.
- 2026-11-26: plan y tareas aprobados. Empieza después de cerrar la corrección `nombres`.
- 2026-11-26: T1 completada: entidad `RefreshToken` con sus reglas y `RefreshTokenTests`.

## Objetivo

Hoy el token dura 60 minutos y no se puede renovar: al vencer, el SPA descarta la sesión y manda al usuario al login, aunque esté a mitad de un formulario (RFF-03 del SPA). Con este módulo, el SPA recibe junto al access token un **refresh token** con el que obtiene un access token nuevo sin pedir la contraseña, durante una jornada de trabajo (8 horas). El access token dura menos (15 minutos), así que un token robado sirve por menos tiempo.

Cubre un requerimiento nuevo (RF-60) y tres reglas nuevas (RN-50 a RN-52), que se agregan a `docs/requerimientos.md` (punto 7 del alcance).

**Usuarios:** administradores, profesores y estudiantes con sesión iniciada con su contraseña definitiva. Quien ingresa con una temporal no recibe refresh token (RN-24): su sesión solo sirve para el cambio obligatorio.

### Alcance

1. **Entidad `RefreshToken`** en `Domain/Entities`. Es técnica, no un dato del instituto: nombres en inglés (decisión 1 de esta spec). **No** hereda de `EntidadBase`, así que la auditoría no la captura (RN-46: datos internos de sesión) y `TimestampsInterceptor` no la toca.

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Id` | `long` | Identidad generada por PostgreSQL |
   | `UsuarioId`, `Usuario` | `int`, `Usuario` | FK a `usuario`, `Restrict` como todas, con su navegación |
   | `TokenHash` | `string` (64) | SHA-256 del token en hexadecimal. **Nunca** se guarda el token en claro. Índice único |
   | `FamilyId` | `Guid` | Igual para todos los tokens que salen de un mismo ingreso. Índice |
   | `SecurityStamp` | `string` | Copia del stamp de la cuenta al emitirlo |
   | `CreatedAt` | `DateTimeOffset` | UTC, de `TimeProvider` |
   | `ExpiresAt` | `DateTimeOffset` | Fin de la sesión: ingreso + 8 horas. Al rotar se **copia**, no se extiende (RN-50) |
   | `RevokedAt` | `DateTimeOffset?` | Cuándo se usó (rotación) o se anuló (reuso) |
   | `Version` | `uint` | `xmin`, token de concurrencia (punto 4) |

   Métodos (reglas puras, con pruebas unitarias):
   - `static RefreshToken Issue(int usuarioId, string tokenHash, string securityStamp, DateTimeOffset now, TimeSpan lifetime)`: abre una familia nueva.
   - `RefreshToken Rotate(string newTokenHash, DateTimeOffset now)`: marca este como usado y devuelve el siguiente, con la misma familia, el mismo stamp y el mismo `ExpiresAt`. Lanza `InvalidOperationException` si este no está activo.
   - `bool IsActive(DateTimeOffset now)`: sin `RevokedAt` y `now < ExpiresAt`.
   - `bool IsExpired(DateTimeOffset now)`.
   - `void Revoke(DateTimeOffset now)`: no cambia un `RevokedAt` que ya tenía valor.

   Migración `AgregaRefreshToken`: tabla `refresh_token`, índice único `(token_hash)`, índices `(usuario_id)` y `(family_id)`, FK a `usuario` sin cascada.

2. **Emisión.** `ITokenGenerator` agrega `GeneratedRefreshToken GenerateRefreshToken()`, que devuelve el token (32 bytes de `RandomNumberGenerator` en Base64Url) y su hash, y `string HashRefreshToken(string token)`. `AuthService` emite el par (access + refresh) en un método privado común, con el mismo `SaveChangesAsync` de la operación:

   | Operación | Access token | Refresh token |
   |---|---|---|
   | Login con la contraseña definitiva | Sí | Sí, en una familia nueva |
   | Login con una temporal (RN-24) | Sí, con `must_change_password` | **No** (`refreshToken: null`) |
   | Cambio obligatorio (RF-07) | Sí | Sí, en una familia nueva. Las anteriores ya no sirven porque cambió el stamp |
   | Cambiar contraseña (RF-09) | Sí | Igual que el cambio obligatorio |
   | Renovar (RF-60, punto 3) | Sí | Sí, en la misma familia (rotación) |

   `TokenResponse` agrega dos propiedades: `RefreshToken` (`string?`) y `RefreshExpiraEn` (`DateTimeOffset?`), las dos `null` en el login con temporal. Las demás no cambian.

3. **`POST /api/v1/auth/refresh`** (RF-60; nombre de la ruta en la decisión 2 de esta spec). Anónimo (`[AllowAnonymous]`): el access token puede estar vencido. Body `RefreshTokenRequest { refreshToken }`, validado con FluentValidation (obligatorio, máximo 100 caracteres). Responde `200` con `TokenResponse`.

   `AuthService.RefreshAsync`:
   1. Busca el `RefreshToken` por `HashRefreshToken(request.RefreshToken)`, junto con su `Usuario`. Si no existe → `401`.
   2. Si ya tiene `RevokedAt` → **reuso** (RN-51): anula los tokens activos de esa familia, guarda y responde `401`. Puede ser un robo: el token ya se había usado.
   3. Si venció → `401`.
   4. Si la cuenta está inactiva o su `SecurityStamp` no es el del token → `401` (RN-52). No hace falta escribir nada: esa familia ya no puede renovar.
   5. Rota (`Rotate`), agrega el nuevo, guarda y emite el access token con los mismos claims que el login (`estudiante_id`, `profesor_id`).

   Todos los `401` dan el mismo `ProblemDetails` ("La sesión terminó. Inicie sesión de nuevo.", `code: "RF-60"`), para no revelar por qué falló. Para eso `UnauthorizedException` acepta un `code` opcional, como `ForbiddenException`, y `GlobalExceptionHandler` lo pasa a la respuesta; los `401` actuales siguen sin `code`. No actualiza `UltimoIngreso`: renovar no es un ingreso.

   **Rate limiting:** política nueva `Refresh`, por IP, de 30 por minuto (supuesto 4).

4. **Concurrencia.** Si dos peticiones rotan el mismo token a la vez, `xmin` hace que una falle con `DbUpdateConcurrencyException`. El servicio la captura y responde el `401` de RF-60 (no el `409` genérico). La otra sigue normal. El SPA evita el caso con una sola renovación en curso por pestaña (punto 8).

5. **Fin de las sesiones** (RN-52). Ya funciona por el `SecurityStamp`, sin código nuevo en estos casos:
   - **Logout (RF-08):** regenera el stamp, así que **todas** las sesiones de la cuenta dejan de renovar (decisión del usuario: se mantiene el comportamiento actual).
   - **Cambio de contraseña** (RF-07, RF-09) y **desactivación** (RF-14, RN-29): también regeneran el stamp.
   - **Eliminación de un estudiante o profesor** (RF-19, RF-26, RN-35): `EstudianteService.DeleteAsync` y `ProfesorService.DeleteAsync` borran los `RefreshToken` de la cuenta con `RemoveRange`, en el mismo `SaveChangesAsync` que elimina a la persona y su usuario. Sin cascada.

6. **Limpieza** (supuesto 3). En el login con la contraseña definitiva, el servicio borra con `RemoveRange` los `RefreshToken` vencidos de esa cuenta, en el mismo guardado del ingreso. Los anulados que no han vencido se conservan, porque sirven para detectar el reuso. No hay `BackgroundService` ni `ExecuteDelete`.

7. **Configuración.** `JwtOptions` agrega `RefreshTokenMinutes` (por defecto `480`) y cambia el valor por defecto de `AccessTokenMinutes` a `15`. Los dos se validan al arrancar (mayores que cero, y el refresh mayor que el access). En minutos, para poder probar el vencimiento con un valor pequeño.

8. **Documentos:**
   - `docs/requerimientos.md`: RF-60 en §3.2 (y en el diagrama de flujos de acceso), RN-50 a RN-52 en §4.1, la fila "Renovar la sesión" en la matriz de §2.2, el dato §5.14 *Sesión* y una línea en §7 (los refresh tokens se guardan como hash).
   - `docs/arquitectura.md` §7: el refresh token, la rotación y la relación con el `SecurityStamp`, en lugar de "tokens de vida corta (30–60 min)".
   - `README.md`: `Jwt:AccessTokenMinutes` y `Jwt:RefreshTokenMinutes` en la tabla de claves.
   - `Matricula.Api.http`: login, refresh, reuso y logout.
   - `SPEC-acceso`: una línea en el historial que remite a este módulo. `specs/mapa-modulos.md`: el módulo, su estado y una decisión.

**Reglas nuevas** (para `docs/requerimientos.md`):

| Código | Regla |
|---|---|
| RF-60 | **Renovar la sesión:** mientras la sesión esté vigente, el sistema entrega un token de acceso nuevo sin pedir de nuevo la contraseña. |
| RN-50 | El token de acceso vence a los **15 minutos**. La sesión dura como máximo **8 horas** desde el inicio de sesión; renovarla no la extiende. Después hay que iniciar sesión de nuevo. |
| RN-51 | Cada renovación reemplaza el refresh token por uno nuevo, y el anterior deja de servir. Si alguien usa uno ya reemplazado, el sistema termina esa sesión. |
| RN-52 | Una sesión no se puede renovar si la cuenta se desactivó, cambió su contraseña o cerró sesión, ni si se inició con una contraseña temporal. |

### Fuera de alcance
- **Cerrar una sola sesión.** El logout cierra todas las de la cuenta, como hoy.
- **Ver o cerrar las sesiones abiertas** desde el perfil o desde la administración.
- **Cookies httpOnly.** El refresh token va en el body: una cookie se comparte entre pestañas, rompe el modelo del SPA de "cada pestaña su sesión" y exige `AllowCredentials` y protección CSRF.
- **Un margen de gracia para el reuso.** Si una respuesta de renovación se pierde y el SPA reintenta con el token viejo, la sesión termina (supuesto 2).
- **Los cambios del SPA** (guardar el refresh token, renovar ante un `401` y reintentar la petición). Van en una spec del SPA; este módulo solo fija el contrato (punto 3).

## Tech Stack

Sin cambios ni paquetes nuevos. `RandomNumberGenerator` y `SHA256` son de `System.Security.Cryptography`.

## Commands

```bash
dotnet build
dotnet test tests/Matricula.UnitTests
dotnet ef migrations add AgregaRefreshToken --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api      # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/Entities/RefreshToken.cs
src/Matricula.Application/Abstractions/ITokenGenerator.cs           (+ GenerateRefreshToken, HashRefreshToken)
src/Matricula.Application/Abstractions/GeneratedRefreshToken.cs
src/Matricula.Application/Abstractions/IAppDbContext.cs            (+ DbSet<RefreshToken> RefreshTokens)
src/Matricula.Application/Auth/AuthService.cs                      (emisión, RefreshAsync, limpieza)
src/Matricula.Application/Auth/RefreshTokenRequest.cs (+ Validator)
src/Matricula.Application/Auth/TokenResponse.cs                    (+ RefreshToken, RefreshExpiraEn)
src/Matricula.Application/Estudiantes/EstudianteService.cs          (DeleteAsync borra las sesiones)
src/Matricula.Application/Profesores/ProfesorService.cs             (ídem)
src/Matricula.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs
src/Matricula.Infrastructure/Persistence/Migrations/<fecha>_AgregaRefreshToken.cs
src/Matricula.Api/Security/JwtTokenGenerator.cs, JwtOptions.cs, RateLimitPolicies.cs
src/Matricula.Api/Controllers/AuthController.cs                     (+ Refresh)
src/Matricula.Application/Common/Exceptions/UnauthorizedException.cs (+ code opcional)
src/Matricula.Api/Common/GlobalExceptionHandler.cs                   (pasa el code del 401)
tests/Matricula.UnitTests/Domain/RefreshTokenTests.cs
```

## Code Style

```csharp
// Application/Auth/AuthService.cs
/// <summary>
/// RF-60: entrega un token de acceso nuevo y rota el refresh token (RN-51). Todos los rechazos dan el mismo 401.
/// </summary>
public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);

    var now = timeProvider.GetUtcNow();
    var hash = tokenGenerator.HashRefreshToken(request.RefreshToken);
    var current = await db.RefreshTokens.Include(t => t.Usuario)
        .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
        ?? throw SessionEnded();

    if (current.RevokedAt is not null)
    {
        await RevokeFamilyAsync(current.FamilyId, now, cancellationToken); // RN-51: reuso
        throw SessionEnded();
    }

    var usuario = current.Usuario;
    if (current.IsExpired(now) || !usuario.Activo || usuario.SecurityStamp != current.SecurityStamp)
    {
        throw SessionEnded(); // RN-50, RN-52
    }

    var next = tokenGenerator.GenerateRefreshToken();
    db.RefreshTokens.Add(current.Rotate(next.Hash, now));
    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
        throw SessionEnded(); // otra petición rotó el mismo token
    }

    return await IssueTokensAsync(usuario, next, current.ExpiresAt, cancellationToken);
}

private static UnauthorizedException SessionEnded() =>
    new("La sesión terminó. Inicie sesión de nuevo.", "RF-60");
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests/Domain/RefreshTokenTests.cs`), reglas puras sin BD:
  - `Issue` abre una familia nueva con `ExpiresAt = now + lifetime`.
  - `Rotate` revoca el actual y devuelve uno con la misma familia, el mismo stamp y el **mismo** `ExpiresAt` (RN-50).
  - `Rotate` sobre uno revocado o vencido lanza `InvalidOperationException`.
  - `IsActive` e `IsExpired` en el límite exacto de `ExpiresAt`.
  - `Revoke` no cambia un `RevokedAt` previo.
  - La validación de `JwtOptions` rechaza `RefreshTokenMinutes <= AccessTokenMinutes` (si la validación queda en un método puro; si no, se verifica al arrancar).
- **Manual** con `Matricula.Api.http`, Scalar y smtp4dev, con `Jwt:AccessTokenMinutes = 1` y `Jwt:RefreshTokenMinutes = 3` en user-secrets:

  | Caso | Resultado esperado |
  |---|---|
  | Login con la definitiva | `200` con `refreshToken` y `refreshExpiraEn` = ingreso + 3 min |
  | Login con una temporal | `refreshToken: null` |
  | Refresh con un token válido | `200`, tokens nuevos, mismo `refreshExpiraEn`; el access nuevo funciona en `GET /me` |
  | Refresh con el token anterior (ya rotado) | `401 RF-60`, y el último token de la familia también da `401` (RN-51) |
  | Refresh después de logout, cambio de contraseña o desactivación | `401 RF-60` |
  | Refresh después de `refreshExpiraEn` | `401 RF-60` |
  | Dos refresh simultáneos con el mismo token | Uno `200` y el otro `401` |
  | Refresh token inventado | `401 RF-60` |
  | Más de 30 refresh por minuto desde la misma IP | `429` con `Retry-After` |
  | Login después de que vencieron sesiones | Las vencidas de esa cuenta desaparecen de `refresh_token` |
  | Eliminar un estudiante sin matrículas que ya inició sesión | `204`, y sus filas de `refresh_token` desaparecen (antes de este módulo daría `409 RN-34`) |
  | `GET /auditoria` después de varios refresh | No hay registros de `RefreshToken` |
  | `refresh_token` en la BD | Solo hashes de 64 caracteres; ningún token en claro |
  | OpenAPI | `TokenResponse` con las dos propiedades nuevas, anulables; `RefreshTokenRequest`; la ruta nueva sin el candado de Bearer |

## Boundaries

- **Siempre:**
  - Guardar solo el hash del refresh token; nunca escribir el token en logs, en la auditoría ni en el registro de errores.
  - Responder el mismo `401` en todos los rechazos de RF-60.
  - Escribir con el change tracker y `SaveChangesAsync`, también al borrar sesiones.
  - Commit por tarea, citando RF-60 y RN-50 a RN-52.
- **Preguntar antes:**
  - Cambiar las vigencias, el límite de peticiones o el comportamiento del logout.
  - Cualquier cambio de esquema fuera de la tabla `refresh_token`.
- **Nunca:**
  - Emitir un refresh token en un login con temporal (RN-24).
  - Extender `ExpiresAt` al rotar.
  - Usar `ExecuteDelete` o SQL manual para borrar sesiones, o un `BackgroundService` de limpieza.
  - Poner el refresh token en una cookie o en la URL.
  - Hacer que `RefreshToken` herede de `EntidadBase` (quedaría en la auditoría).

## Success Criteria

1. `dotnet build` sin warnings y las pruebas unitarias en verde, con las nuevas de `RefreshTokenTests`.
2. La migración `AgregaRefreshToken` crea solo la tabla `refresh_token` con sus índices y su FK, sin cascada.
3. Todos los casos de la tabla de verificación manual dan el resultado esperado.
4. Un estudiante o profesor sin datos relacionados se puede eliminar aunque haya iniciado sesión (RN-35).
5. Los refresh no generan registros de auditoría.
6. `docs/requerimientos.md` tiene RF-60 y RN-50 a RN-52; `docs/arquitectura.md` §7, el README, `Matricula.Api.http`, `SPEC-acceso` y el mapa están actualizados.

## Supuestos

1. **La sesión dura 8 horas desde el ingreso y no se extiende con el uso.** Es una jornada de trabajo; después se vuelve a iniciar sesión. Coincide con `sessionStorage`, que se borra al cerrar la pestaña.
2. **Sin margen de gracia para el reuso.** Si la respuesta de una renovación se pierde y el SPA reintenta con el token viejo, la sesión termina y el usuario inicia sesión de nuevo. Es raro y es el lado seguro.
3. **La limpieza ocurre solo en el login, por cuenta.** Una cuenta que no vuelve a ingresar deja sus filas vencidas hasta que se elimine o vuelva a entrar. Son pocas filas, sin datos sensibles (solo hashes).
4. **30 renovaciones por minuto por IP.** Con access tokens de 15 minutos, un usuario renueva unas 4 veces por hora; el límite deja margen para muchos usuarios detrás de la misma IP del instituto.
5. **El access token sigue validando el stamp en cada petición** (`SecurityStampValidation`), así que acortarlo a 15 minutos no cambia cómo se cierra una sesión, solo cuánto dura sin renovar.
6. **El logout no recibe el refresh token.** Si el access token ya venció, el SPA renueva primero (su interceptor lo hace solo ante el `401`) y luego cierra sesión. Si el SPA descarta la sesión sin llamar al logout, el refresh token queda en la BD hasta vencer, pero ya no existe en el navegador.

## Decisiones

Se aplican las decisiones del usuario del 2026-11-26 (historial) y las decisiones 1, 5, 10 y 11 del mapa. La entidad no se audita por RN-46 (datos internos de sesión). Además:

1. **La entidad se llama `RefreshToken`** (2026-11-26, decidido por el usuario), con tabla `refresh_token` y propiedades en inglés: es una pieza técnica, no un dato del instituto. En los requerimientos se describe como "sesión".
2. **La ruta es `POST /api/v1/auth/refresh`** (2026-11-26, decidido por el usuario), junto a `login` y `logout`.

## Preguntas abiertas

Ninguna.
