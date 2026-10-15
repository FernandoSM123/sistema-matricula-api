# Spec: fundacion

Módulo `fundacion` del [mapa de módulos](mapa-modulos.md). Sin dependencias.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/fundacion/plan.md) | [Aprobadas](../tasks/fundacion/todo.md) | 5/6 tareas completadas |

**Historial:**
- 2026-10-15: v1 aprobada.
- 2026-10-15: v2, se difieren las pruebas de integración y el módulo se reduce a errores, CORS, rutas y README.

## Objetivo

Completar la base transversal que todos los módulos usan. Este módulo no implementa RF ni crea entidades: el esquema lo aporta cada módulo (decisión 1 del mapa).

Parte del esqueleto ya existe: capas, `Directory.Build.props`, `GlobalExceptionHandler`, `ValidationFilter`, paginación, `SmtpEmailSender`, `AuditoriaInterceptor`, `TimeProvider`, OpenAPI/Scalar y `/health`. Esta spec cubre lo que falta.

**Usuario:** el equipo de desarrollo (y los agentes) que construirá los módulos siguientes.

### Alcance

1. **Errores como ProblemDetails, completos**
   - Violación de unicidad de PostgreSQL (`DbUpdateException` con `PostgresException.SqlState == "23505"`) → `409` con `code: "RN-13"` y un mensaje legible, sin el nombre de la restricción. Es la red de seguridad: los servicios deben validar antes y lanzar su propia `BusinessRuleException("RN-13", ...)` con un mensaje claro.
   - El `400` de `ValidationFilter` incluye `traceId`, como el resto de respuestas de error (crearlo con `ProblemDetailsFactory`).
   - Los `401`, `403`, `404` y `405` sin cuerpo salen como `ProblemDetails`. Ya ocurre con `UseStatusCodePages` + `AddProblemDetails`; se confirma manualmente.
   - Los `500` nunca exponen el mensaje ni la traza de la excepción. Ya ocurre; se mantiene.

2. **CORS**
   - Una política por defecto que lee los orígenes permitidos de `Cors:OrigenesPermitidos` (arreglo en la configuración).
   - En `appsettings.json` la lista está vacía y en `appsettings.Development.json` contiene `http://localhost:5173` (provisional hasta conocer el puerto del frontend).
   - Con la lista vacía no se permite ningún origen.

3. **Rutas en kebab-case**
   - `ApiControllerBase` ya fija `api/v1/[controller]`. Se añade un transformador de rutas (`SlugifyParameterTransformer`), así los controllers con nombre compuesto generan rutas como `/api/v1/cambio-grupo`.

4. **Pruebas unitarias de lo existente**
   - `PaginacionRequest` limita `Page ≥ 1` y `1 ≤ PageSize ≤ 100`.

5. **README**
   - Documentar la sección `Cors:OrigenesPermitidos`.

### Fuera de alcance

- JWT, políticas de autorización, el esquema de seguridad en OpenAPI, rate limiting, seed del administrador y `PasswordHasher`: van en `acceso`.
- Entidades y migraciones.
- Pruebas de integración y su infraestructura (`matricula_tests`, `WebApplicationFactory`): diferidas.
- Health check de la BD: requiere un paquete nuevo.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, MailKit 4.18.1, xUnit 2.9.3, `System.Security.Cryptography.Pkcs` 10.0.13 (aprobado: parchea vulnerabilidades que trae MailKit). **No se añaden más paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Api/Common/
  GlobalExceptionHandler.cs        + mapeo 23505 → 409 RN-13
  ValidationFilter.cs              + traceId en el 400
  SlugifyParameterTransformer.cs   nuevo
src/Matricula.Api/Program.cs       + CORS, transformador de rutas
src/Matricula.Api/appsettings*.json  + sección Cors
tests/Matricula.UnitTests/Common/
  PaginacionRequestTests.cs        nuevo
README.md                          + sección Cors
```

## Code Style

Igual que el código existente: constructores primarios, `ArgumentNullException.ThrowIfNull`, nombres de dominio en español y sufijos técnicos en inglés, y nombres de prueba `Metodo_Escenario_Resultado`.

```csharp
public class PaginacionRequestTests
{
    [Fact]
    public void PageSize_MayorAlMaximo_SeLimitaA100()
    {
        var request = new PaginacionRequest { PageSize = 500 };

        Assert.Equal(PaginacionRequest.PageSizeMaximo, request.PageSize);
    }
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD): `PaginacionRequest`.
- **Manual**, con la API en marcha (`dev.cmd` o `dotnet run`):

| Qué | Cómo | Esperado |
|---|---|---|
| Ruta inexistente | `curl -i http://localhost:5022/api/v1/no-existe` | `404`, `application/problem+json`, con `traceId` |
| CORS permitido | `curl -i -X OPTIONS http://localhost:5022/health -H "Origin: http://localhost:5173" -H "Access-Control-Request-Method: GET"` | Incluye `Access-Control-Allow-Origin: http://localhost:5173` |
| CORS no permitido | El mismo comando con `Origin: http://otro.local` | Sin `Access-Control-Allow-Origin` |
| Health | `curl -i http://localhost:5022/health` | `200` |
| Scalar | Abrir http://localhost:5022/scalar | Carga sin errores |

- **Verificación diferida a `acceso`**, porque todavía no hay endpoints ni tablas que lo permitan:
  - el `400` con `traceId` (primer DTO con validador);
  - el `409 RN-13` por violación de unicidad (primera tabla con índice único: `usuario.correo`);
  - la ruta kebab-case (primer controller con nombre compuesto, si lo hay).

  Se anotan como verificaciones en las tareas de `acceso`.

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. Los mensajes de error en español y comprensibles.
- **Preguntar antes:** añadir cualquier paquete NuGet. Cambiar los códigos HTTP de `docs/arquitectura.md` §3.
- **Nunca:** crear entidades o migraciones en este módulo. `EnsureCreated`.

## Success Criteria

1. Una violación de unicidad 23505 se traduce en `409` con `code: "RN-13"`, sin exponer el nombre de la restricción (revisión de código ahora; verificación manual en `acceso`).
2. El `400` de validación incluye `traceId` (revisión de código ahora; verificación manual en `acceso`).
3. Las verificaciones manuales de la tabla dan el resultado esperado.
4. CORS se rige por `Cors:OrigenesPermitidos`; con la lista vacía no se permite ningún origen.
5. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Decisiones

1. Pruebas de integración diferidas; por ahora, unitarias y verificación manual.
2. `System.Security.Cryptography.Pkcs` 10.0.13 aprobado.
3. CORS en desarrollo: `http://localhost:5173`, provisional.
4. Health check de la BD: fuera por ahora.

## Preguntas abiertas

Ninguna.
