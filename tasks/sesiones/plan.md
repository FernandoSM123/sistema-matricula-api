# Plan: sesiones

Módulo `sesiones` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-sesiones.md`](../../specs/SPEC-sesiones.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-11-26)

**Requisito previo:** la corrección `nombres` completada y fusionada en `main`. Este plan usa sus nombres (`AuthService.RequiredPasswordChangeAsync`, `ITokenGenerator`, `JwtOptions.AccessTokenMinutes`, `RateLimitPolicies`, `EstudianteService.DeleteAsync`…).

## Enfoque

El mismo orden de los módulos anteriores:
1. **La base sin endpoints:** la entidad con sus reglas puras y pruebas unitarias, y después la tabla.
2. **La emisión:** los ingresos empiezan a entregar refresh tokens. Hasta aquí nada los consume, pero ya se ven en la BD y en la respuesta.
3. **La renovación**, el corazón del módulo, con la rotación, el reuso y la concurrencia.
4. **El borrado con la persona**, que sin este módulo ya sería un `409` en cuanto alguien inicie sesión: va justo después de la emisión en el orden de dependencias, pero se verifica mejor con la renovación lista.
5. **El cierre:** documentos y la tabla de verificación completa.

Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit que cite RF-60 o RN-50 a RN-52. Las peticiones nuevas se agregan a `Matricula.Api.http` en la misma tarea. Para verificar vencimientos se usa `Jwt:AccessTokenMinutes = 1` y `Jwt:RefreshTokenMinutes = 3` en user-secrets.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio:** `RefreshToken` y sus reglas | — | Solo pruebas unitarias |
| 2 | **Persistencia:** configuración, `IAppDbContext.RefreshTokens`, migración `AgregaRefreshToken` | 1 | Sin escrituras todavía |
| 3 | **Emisión:** `ITokenGenerator`, `JwtOptions`, `TokenResponse`, ingresos y limpieza en el login | 2 | RN-50, RN-24 |
| 4 | **Renovación:** `POST /api/v1/auth/refresh`, `UnauthorizedException` con `code`, rate limiting | 3 | RF-60, RN-51, RN-52 |
| 5 | **Eliminación de personas** borra sus sesiones | 3 | RN-35 |
| 6 | **Cierre:** requerimientos, arquitectura, README, mapa, verificación completa | 1–5 | |

## Decisiones de implementación

- **Entidad.** `RefreshToken` con constructor privado para EF y `Issue` como fábrica. `Rotate` revoca la instancia actual y devuelve la siguiente; el servicio la agrega con `db.RefreshTokens.Add`. Las reglas lanzan `InvalidOperationException` (errores de programación, no de negocio: el servicio valida antes).
- **Configuración EF** (`RefreshTokenConfiguration`): `ToTable("refresh_token")`, `TokenHash` `char(64)` o `varchar(64)` con índice único, `SecurityStamp` con la misma longitud que en `usuario`, índices `(usuario_id)` y `(family_id)`, `Version` con `.IsRowVersion()` (Npgsql la mapea a `xmin`), FK a `Usuario` con `Restrict` (ya es global). Sin `HasData`.
- **Token y hash** en `JwtTokenGenerator`: `RandomNumberGenerator.GetBytes(32)` en Base64Url (`Base64Url.EncodeToString`, .NET 9+) y `Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)))`. `GeneratedRefreshToken(string Token, string Hash)` en `Application/Abstractions`.
- **Un solo método de emisión** en `AuthService`, `IssueTokensAsync(Usuario usuario, bool mustChangePassword, RefreshToken? refreshToken, string? refreshTokenValue)`, que arma el `TokenResponse`. Quien llama crea el `RefreshToken` (con `Issue` o `Rotate`) **antes** del `SaveChangesAsync` de su operación, para que todo quede en un guardado:
  - Login con la definitiva: `RecordLogin` + limpieza de vencidos + `Issue`, un guardado.
  - Login con temporal: como hoy, sin refresh.
  - Cambio obligatorio y cambiar contraseña: `SetPassword` (regenera el stamp) y **después** `Issue`, para que el token nuevo lleve el stamp nuevo; un guardado; luego el correo, como hoy.
- **Limpieza:** `db.RefreshTokens.Where(t => t.UsuarioId == id && t.ExpiresAt <= now)` + `RemoveRange`, antes del guardado del login.
- **Reuso:** `RevokeFamilyAsync` carga los tokens activos de la familia, llama a `Revoke` y guarda **antes** de lanzar el `401`, en un guardado propio (no hay otra escritura en esa petición).
- **Concurrencia:** el `catch (DbUpdateConcurrencyException)` está solo alrededor del guardado de la rotación y responde el `401` de RF-60. No se toca el mapeo genérico de `GlobalExceptionHandler`.
- **`UnauthorizedException(string message, string? code = null)`**: el handler agrega `code` a los `ProblemDetails` del `401` solo cuando viene. Los `401` actuales no cambian.
- **Endpoint:** `AuthController.Refresh`, `[AllowAnonymous]`, `[EnableRateLimiting(RateLimitPolicies.Refresh)]`, `[ProducesResponseType<TokenResponse>(200)]`, `401`, `400`, `429`. La política `Refresh`: ventana fija por IP de 30 por minuto.
- **`JwtOptions`:** `AccessTokenMinutes = 15`, `RefreshTokenMinutes = 480`, y en `SecurityExtensions` las validaciones `> 0` y `RefreshTokenMinutes > AccessTokenMinutes`, con mensajes que nombran las claves. `appsettings.json` pasa a `"AccessTokenMinutes": 15, "RefreshTokenMinutes": 480`.
- **Eliminación de personas:** en `EstudianteService.DeleteAsync` y `ProfesorService.DeleteAsync`, `db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(t => t.UsuarioId == usuarioId))` antes de quitar a la persona y su usuario, en el mismo guardado. EF ordena los `DELETE` por las FK.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| El refresh token aparece en claro en la BD, el log o la auditoría | Solo se guarda el hash; `RefreshToken` no hereda de `EntidadBase`; el request no se registra en logs ni en el registro de errores (RN-47 ya lo prohíbe). En T3 y T4 se revisa la tabla con `psql` y el log |
| Un token emitido con el stamp viejo tras un cambio de contraseña | `Issue` va después de `SetPassword`. En T3 se verifica que el refresh recién emitido por el cambio funciona y que el anterior al cambio da `401` |
| La sesión se extiende al rotar | `Rotate` copia `ExpiresAt`, con prueba unitaria. En T4 se compara `refreshExpiraEn` antes y después |
| El reuso no revoca la familia | Prueba manual en T4: usar el token viejo y luego el último, que también debe dar `401` |
| Dos rotaciones simultáneas emiten dos tokens válidos | `xmin` en la fila. En T4 se lanzan dos refresh en paralelo (`curl ... & curl ...`) y solo uno responde `200` |
| La FK impide eliminar personas que iniciaron sesión | T5 borra las sesiones en el mismo guardado; se verifica eliminando un estudiante que entró y renovó |
| La migración toca otras tablas | Se revisa el script (`migrations script`) en T2 |
| El SPA deja de funcionar con 15 minutos de token mientras no implemente el refresh | El contrato solo agrega propiedades; el SPA sigue funcionando, con sesiones de 15 minutos en lugar de 60. Se avisa en el cierre para coordinar la spec del SPA |

## Puntos de verificación

- **Después de T2:** la tabla existe con sus índices y su FK, sin otros cambios en el esquema.
- **Después de T3:** el login devuelve el refresh token y la BD tiene su hash; el login con temporal no lo devuelve.
- **Después de T4:** toda la tabla de verificación de la spec, salvo la eliminación de personas.
- **Después de T6:** los 6 criterios de éxito de la spec.

## Fuera de este plan

- Los cambios del SPA (guardar el refresh token, renovar ante un `401` con una sola renovación en curso, reintentar la petición, sus documentos). Van en una spec del SPA.
