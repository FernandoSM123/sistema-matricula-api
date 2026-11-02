# Tareas: catalogos

Módulo `catalogos` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-catalogos.md`](../../specs/SPEC-catalogos.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-02)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones de cada flujo se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Fecha local del instituto | Completada | ✅ | ✅ | ✅ | ✅ |
| T2. Dominio y persistencia de `Curso` | Completada | ✅ | ✅ | ✅ | ✅ |
| T3. Cursos: registro y detalle | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Cursos: listar, editar, desactivar y reactivar | Completada | ✅ | ➖ | ⏳ | ✅ |
| T5. Dominio y persistencia de `Aula` | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T6. Aulas: API completa | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T7. Dominio y persistencia de `Periodo` | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T8. Periodos: registrar, listar, consultar y editar | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T9. Periodo vigente | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T10. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Verificaciones heredadas

Ninguna se copia a este módulo. La de `fundacion` T3 (ruta en kebab-case de un controller de nombre compuesto) sigue pendiente en el mapa: `CursosController`, `AulasController` y `PeriodosController` no tienen nombre compuesto (decisión 3 de la spec).

## Detalle

### T1. Fecha local del instituto
- Acceptance:
  - `FechaInstituto.Calcular(utc, zona)` en `Domain/Reglas`: función pura que devuelve la fecha local.
  - `IFechaLocal` con `DateOnly Hoy()` en `Application/Abstractions`; `FechaLocal` en Infrastructure, `Singleton`, usa `TimeProvider` y `InstitutoOptions`.
  - `Instituto:ZonaHoraria` en `appsettings.json` con `America/Costa_Rica`. `ValidateOnStart` rechaza una zona vacía o inexistente con un mensaje que nombra la opción.
  - `DatosPersonalesRules.FechaNacimiento`, sus tres validadores y `EstudianteService` usan `IFechaLocal`. No queda ningún cálculo de la fecha de hoy desde UTC en `src/`.
  - README: sección `Instituto` con qué es, el valor, cómo cambiarlo (`Instituto__ZonaHoraria`), por qué no se usa UTC y el requisito de datos de zonas horarias en el despliegue.
- Verify:
  - `FechaInstitutoTests`: a las 05:59 UTC en Costa Rica sigue siendo el día anterior; a las 06:00 UTC ya es el mismo día.
  - Arrancar con `Instituto__ZonaHoraria` vacía o inválida → la API no arranca y el error nombra la opción.
  - Registrar un estudiante después de las 18:00 de Costa Rica, o con `Pacific/Kiritimati` si no es esa hora → `fechaRegistro` es la fecha de la zona configurada.
  - Regresión de `estudiantes`: registro público, registro por administrador y edición siguen funcionando; una fecha de nacimiento futura → `400`.
  - Buscar `GetUtcNow` en `src/`: ningún resultado convierte a `DateOnly`.
  - Resultado (2026-11-02): la verificación se hizo a las 17:58 UTC, así que el caso de la zona usó `Pacific/Kiritimati`, ya en 2026-11-03: `fechaRegistro` y el límite de la fecha de nacimiento siguieron esa zona. Zona vacía o inválida: la API no arranca. Regresión de registro público, registro por administrador y edición, correcta.
- Files: `src/Matricula.Domain/Reglas/FechaInstituto.cs`, `src/Matricula.Application/Abstractions/IFechaLocal.cs`, `src/Matricula.Infrastructure/Tiempo/{InstitutoOptions,FechaLocal}.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`, `src/Matricula.Application/Common/Validacion/DatosPersonalesRules.cs`, `src/Matricula.Application/Estudiantes/{EstudianteService,RegistrarEstudianteRequestValidator,ActualizarEstudianteRequestValidator,RegistroPublicoRequestValidator}.cs`, `src/Matricula.Api/appsettings.json`, `README.md`, `tests/Matricula.UnitTests/Domain/FechaInstitutoTests.cs`

### T2. Dominio y persistencia de `Curso`
- Acceptance:
  - Enum `NivelCurso`: `Basico`, `Intermedio`, `Avanzado`.
  - `Curso : EntidadBase` con las propiedades de la spec, constructor privado para EF y constructor público que crea el curso activo. Métodos `NormalizarCodigo` (estático), `ActualizarDatos`, `Desactivar` y `Reactivar`.
  - `CursoConfiguration`: longitudes, `Nivel` como string, `Costo` con precisión `(10,2)`, índice único en `codigo`.
  - `DbSet<Curso>` en `IAppDbContext` y `AppDbContext`. Migración `AgregaCurso`.
- Verify:
  - `CursoTests`: normaliza el código, recorta textos, descripción en blanco → `null`, se crea activo, `ActualizarDatos` cambia todos los datos, `Desactivar` y `Reactivar` cambian el estado.
  - `dotnet ef database update`; `\d curso` muestra el índice único y `costo numeric(10,2)`.
  - Resultado (2026-11-02): `AgregaCurso` aplicada a la BD de desarrollo y sin cambios de modelo pendientes. `\d curso` no se pudo ejecutar porque `psql` pide una contraseña que no estaba disponible; el índice `ix_curso_codigo` (único) y `costo numeric(10,2)` se comprobaron en la migración aplicada, no en el catálogo de la BD.
- Files: `src/Matricula.Domain/Enums/NivelCurso.cs`, `src/Matricula.Domain/Entities/Curso.cs`, `src/Matricula.Infrastructure/Persistence/Configurations/CursoConfiguration.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración, `tests/Matricula.UnitTests/Domain/CursoTests.cs`

### T3. Cursos: registro y detalle
- Acceptance:
  - `RegistrarCursoRequest` + validador: código hasta 20, nombre hasta 150, descripción hasta 1000, duración mayor que 0, costo mayor o igual a 0 con hasta 2 decimales y por debajo del máximo de `numeric(10,2)`, nivel válido.
  - `CursoResponse` y `CursoProyecciones`.
  - `CursoService.RegistrarAsync`: código de un curso activo → `409 RN-13`; de uno inactivo → `409 RN-39` con `cursoId`.
  - `CursoService.ObtenerAsync` → `200` o `404`.
  - `CursosController`: `POST /api/v1/cursos` (`201` + `Location`, `Politicas.Administrador`) y `GET /api/v1/cursos/{id:int}` (`[Authorize]`). Sin atributo de clase. `CursoService` registrado en `AddApplication`.
- Verify:
  - Registro válido → `201` + `Location`; `Location` responde `200`.
  - Código repetido con otra capitalización → `409 RN-13`.
  - Costo negativo, duración 0, código vacío o de 21 caracteres, nivel inexistente o cuerpo vacío → `400` en español.
  - Detalle de un id inexistente → `404`.
  - `POST` con token de profesor o estudiante → `403`; `GET /{id}` con esos tokens → `200`; sin token → `401`; con cambio pendiente → `403`.
  - Se verifica en T4 (necesita un curso inactivo): código de un inactivo → `409 RN-39` con `cursoId`.
  - Resultado (2026-11-02): todos los casos dieron lo esperado con tokens reales de administrador, profesor y estudiante, y con un token de cambio pendiente (`403 RN-24`). También se comprobaron el costo máximo (`99999999.99` → `201`), más de 2 decimales y por encima del máximo (`400`), y que omitir `costo` o `nivel` responde `400`. Por eso son anulables en el DTO: 0 y `Basico` son valores válidos. `nivel` acepta el número del enum (`1` → `Intermedio`) por la configuración global de `JsonStringEnumConverter`; un número fuera de rango → `400`.
- Files: `src/Matricula.Application/Cursos/{CursoService,RegistrarCursoRequest,RegistrarCursoRequestValidator,CursoRules,CursoResponse,CursoProyecciones}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/CursosController.cs`

### T4. Cursos: listar, editar, desactivar y reactivar
- Acceptance:
  - `ListarCursosRequest` (hereda `PaginacionRequest`, con `Nivel` e `IncluirInactivos`); `ListarAsync`: solo activos salvo `incluirInactivos` (RN-41), búsqueda en nombre y código, orden por nombre e id, `Select` y `AsNoTracking`.
  - `ActualizarCursoRequest` + validador con las mismas reglas; `ActualizarAsync`: código de otro curso, activo o inactivo → `409 RN-13`.
  - `DesactivarAsync` y `ReactivarAsync`, idempotentes.
  - `GET /api/v1/cursos` (`[Authorize]`); `PUT /{id:int}`, `POST /{id:int}/desactivar` y `.../reactivar` (`Politicas.Administrador`).
- Verify:
  - El listado pagina, ordena, busca por nombre y código y filtra por nivel; `nivel` inexistente → `400`.
  - Editar → `200`; conservar el código propio → `200`; código de otro curso → `409 RN-13`; id inexistente → `404`.
  - Desactivar y reactivar → `204`, también al repetir; el detalle refleja el estado.
  - El inactivo se oculta sin `incluirInactivos` y aparece con el filtro.
  - ✅ Pendiente de T3: registrar el código de un inactivo → `409 RN-39` con `cursoId`.
  - Escrituras con token de profesor o estudiante → `403`; listado con esos tokens → `200`.
  - ⏳ La guarda RN-37 la añade `grupos`.
  - Resultado (2026-11-02): todos los casos dieron lo esperado, incluido el pendiente de T3. También se comprobó editar un curso inactivo (`200`, sigue inactivo), editar al código de un inactivo (`409 RN-13`) y desactivar o reactivar un id inexistente (`404`). En la consulta, un `nivel` numérico fuera de rango → `400`.
- Files: `src/Matricula.Application/Cursos/{CursoService,ListarCursosRequest,ActualizarCursoRequest,ActualizarCursoRequestValidator}.cs`, `src/Matricula.Api/Controllers/CursosController.cs`

### T5. Dominio y persistencia de `Aula`
- Acceptance:
  - `Aula : EntidadBase` con las propiedades de la spec, activa al crearse. Métodos `NormalizarCodigo`, `ActualizarDatos`, `Desactivar` y `Reactivar`.
  - `AulaConfiguration`: longitudes e índice único en `codigo`.
  - `DbSet<Aula>` en `IAppDbContext` y `AppDbContext`. Migración `AgregaAula`.
- Verify:
  - `AulaTests`: normaliza el código, recorta textos, ubicación en blanco → `null`, se crea activa, `ActualizarDatos` cambia todos los datos, `Desactivar` y `Reactivar` cambian el estado.
  - `dotnet ef database update`; `\d aula` muestra el índice único.
- Files: `src/Matricula.Domain/Entities/Aula.cs`, `src/Matricula.Infrastructure/Persistence/Configurations/AulaConfiguration.cs`, `IAppDbContext.cs`, `AppDbContext.cs`, migración, `tests/Matricula.UnitTests/Domain/AulaTests.cs`

### T6. Aulas: API completa
- Acceptance:
  - `Politicas.AdministradorOProfesor`, combinada con `conSesionSinCambioPendiente`.
  - `AulaService` con `RegistrarAsync`, `ObtenerAsync`, `ListarAsync`, `ActualizarAsync`, `DesactivarAsync` y `ReactivarAsync`, con el mismo comportamiento que cursos. Búsqueda en código y ubicación; orden por código e id.
  - Validadores: código hasta 20, ubicación hasta 150, capacidad mayor que 0.
  - `AulasController`: lecturas con `Politicas.AdministradorOProfesor` y escrituras con `Politicas.Administrador`. `AulaService` registrado en `AddApplication`.
- Verify:
  - Registro válido → `201` + `Location`; código repetido → `409 RN-13`; capacidad 0 → `400`.
  - Código de un aula inactiva → `409 RN-39` con `aulaId`.
  - Listado con búsqueda, paginación e `incluirInactivos`; editar, desactivar y reactivar como en cursos.
  - Lecturas: administrador y profesor → `200`; estudiante → `403`. Escrituras: profesor y estudiante → `403`.
  - ⏳ RN-37 y la guarda de capacidad las añade `grupos`.
- Files: `src/Matricula.Api/Seguridad/Politicas.cs`, `src/Matricula.Application/Aulas/{AulaService,RegistrarAulaRequest,RegistrarAulaRequestValidator,ActualizarAulaRequest,ActualizarAulaRequestValidator,ListarAulasRequest,AulaResponse,AulaProyecciones}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/AulasController.cs`

### T7. Dominio y persistencia de `Periodo`
- Acceptance:
  - `ReglasPeriodo`: una función por cada condición de RN-16 (decisión 5 de la spec) y `Coherente(...)`, que las combina.
  - `Periodo : EntidadBase` con las propiedades de la spec, sin `Activo`. Métodos `NormalizarNombre`, `ActualizarDatos`, `EnRangoMatricula` y `PermiteRetiro`, con límites inclusivos. Constructor y `ActualizarDatos` lanzan `BusinessRuleException("RN-16", ...)` si las fechas no son coherentes.
  - `PeriodoConfiguration`: longitud e índice único en `nombre`.
  - `DbSet<Periodo>` en `IAppDbContext` y `AppDbContext`. Migración `AgregaPeriodo`.
- Verify:
  - `ReglasPeriodoTests`: cada condición en su límite (iguales, un día antes y un día después), incluido `FinMatricula == FinClases` válido y `FechaLimiteRetiro` antes de `InicioClases` válida.
  - `PeriodoTests`: normaliza el nombre, rechaza fechas incoherentes, `EnRangoMatricula` y `PermiteRetiro` en los límites.
  - `dotnet ef database update`; `\d periodo` muestra el índice único.
- Files: `src/Matricula.Domain/Reglas/ReglasPeriodo.cs`, `src/Matricula.Domain/Entities/Periodo.cs`, `src/Matricula.Infrastructure/Persistence/Configurations/PeriodoConfiguration.cs`, `IAppDbContext.cs`, `AppDbContext.cs`, migración, `tests/Matricula.UnitTests/Domain/{ReglasPeriodoTests,PeriodoTests}.cs`

### T8. Periodos: registrar, listar, consultar y editar
- Acceptance:
  - `RegistrarPeriodoRequest`, `ActualizarPeriodoRequest` y validadores: nombre hasta 50 y RN-16 con `ReglasPeriodo`, con el error en el campo correspondiente.
  - `PeriodoService` con `RegistrarAsync`, `ObtenerAsync`, `ListarAsync` y `ActualizarAsync`. Nombre repetido → `409 RN-13`. Listado con búsqueda por nombre y orden por `InicioClases` descendente e id.
  - `PeriodosController`: `POST` y `PUT` con `Politicas.Administrador`; `GET` con `[Authorize]`. `PeriodoService` registrado en `AddApplication`.
- Verify:
  - Registro válido → `201` + `Location`; nombre repetido con otra capitalización → `409 RN-13`.
  - Incumplir cada condición de RN-16, al crear y al editar → `400` con el campo y el mensaje.
  - `FechaLimiteRetiro` anterior a `InicioClases` y no anterior a `InicioMatricula` → `201`.
  - Listado con búsqueda y paginación, los más recientes primero; editar conservando el nombre propio → `200`; id inexistente → `404`.
  - Lecturas con token de profesor y estudiante → `200`; escrituras → `403`.
- Files: `src/Matricula.Application/Periodos/{PeriodoService,RegistrarPeriodoRequest,RegistrarPeriodoRequestValidator,ActualizarPeriodoRequest,ActualizarPeriodoRequestValidator,ListarPeriodosRequest,PeriodoResponse,PeriodoProyecciones}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/PeriodosController.cs`

### T9. Periodo vigente
- Acceptance:
  - `PeriodoService.ObtenerVigenteAsync` con `IFechaLocal.Hoy()`: `InicioMatricula ≤ hoy ≤ FinClases`; ante varios, el de `InicioMatricula` más reciente y luego el `Id` mayor. Sin vigente → `NotFoundException` con un mensaje claro.
  - `GET /api/v1/periodos/vigente` con `[Authorize]`.
- Verify:
  - Hoy en el límite inferior y en el superior → `200`; un día antes o un día después → `404`.
  - Dos periodos traslapados → devuelve el de `InicioMatricula` más reciente.
  - `/periodos/vigente` no choca con `/periodos/{id:int}`.
  - Con `Instituto__ZonaHoraria=Pacific/Kiritimati` y un periodo cuyo límite cae entre las dos fechas, el resultado cambia según la zona.
- Files: `src/Matricula.Application/Periodos/PeriodoService.cs`, `src/Matricula.Api/Controllers/PeriodosController.cs`

### T10. Cierre del módulo
- Acceptance:
  - `Matricula.Api.http` con el recorrido completo de la spec.
  - Recorrido de la tabla de verificación manual de la spec.
  - Estados de la spec, el mapa y esta lista al día.
- Verify: revisión manual de los estados; prueba de humo final con datos nuevos sobre toda la tabla de verificación de la spec.
- Files: `src/Matricula.Api/Matricula.Api.http` (T3 a T9), `specs/mapa-modulos.md`, `specs/SPEC-catalogos.md`, `tasks/catalogos/todo.md`
