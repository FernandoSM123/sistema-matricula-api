# AGENTS.md — API Sistema de Matrícula

API REST en .NET 10 + EF Core 10 + PostgreSQL para la matrícula de un instituto de cursos.

## Fuentes de verdad
- **Qué construir:** `docs/requerimientos.md` (RF-01..RF-60, reglas RN-01..RN-52).
- **Cómo construirlo:** `docs/arquitectura.md` (stack, capas, convenciones REST, EF Core, seguridad, orden de implementación).
- **Mapa de módulos:** `specs/mapa-modulos.md` (ids, dependencias, orden y decisiones aprobadas).
- **Specs por módulo:** `specs/SPEC-<modulo>.md` · **Plan y tareas por módulo:** `tasks/<modulo>/plan.md`, `tasks/<modulo>/todo.md`.
- **Correcciones posteriores al cierre de los módulos:** `correcciones/COR-<nn>-<tema>.md`, con su índice en `correcciones/README.md`.

Si algo de una spec contradice estos documentos, detente y pregunta; no elijas por tu cuenta.

## Comandos
```bash
smtp4dev --smtpport=2525 --urls=http://localhost:5080   # correos de desarrollo (dotnet tool)
dotnet build                         # warnings = errores
dotnet test tests/Matricula.UnitTests   # por ahora solo pruebas unitarias (sin BD)
dotnet run --project src/Matricula.Api   # Scalar: http://localhost:5022/scalar
dotnet ef migrations add <Nombre> --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations

# Reiniciar la BD de desarrollo `matricula` (borra todo); al arrancar, siembra el admin y, con el flag, los datos de prueba
dotnet ef database drop --force --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
```

Datos de prueba (solo `Development`): `TestData:Enabled` y `TestData:Password` en user-secrets; solo siembran sobre una BD sin datos de negocio. Detalle en `README.md` y `specs/SPEC-datos-prueba.md`.

Entorno: PostgreSQL y smtp4dev instalados localmente; **no se usa Docker** (el `docker-compose.yml` es solo una alternativa opcional).

## Estructura
`src/Matricula.Domain` → entidades, enums, reglas puras · `src/Matricula.Application` → servicios por funcionalidad, DTOs, validadores · `src/Matricula.Infrastructure` → EF Core, correo, seed (`Seed/`: administrador inicial; `Seed/TestData/`: datos de prueba con Bogus) · `src/Matricula.Api` → controllers, middleware. Dependencias: `Api → Application → Domain`, `Infrastructure → Application`.

## Límites
**Siempre**
- Seguir `docs/arquitectura.md`.
- Nombres (§8.1 de la arquitectura), siempre sin tildes ni ñ:
  - En español, el dominio del instituto: entidades y sus propiedades (columnas y JSON), enums y sus valores, rutas, controllers (dan la ruta), carpetas de funcionalidad, mensajes, comentarios y descripciones de las pruebas.
  - En inglés, todo lo demás: clases técnicas, métodos, constantes, carpetas técnicas, opciones y sus claves.
  - Combinados, primero el dominio y después lo técnico (`CursoService`, `GrupoDetailResponse`, `CreateCursoRequest`); verbos en inglés (`EnrollAsync`, `ChangeGrupoAsync`).
  - Pruebas: el método en inglés, `Metodo_Escenario_Resultado` (`Rotate_AlreadyRotatedToken_Throws`), y su descripción en español en el `DisplayName` del `[Fact]` o `[Theory]` (COR-04).
- DTOs de entrada/salida; las entidades nunca salen de Application.
- Errores como `ProblemDetails` con `code` de la regla (`"RN-05"`); `BusinessRuleException` → 409.
- `async` + `CancellationToken` de punta a punta; `TimeProvider` en lugar de `DateTime.Now`; `ILocalClock.Today()` para la fecha de hoy (zona de `Institute:TimeZone`), nunca la fecha UTC.
- Tests: **por el momento solo pruebas unitarias** de las reglas puras del dominio (sin BD). Servicios y endpoints se verifican manualmente con Scalar, `Matricula.Api.http` y smtp4dev.
- `dotnet build` sin warnings y las pruebas unitarias en verde antes de dar una tarea por terminada.

**Diferido:** las pruebas de integración sobre PostgreSQL real (`matricula_tests`, variable `ConnectionStrings__MatriculaTests`) se retomarán más adelante. Mientras tanto no se escriben ni se exigen; las prohibiciones de "Nunca" sobre pruebas siguen vigentes para cuando se retomen.

**Preguntar antes**
- Añadir paquetes NuGet o servicios nuevos.
- Cambiar el esquema de BD fuera de lo que define una spec aprobada.
- Cambiar una regla de negocio o interpretar un requerimiento ambiguo.
- El nombre o el idioma de algo, cuando la regla de nombres no lo deja claro.

**Nunca**
- Exponer entidades en controllers ni usar `DbContext` desde controllers.
- `EnsureCreated`, eliminación en cascada, filtros globales de "Activo", proveedor InMemory o SQLite en tests.
- Apuntar las pruebas a la BD de desarrollo `matricula`, ni agregar dependencias que requieran Docker (Testcontainers).
- MediatR, AutoMapper, repositorios genéricos o Clean Architecture/DDD completos.
- Escribir con `ExecuteUpdate`, `ExecuteDelete`, SQL manual o el `SaveChanges` síncrono: se saltan la auditoría (RN-42). Toda escritura pasa por el change tracker y `SaveChangesAsync`. **Única excepción:** `ExecuteDeleteAsync` en `ErrorLogPurgeService`, para depurar `registro_error` (RN-49), que no se audita. No se extiende a ninguna otra tabla ni operación.
- Registrar la auditoría a mano desde un servicio, o modificar o eliminar registros de auditoría (RN-43): la captura es automática en `AppDbContext.SaveChangesAsync`.
- Guardar en el registro de errores el body, los headers (en especial `Authorization`) o el query string de una petición (RN-47); registrar errores con el `AppDbContext` de la petición, en lugar de uno de un scope aparte; o dejar que una excepción del registro o de la depuración llegue al usuario o detenga la API (RN-48). Los errores se registran solo con `IErrorRecorder`, desde `GlobalExceptionHandler` y `EmailDispatcher`.
- Guardar o registrar en logs contraseñas o temporales; poner secretos en `appsettings*.json` (usar user-secrets).

## Flujo de trabajo
Usar la skill **spec-driven-development**: mapa de capacidades → `specs/SPEC-<modulo>.md` → `tasks/<modulo>/plan.md` → `tasks/<modulo>/todo.md` → implementación tarea por tarea, con aprobación en cada fase. Commits pequeños que citen RF/RN cuando aplique.

**Estados (mantenerlos al día):**
- Cada spec lleva en su cabecera su estado, el de su plan y sus tareas, el avance y un historial de cambios.
- Cada `todo.md` lleva una tabla con el estado de cada tarea (Pendiente, En progreso, Implementada, Bloqueada o Completada) y columnas de código, pruebas unitarias, verificación manual y commit.
- `specs/mapa-modulos.md` resume el estado de todos los módulos.
- Al empezar o terminar una tarea, se actualizan los tres. Las leyendas de los estados están en el mapa y en cada `todo.md`.
