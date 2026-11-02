# Plan: catalogos

Módulo `catalogos` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-catalogos.md`](../../specs/SPEC-catalogos.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-11-02)

## Enfoque

Igual que `profesores`: primero la base sin endpoints de cada catálogo (dominio y persistencia), verificada con pruebas unitarias y con la BD. Después **rebanadas verticales**: cada tarea deja un flujo de la spec funcionando de punta a punta (DTO → validador → servicio → controller), verificable a mano con `Matricula.Api.http` y Scalar. Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

La fecha local va primero porque es la única pieza transversal y toca código ya terminado de `estudiantes`: conviene dejarla estable antes de empezar el módulo. Después se sigue el orden **cursos → aulas → periodos**, de menos a más reglas. `Curso` fija el patrón, `Aula` lo repite y `Periodo` agrega RN-16 y el vigente, que necesita la fecha local.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Fecha local:** `FechaInstituto`, `IFechaLocal`, `InstitutoOptions`, ajuste de `estudiantes` y README | — | Toca código existente; regresión del registro de estudiantes |
| 2 | **Dominio y persistencia de `Curso`:** `NivelCurso`, entidad, configuración y migración `AgregaCurso` | — | |
| 3 | **Cursos: registro y detalle** (RF-29, RF-31, RN-13, RN-39) | 2 | `GET /{id}` va aquí para que el `Location` del `201` sea válido |
| 4 | **Cursos: listar, editar, desactivar y reactivar** (RF-30, RF-32, RF-33, RN-41) | 3 | Completa el `409 RN-39` del paso 3 |
| 5 | **Dominio y persistencia de `Aula`** y migración `AgregaAula` | — | |
| 6 | **Aulas: API completa** (RF-39, RF-40, RN-13, RN-39, RN-41) y `Politicas.AdministradorOProfesor` | 5 | Una sola tarea: repite el patrón de cursos |
| 7 | **Dominio y persistencia de `Periodo`:** `ReglasPeriodo`, entidad, configuración y migración `AgregaPeriodo` | — | |
| 8 | **Periodos: registrar, listar, consultar y editar** (RF-35, RF-36, RN-13, RN-16) | 7 | |
| 9 | **Periodo vigente** (RF-38) | 1, 8 | La ruta literal `vigente` no choca con `{id:int}` |
| 10 | **Cierre del módulo:** `Matricula.Api.http`, estados en la spec, el mapa y la lista | todos | Recorrido de la tabla de verificación de la spec |

**Paralelismo:** los tres catálogos son independientes entre sí. Se hacen en serie porque comparten `IAppDbContext`, `AppDbContext` y `Matricula.Api.http`, y las migraciones deben quedar en orden.

## Decisiones de implementación

- **Fecha local:** la conversión es una función pura, `FechaInstituto.Calcular(DateTimeOffset utc, TimeZoneInfo zona)`, en `Domain/Reglas`, para probarla sin BD ni paquetes de pruebas. `FechaLocal` (Infrastructure, `Singleton`) resuelve la zona una vez con `TimeZoneInfo.FindSystemTimeZoneById` y la combina con `TimeProvider`. `InstitutoOptions` se valida con `.Validate(...)` y `.ValidateOnStart()`, igual que `SmtpOptions`.
- **Ajuste de `estudiantes`:** `FechaNacimiento(...)` recibe `IFechaLocal` en lugar de `TimeProvider`, y los dos validadores que lo usan más el de registro público cambian su parámetro. En `EstudianteService` se reemplaza `Hoy()`.
- **Normalización:** `Curso.NormalizarCodigo`, `Aula.NormalizarCodigo` y `Periodo.NormalizarNombre` son estáticos, como `Usuario.NormalizarCorreo`: recortan y pasan a mayúsculas con `ToUpperInvariant`. Los servicios los usan antes de buscar duplicados.
- **Duplicados:** como en `ProfesorService`, un método privado `ValidarCodigoDisponibleAsync`: activo → `RN-13`; inactivo → `RN-39` con `cursoId` o `aulaId`. En la edición, cualquier otro registro con ese código → `RN-13`.
- **Desactivar y reactivar:** `Curso` y `Aula` cambian `Activo` en el dominio. El servicio guarda sin validar nada más; `grupos` pondrá ahí RN-37. Sin `UsuarioService`, porque no tienen cuenta.
- **Autorización por acción:** cada controller lleva `[Authorize]` en las lecturas y `[Authorize(Policy = Politicas.Administrador)]` en las escrituras, sin atributo de clase.
- **Filtro `nivel`:** `ListarCursosRequest.Nivel` es `NivelCurso?`. Un valor inexistente responde `400` mediante `RespuestaModeloInvalido`.
- **RN-16:** `ReglasPeriodo` expone una función por condición, para que el validador indique el campo exacto, y `Coherente(...)`, que las combina. La entidad lanza `BusinessRuleException("RN-16", ...)` si no se cumple; por la API no se alcanza, porque el validador responde `400` antes.
- **Vigente:** `PeriodoService.ObtenerVigenteAsync` filtra en SQL con `InicioMatricula <= hoy && hoy <= FinClases`, y ordena por `InicioMatricula` y `Id` descendentes. No lleva método de dominio: el criterio de desempate es de consulta, y se verifica a mano. `matriculas` y `grupos` reutilizan este método.
- **Configuraciones EF:** `HasConversion<string>()` para `Nivel`, `HasPrecision(10, 2)` para `Costo`, e índices únicos en `Codigo` y `Nombre`.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| Un entorno sin datos de zonas horarias no resuelve `America/Costa_Rica` (imagen Docker mínima o globalización invariante) | `ValidateOnStart` hace fallar el arranque con un mensaje que nombra la opción, en vez de calcular fechas incorrectas. Se documenta en el README |
| El ajuste de `estudiantes` rompe el registro público o el de administrador | Al terminar el paso 1 se repiten a mano los dos registros y la edición, incluida la validación de fecha de nacimiento futura |
| Un `[Authorize]` a nivel de controller deja las lecturas solo para administradores | Autorización por acción y verificación manual por rol en cada tarea de API |
| `ToUpperInvariant` se usa en el servicio y en el dominio de forma distinta y deja pasar duplicados | Un solo método estático de normalización por entidad, cubierto por pruebas unitarias |
| Dos registros simultáneos con el mismo código pasan la validación previa | El índice único lo impide y `GlobalExceptionHandler` responde `409 RN-13`. Se acepta, igual que en `profesores` |
| Un curso o aula creados por error no se pueden eliminar hasta `grupos` | Es lo esperado (decisión 1 de la spec): se desactivan |
| Un `DELETE` futuro con FK `Restrict` responde `500` si no se valida antes | Queda para `grupos`, que debe comprobar los grupos antes de eliminar |

## Puntos de verificación

- **Tras 1:** registrar un estudiante después de las 18:00 de Costa Rica da `fechaRegistro` local; una zona inválida impide arrancar.
- **Tras 2, 5 y 7:** `\d curso`, `\d aula` y `\d periodo` en `psql` muestran los índices únicos, y `costo` como `numeric(10,2)`.
- **Tras 4:** ciclo completo de un curso: registrar, listar, editar, desactivar, `409 RN-39` y reactivar, con lectura por los tres roles.
- **Tras 6:** el estudiante recibe `403` en aulas y el profesor `200` en las lecturas y `403` en las escrituras.
- **Tras 9:** con periodos traslapados, el vigente es el de matrícula más reciente; sin ninguno, `404`.
- **Final:** toda la tabla de verificación manual de la spec; los criterios de éxito cumplidos; `dotnet build` sin warnings; `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Eliminar cursos, aulas y periodos, RN-37 y la guarda de capacidad → `grupos` (decisiones 1 y 2 de la spec). RF-34 y RF-41 → `grupos`. Verificación heredada de kebab-case → primer módulo con un controller de nombre compuesto (decisión 3 de la spec). Pruebas de integración → diferidas.
