# Plan: fundacion

Módulo `fundacion` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-fundacion.md`](../../specs/SPEC-fundacion.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-10-15; ajustado ese mismo día al diferir las pruebas de integración)

## Enfoque

Cambios pequeños sobre el esqueleto existente, cada uno verificable con `dotnet build`, pruebas unitarias cuando aplique y una comprobación manual con la API en marcha. Cada paso termina con un commit pequeño.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 0 | Commits iniciales: paquete `Pkcs` 10.0.13 y, aparte, la documentación (`specs/`, `tasks/`, `AGENTS.md`, `docs/arquitectura.md`, `README.md`) | — | Deja el árbol limpio antes de empezar |
| 1 | **Manejo de errores**: 23505 → 409 `RN-13` en `GlobalExceptionHandler`; `traceId` en el 400 de `ValidationFilter` | — | Verificación completa en `acceso` |
| 2 | **CORS** desde `Cors:OrigenesPermitidos` | — | Verificable ya con `curl` |
| 3 | **Rutas en kebab-case** (`SlugifyParameterTransformer`) | — | |
| 4 | Unit tests de `PaginacionRequest` | — | |
| 5 | README: sección `Cors` | 2 | |

Los pasos 1 a 4 son independientes entre sí.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| El mapeo 23505 y el `traceId` del 400 no se pueden verificar a mano hasta que existan tablas y DTOs | Se dejan anotados como verificación en las tareas de `acceso`. Para 23505 se comprueba que `InnerException` sea `PostgresException`, para no capturar otros `DbUpdateException` |
| El mensaje de 23505 filtra nombres de tablas o índices | El `detail` es un texto fijo ("Ya existe un registro con ese valor único."); el detalle técnico solo va al log |
| `UseCors` en el orden equivocado del pipeline deja de aplicar | Va antes de `UseAuthorization` (y antes de `UseAuthentication` cuando exista) |
| `ValidationFilter` arma el `ValidationProblemDetails` a mano y no recibe `traceId` | Usar `ProblemDetailsFactory.CreateValidationProblemDetails`, que aplica los valores por defecto (incluye `traceId`) |

## Puntos de verificación

- **Tras 2:** las comprobaciones de CORS con `curl` de la spec dan lo esperado.
- **Final:** los criterios de éxito de la spec cumplidos (1 y 2 por revisión de código); `dotnet build` sin warnings; `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

JWT, autorización, rate limiting y seed del admin → `acceso`. Pruebas de integración → diferidas.
