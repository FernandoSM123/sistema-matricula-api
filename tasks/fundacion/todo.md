# Tareas: fundacion

Módulo `fundacion` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-fundacion.md`](../../specs/SPEC-fundacion.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-10-15)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T0. Commits iniciales | Completada | ➖ | ➖ | ✅ | ✅ |
| T1. Manejo de errores | Pendiente | ⬜ | ➖ | ⬜ ⏳ | ⬜ |
| T2. CORS | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T3. Rutas kebab-case | Pendiente | ⬜ | ➖ | ⬜ ⏳ | ⬜ |
| T4. Tests de `PaginacionRequest` | Pendiente | ⬜ | ⬜ | ➖ | ⬜ |
| T5. README | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Detalle

### T0. Commits iniciales
- Acceptance: un commit con el paquete `System.Security.Cryptography.Pkcs` 10.0.13 y otro con la documentación (`specs/`, `tasks/`, `AGENTS.md`, `docs/arquitectura.md`, `README.md`). `.claude/` queda fuera.
- Verify: `dotnet build`; `git status` limpio salvo `.claude/`.
- Files: `src/Matricula.Infrastructure/Matricula.Infrastructure.csproj`, documentación

### T1. Completar el manejo de errores
- Acceptance:
  - `GlobalExceptionHandler` mapea `DbUpdateException` cuya `InnerException` es `PostgresException { SqlState: "23505" }` → 409, `code: "RN-13"`, con un `detail` fijo y legible. El detalle técnico se registra en el log.
  - `ValidationFilter` crea el 400 con `ProblemDetailsFactory`, para que incluya `traceId`.
- Verify:
  - `dotnet build`.
  - Con la API en marcha, `curl -i http://localhost:5022/api/v1/no-existe` → 404 `application/problem+json` con `traceId`.
  - ⏳ En `acceso`: el 409 RN-13 y el `traceId` del 400.
- Files: `src/Matricula.Api/Common/GlobalExceptionHandler.cs`, `src/Matricula.Api/Common/ValidationFilter.cs`

### T2. CORS
- Acceptance:
  - Política por defecto con los orígenes de `Cors:OrigenesPermitidos`; `UseCors()` va antes de `UseAuthorization()`.
  - `appsettings.json` con `[]` y `appsettings.Development.json` con `["http://localhost:5173"]`.
  - Con la lista vacía no se permite ningún origen.
- Verify: los dos `curl -X OPTIONS` de la spec. El origen permitido recibe `Access-Control-Allow-Origin` y el no permitido no lo recibe.
- Files: `src/Matricula.Api/Program.cs`, `src/Matricula.Api/appsettings.json`, `src/Matricula.Api/appsettings.Development.json`

### T3. Rutas en kebab-case
- Acceptance: `SlugifyParameterTransformer` registrado como `RouteTokenTransformerConvention` en `AddControllers`; convierte `CambioGrupo` en `cambio-grupo`.
- Verify:
  - `dotnet build`; `/health` y Scalar siguen funcionando.
  - ⏳ La ruta compuesta se verifica con el primer controller de nombre compuesto.
- Files: `src/Matricula.Api/Common/SlugifyParameterTransformer.cs`, `src/Matricula.Api/Program.cs`

### T4. Unit tests de `PaginacionRequest`
- Acceptance: `Page < 1` → 1; `PageSize < 1` → 1; `PageSize > 100` → 100; los valores por defecto son 1 y 20.
- Verify: `dotnet test tests/Matricula.UnitTests`.
- Files: `tests/Matricula.UnitTests/Common/PaginacionRequestTests.cs`

### T5. README
- Acceptance: el README documenta `Cors:OrigenesPermitidos`: qué es, el valor en desarrollo y cómo cambiarlo.
- Verify: revisión manual.
- Files: `README.md`

## Cierre del módulo
- ⬜ Criterios de éxito de la spec cumplidos (1 y 2 por revisión de código).
- ⬜ Verificaciones ⏳ anotadas en `tasks/acceso/todo.md` cuando se cree: el 409 RN-13, el `traceId` en el 400 y la ruta kebab-case.
