# Tareas: sesiones

Módulo `sesiones` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-sesiones.md`](../../specs/SPEC-sesiones.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-26)

**Requisito previo:** la corrección `nombres` completada y fusionada en `main`.

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite RF-60 o RN-50 a RN-52. Las peticiones nuevas se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea. Para los vencimientos: `Jwt:AccessTokenMinutes = 1` y `Jwt:RefreshTokenMinutes = 3` en user-secrets.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio: `RefreshToken` | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia y migración `AgregaRefreshToken` | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Emisión en los ingresos | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Renovación `POST /api/v1/auth/refresh` | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. Eliminar a una persona borra sus sesiones | Completada | ✅ | ➖ | ✅ | ✅ |
| T6. Cierre del módulo | Completada | ✅ | ➖ | ✅ | ✅ |

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

### T1. Dominio: `RefreshToken`
- Acceptance:
  - Entidad `RefreshToken` en `Domain/Entities`, sin heredar de `EntidadBase`, con las propiedades de la spec (punto 1), sin setters públicos y con constructor privado para EF.
  - `Issue(usuarioId, tokenHash, securityStamp, now, lifetime)`: familia nueva, `CreatedAt = now`, `ExpiresAt = now + lifetime`. Exige hash y stamp no vacíos y `lifetime > 0`.
  - `Rotate(newTokenHash, now)`: revoca la actual y devuelve una con la misma `FamilyId`, `UsuarioId`, `SecurityStamp` y `ExpiresAt`, y `CreatedAt = now`. `InvalidOperationException` si no está activa.
  - `IsActive(now)`, `IsExpired(now)` (`now >= ExpiresAt`), `Revoke(now)` idempotente.
- Verify: `dotnet test`, con `RefreshTokenTests`:
  - `Issue` asigna familia, fechas y stamp; rechaza hash vacío y `lifetime` cero o negativo.
  - `Rotate` conserva familia, stamp y `ExpiresAt` (RN-50) y deja la anterior revocada.
  - `Rotate` sobre una revocada y sobre una vencida lanza `InvalidOperationException`.
  - `IsActive`/`IsExpired` un tick antes, en y después de `ExpiresAt`.
  - `Revoke` dos veces conserva el primer `RevokedAt`.
- Files: `src/Matricula.Domain/Entities/RefreshToken.cs`, `tests/Matricula.UnitTests/Domain/RefreshTokenTests.cs`.

### T2. Persistencia y migración `AgregaRefreshToken`
- Acceptance:
  - `RefreshTokenConfiguration` según el plan (tabla, longitudes, índice único de `token_hash`, índices de `usuario_id` y `family_id`, `xmin`, FK sin cascada).
  - `DbSet<RefreshToken> RefreshTokens` en `IAppDbContext` y `AppDbContext`.
  - Migración `AgregaRefreshToken` aplicada a la BD de desarrollo.
- Verify:
  - `dotnet ef migrations script` de la migración: solo `CREATE TABLE refresh_token`, sus índices y la FK `ON DELETE RESTRICT`.
  - `\d refresh_token` en `psql` coincide.
  - Un cambio cualquiera (editar un curso) sigue apareciendo en `GET /auditoria`.
- Files: `RefreshTokenConfiguration.cs`, `IAppDbContext.cs`, `AppDbContext.cs`, la migración.

### T3. Emisión en los ingresos
- Acceptance:
  - `ITokenGenerator.GenerateRefreshToken()` y `HashRefreshToken(token)`, `GeneratedRefreshToken`, implementados en `JwtTokenGenerator`.
  - `JwtOptions.AccessTokenMinutes = 15`, `RefreshTokenMinutes = 480`, con sus validaciones al arrancar; `appsettings.json` actualizado.
  - `TokenResponse` con `RefreshToken` y `RefreshExpiraEn` anulables.
  - `AuthService`: el login con la definitiva, el cambio obligatorio y cambiar contraseña emiten el par en el mismo guardado (stamp nuevo después de `SetPassword`); el login con temporal no emite refresh; el login con la definitiva borra los vencidos de la cuenta.
- Verify:
  - `dotnet test` en verde.
  - Arrancar con `RefreshTokenMinutes` menor o igual que `AccessTokenMinutes`: la API no arranca y el mensaje nombra las claves.
  - Login con la definitiva: `refreshToken` y `refreshExpiraEn` = ingreso + 3 min; la fila en `refresh_token` tiene un hash de 64 caracteres, distinto del token devuelto.
  - Login con temporal (`profesor2@prueba.test`): `refreshToken: null` y ninguna fila nueva.
  - Cambio obligatorio y cambiar contraseña devuelven un par nuevo, en una familia nueva, con el stamp nuevo.
  - Vencidas: después de 3 minutos, otro login borra las filas vencidas de esa cuenta y conserva las de otras cuentas.
  - Ninguna fila nueva en `registro_auditoria` por los refresh tokens.
- Files: `ITokenGenerator.cs`, `GeneratedRefreshToken.cs`, `JwtTokenGenerator.cs`, `JwtOptions.cs`, `SecurityExtensions.cs`, `TokenResponse.cs`, `AuthService.cs`, `appsettings.json`.

### T4. Renovación `POST /api/v1/auth/refresh`
- Acceptance:
  - `UnauthorizedException(message, code = null)` y `GlobalExceptionHandler` que agrega `code` cuando viene.
  - `RefreshTokenRequest` y su validador (obligatorio, máximo 100 caracteres).
  - `AuthService.RefreshAsync` con los 5 pasos de la spec, `RevokeFamilyAsync` y el `catch` de concurrencia.
  - `AuthController.Refresh` anónimo, con la política `RateLimitPolicies.Refresh` (30 por minuto por IP) y sus `ProducesResponseType`.
  - Peticiones de login, refresh y reuso en `Matricula.Api.http`.
- Verify (todas con el mismo `401` y `code: "RF-60"` cuando fallan):
  - Refresh válido: `200`, par nuevo, mismo `refreshExpiraEn`; el access nuevo funciona en `GET /me`; la fila anterior tiene `revoked_at`.
  - Reuso del token anterior: `401`; después, el último token de la familia también da `401`.
  - Después de logout, de cambiar la contraseña y de desactivar la cuenta: `401`.
  - Después de `refreshExpiraEn`: `401`.
  - Token inventado y body vacío: `401` y `400`.
  - Dos refresh en paralelo con el mismo token: uno `200` y otro `401`.
  - 31 refresh en un minuto: `429` con `Retry-After`.
  - Los `401` de login con credenciales incorrectas no cambian (sin `code`).
  - OpenAPI: `TokenResponse` con las dos propiedades anulables, `RefreshTokenRequest`, la ruta sin requisito de Bearer.
  - El token no aparece en el log de la consola.
- Files: `UnauthorizedException.cs`, `GlobalExceptionHandler.cs`, `RefreshTokenRequest.cs`, `RefreshTokenRequestValidator.cs`, `AuthService.cs`, `AuthController.cs`, `RateLimitPolicies.cs`, `Matricula.Api.http`.

### T5. Eliminar a una persona borra sus sesiones
- Acceptance:
  - `EstudianteService.DeleteAsync` y `ProfesorService.DeleteAsync` borran los `RefreshToken` de la cuenta en el mismo `SaveChangesAsync` que elimina a la persona y su usuario (RN-35).
- Verify:
  - Registrar un estudiante, iniciar sesión con él y renovar; eliminarlo como administrador: `204`, y sin filas suyas en `refresh_token`.
  - Lo mismo con un profesor sin grupos (tras su cambio obligatorio).
  - La auditoría tiene las eliminaciones del estudiante y su usuario, y ninguna de los refresh tokens.
- Files: `EstudianteService.cs`, `ProfesorService.cs`.

### T6. Cierre del módulo
- Acceptance:
  - `docs/requerimientos.md`: RF-60 en §3.2 y en el diagrama de flujos de acceso, RN-50 a RN-52 en §4.1, la fila "Renovar la sesión" en §2.2, §5.14 *Sesión* y la línea de §7 sobre los hashes.
  - `docs/arquitectura.md` §7: refresh token, rotación, reuso y relación con el `SecurityStamp`; §3: la ruta en la lista de acciones de auth.
  - `README.md`: `Jwt:AccessTokenMinutes` y `Jwt:RefreshTokenMinutes` en la tabla de claves.
  - `SPEC-acceso`: línea en el historial que remite a este módulo. `specs/mapa-modulos.md`: decisión nueva y estado del módulo.
  - La spec con su historial y avance; esta lista con todas las columnas.
- Verify:
  - La tabla de verificación manual completa de la spec, con la BD recreada y los datos de prueba.
  - Los 6 criterios de éxito.
  - Aviso al usuario: el SPA necesita su spec para usar el refresh; mientras tanto sus sesiones duran 15 minutos.
- Files: `docs/requerimientos.md`, `docs/arquitectura.md`, `README.md`, `specs/SPEC-acceso.md`, `specs/mapa-modulos.md`, `specs/SPEC-sesiones.md`, este `todo.md`.
