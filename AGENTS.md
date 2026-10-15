# AGENTS.md — API Sistema de Matrícula

API REST en .NET 10 + EF Core 10 + PostgreSQL para la matrícula de un instituto de cursos.

## Fuentes de verdad
- **Qué construir:** `docs/requerimientos.md` (RF-01..RF-54, reglas RN-01..RN-41).
- **Cómo construirlo:** `docs/arquitectura.md` (stack, capas, convenciones REST, EF Core, seguridad, orden de implementación).
- **Mapa de módulos:** `specs/mapa-modulos.md` (ids, dependencias, orden y decisiones aprobadas).
- **Specs por módulo:** `specs/SPEC-<modulo>.md` · **Plan y tareas por módulo:** `tasks/<modulo>/plan.md`, `tasks/<modulo>/todo.md`.

Si algo de una spec contradice estos documentos, detente y pregunta; no elijas por tu cuenta.

## Comandos
```bash
smtp4dev --smtpport=2525 --urls=http://localhost:5080   # correos de desarrollo (dotnet tool)
dotnet build                         # warnings = errores
dotnet test tests/Matricula.UnitTests   # por ahora solo pruebas unitarias (sin BD)
dotnet run --project src/Matricula.Api   # Scalar: http://localhost:5022/scalar
dotnet ef migrations add <Nombre> --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
```

Entorno: PostgreSQL y smtp4dev instalados localmente; **no se usa Docker** (el `docker-compose.yml` es solo una alternativa opcional).

## Estructura
`src/Matricula.Domain` → entidades, enums, reglas puras · `src/Matricula.Application` → servicios por funcionalidad, DTOs, validadores · `src/Matricula.Infrastructure` → EF Core, correo, seed · `src/Matricula.Api` → controllers, middleware. Dependencias: `Api → Application → Domain`, `Infrastructure → Application`.

## Límites
**Siempre**
- Seguir `docs/arquitectura.md`; nombres de dominio en español sin tildes ni ñ, sufijos técnicos en inglés.
- DTOs de entrada/salida; las entidades nunca salen de Application.
- Errores como `ProblemDetails` con `code` de la regla (`"RN-05"`); `BusinessRuleException` → 409.
- `async` + `CancellationToken` de punta a punta; `TimeProvider` en lugar de `DateTime.Now`.
- Tests: **por el momento solo pruebas unitarias** de las reglas puras del dominio (sin BD). Servicios y endpoints se verifican manualmente con Scalar, `Matricula.Api.http` y smtp4dev.
- `dotnet build` sin warnings y las pruebas unitarias en verde antes de dar una tarea por terminada.

**Diferido:** las pruebas de integración sobre PostgreSQL real (`matricula_tests`, variable `ConnectionStrings__MatriculaTests`) se retomarán más adelante. Mientras tanto no se escriben ni se exigen; las prohibiciones de "Nunca" sobre pruebas siguen vigentes para cuando se retomen.

**Preguntar antes**
- Añadir paquetes NuGet o servicios nuevos.
- Cambiar el esquema de BD fuera de lo que define una spec aprobada.
- Cambiar una regla de negocio o interpretar un requerimiento ambiguo.

**Nunca**
- Exponer entidades en controllers ni usar `DbContext` desde controllers.
- `EnsureCreated`, eliminación en cascada, filtros globales de "Activo", proveedor InMemory o SQLite en tests.
- Apuntar las pruebas a la BD de desarrollo `matricula`, ni agregar dependencias que requieran Docker (Testcontainers).
- MediatR, AutoMapper, repositorios genéricos o Clean Architecture/DDD completos.
- Guardar o registrar en logs contraseñas o temporales; poner secretos en `appsettings*.json` (usar user-secrets).

## Flujo de trabajo
Usar la skill **spec-driven-development**: mapa de capacidades → `specs/SPEC-<modulo>.md` → `tasks/<modulo>/plan.md` → `tasks/<modulo>/todo.md` → implementación tarea por tarea, con aprobación en cada fase. Commits pequeños que citen RF/RN cuando aplique.

**Estados (mantenerlos al día):**
- Cada spec lleva en su cabecera su estado, el de su plan y sus tareas, el avance y un historial de cambios.
- Cada `todo.md` lleva una tabla con el estado de cada tarea (Pendiente, En progreso, Implementada, Bloqueada o Completada) y columnas de código, pruebas unitarias, verificación manual y commit.
- `specs/mapa-modulos.md` resume el estado de todos los módulos.
- Al empezar o terminar una tarea, se actualizan los tres. Las leyendas de los estados están en el mapa y en cada `todo.md`.
