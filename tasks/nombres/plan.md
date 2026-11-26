# Plan: nombres

Corrección transversal `nombres` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-nombres.md`](../../specs/SPEC-nombres.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-11-26)

## Enfoque

Es un renombre puro, así que el orden lo da el riesgo, no las funcionalidades:

1. **Primero la regla y la línea base.** La regla queda escrita en `docs/arquitectura.md` §8 y en `AGENTS.md` antes de tocar el código, y se guarda el `openapi/v1.json` de hoy para compararlo al final.
2. **Después el código, de adentro hacia afuera:** Domain → piezas técnicas de Application e Infrastructure → Api → métodos de los servicios y acciones → DTO. Cada capa compila sola antes de pasar a la siguiente, y los DTO, que son lo único que cambia el contrato, van juntos en una tarea aislada.
3. **La configuración aparte**, porque sus claves son texto que el compilador no revisa y exige un paso manual en los user-secrets.
4. **Al final el barrido de documentos**, la búsqueda de los nombres viejos y la verificación manual.

Cada tarea termina con `dotnet build` sin warnings, las 1104 pruebas unitarias en verde y un commit en la rama `nombres`.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | Regla en `docs/arquitectura.md` §8 y `AGENTS.md`; línea base del OpenAPI | — | Sin código |
| 2 | Domain: `Rules/`, métodos de las entidades, `BusinessRuleException.Extensions`, pruebas | 1 | La capa sin dependencias; casi todo el peso de las pruebas unitarias |
| 3 | Application e Infrastructure: abstracciones técnicas y sus implementaciones, carpetas técnicas | 2 | Interfaz e implementación cambian en la misma tarea para que compile |
| 4 | Api técnica, métodos de los servicios y acciones de los controllers | 3 | `Security/`, `Common/`, verbos de §2.3 y §2.5 |
| 5 | DTO y validadores (§2.6) | 4 | Única tarea que cambia el OpenAPI |
| 6 | Configuración (§2.7) y `Seed/TestData` | 3 | Claves de texto; README y `secrets.example.json` en la misma tarea |
| 7 | Cierre: documentos, búsqueda final, verificación manual, estados | 1–6 | |

## Decisiones de implementación

- **Rama `nombres`** desde `main`. Un commit por tarea; el pull request lo abre el usuario.
- **Archivos y carpetas con `git mv`**, para conservar la historia. El namespace sigue a la carpeta (`Matricula.Api.Security`, `Matricula.Application.Common.Pagination`…), y los `using` se corrigen en la misma tarea.
- **Identificadores con reemplazos acotados** (`perl -pi -e 's/\bViejo\b/Nuevo/g'`) sobre `src/` y `tests/`, **excluyendo `Persistence/Migrations/`**, uno por identificador y revisando el diff antes de compilar. Los verbos comunes (`Listar`, `Retirar`, `Obtener`, `Actualizar`) también aparecen en comentarios y mensajes en español: para ellos el patrón incluye el contexto de código (`\bListarAsync\(`, `\.Retirar\(`, `nameof\(Obtener\)`) y el diff se revisa buscando cambios dentro de strings y comentarios, que se revierten.
- **Lo que el compilador no revisa**, y se revisa a mano en la tarea que lo toca:
  - `nameof(...)` en `CreatedAtAction`: el compilador sí los revisa, pero el nombre de la acción viaja como texto a la URL de `Location`; se verifica un `201` en T4.
  - Los valores de las políticas de rate limiting (`"forgot-password"`, `"sign-up"`) y de autorización: se verifican con un `429` y un `403` en T4.
  - Las claves de configuración (T6): `ValidateOnStart` cubre `Jwt` e `Institute`; `TestData` y `ErrorLog` se verifican arrancando con valores distintos de los por defecto.
  - Los métodos `[LoggerMessage]`: el generador de código los revisa; sus mensajes no cambian.
- **Pruebas unitarias:** el primer segmento del nombre del método sigue al método probado (supuesto 4 de la spec). Se renombran en la misma tarea que el código que prueban, y el conteo debe seguir en 1104.
- **Línea base del OpenAPI** en el scratchpad de la sesión, no en el repositorio: `openapi-antes.json` en T1 y `openapi-despues.json` en T5 y T7.
- **`DependencyInjection.cs`** se actualiza en cada tarea con los tipos que cambian; no hay una tarea propia.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| Un reemplazo cambia un mensaje, un comentario o un string | Reemplazos con contexto de código y revisión del diff por tarea; búsqueda en T7 de los mensajes de error conocidos (`git diff main -- '*.cs' \| grep '^[-+].*"'`), que solo debe mostrar `nameof` y claves técnicas |
| Cambia una ruta sin que nadie lo note | Los controllers no se renombran y todas las acciones tienen plantilla explícita. El diff del OpenAPI en T5 y T7 no debe mostrar cambios en `paths` |
| Cambia el modelo de EF | `migrations add VerificaNombres` en T2, T3 y T7 debe salir vacía y se descarta con `migrations remove` |
| Las claves viejas de user-secrets se ignoran sin avisar | `ValidateOnStart` para `Jwt:Key`; el README lista el paso manual; T6 verifica arrancando con los datos de prueba |
| Un nombre nuevo choca con uno del framework | Los choques conocidos ya están resueltos en la spec (`JsonSettings`, `IPasswordHashService`, `PasswordCheckResult`, `Extensions`). Si aparece otro, se pregunta antes de elegir |
| Un nombre que la spec no lista | Se aplica la regla si es claro; si no, se pregunta (decisión 3) y se anota en el historial de la spec |

## Puntos de verificación

- **Después de T2:** las pruebas del dominio, renombradas, siguen en 1104 y la migración de prueba sale vacía.
- **Después de T4:** login, una creación con `201` y su `Location`, un `403` por rol y un `429` del olvido de contraseña se comportan igual que antes.
- **Después de T5:** el diff del OpenAPI contiene solo los esquemas de §2.6 y sus `$ref`.
- **Después de T7:** todos los criterios de éxito de la spec.

## Fuera de este plan

- La regeneración de los tipos del SPA (`pnpm api:types`) y el alias `ProfileResponse` en su `models.ts`: es un cambio del SPA.
- El módulo `sesiones`, que empieza después de cerrar esta corrección.
