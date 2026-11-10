# Plan: matriculas

Módulo `matriculas` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-matriculas.md`](../../specs/SPEC-matriculas.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-11-10)

## Enfoque

Igual que `grupos`: primero la base sin endpoints (dominio y persistencia), cubierta con pruebas unitarias y verificada contra la BD. Después **rebanadas verticales**: cada tarea deja un flujo de la spec funcionando de punta a punta (DTO → validador → servicio → controller), verificable a mano con `Matricula.Api.http` y Scalar. Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

El orden sigue el ciclo de vida de una matrícula y termina con las consultas y las guardas sobre otros módulos:

1. **Dominio** (`Matricula`, cupos y RN-11 en `Grupo`, traslape de periodos). Es donde viven RN-03, RN-11 y el retiro, así que se prueba primero y sin BD.
2. **Persistencia** con el token `xmin`: sin él no se puede verificar la concurrencia del último cupo.
3. **Escrituras:** matricular, retirar y cambiar de grupo. Matricular va primero porque crea las validaciones (RN-01…06, RN-11) que reutilizan el retiro y el cambio; el cambio va al final porque combina las dos.
4. **Lecturas:** matrículas e historial del estudiante, su horario y los estudiantes de un grupo. Necesitan matrículas reales, activas y retiradas.
5. **Guardas sobre otros módulos:** RN-12 y RN-06 en `grupos`, eliminación de grupos, RN-36 y eliminación de estudiantes. Van al final porque necesitan matrículas para verificarse y no cambian nada de lo anterior.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio:** enums, `Matricula`, cupos y RN-11 en `Grupo`, traslape de periodos | — | Solo pruebas unitarias; incluye la verificación heredada de `grupos` T1 |
| 2 | **Persistencia:** configuración, `xmin` en `Grupo`, `DbSet`, migración `AgregaMatricula` | 1 | |
| 3 | **Matricular y detalle** (RF-51, RN-01…06, RN-11, RN-18) y concurrencia | 2 | Crea las validaciones compartidas y `Politicas.AdministradorOEstudiante`; verificación heredada de `grupos` T8 |
| 4 | **Retirar** (RF-52, RN-10, RN-11) | 3 | |
| 5 | **Cambiar de grupo** (RF-53) | 3, 4 | Reutiliza las validaciones de 3 y 4 |
| 6 | **Matrículas e historial de un estudiante** (RF-20, RF-54) | 3 | Consultas 2 y 10 |
| 7 | **Horario del estudiante** (RF-21) | 3 | Consulta 6; amplía `/me/horario` |
| 8 | **Estudiantes de un grupo** (RF-49) y detalle del grupo para el estudiante matriculado | 3 | Consulta 3 |
| 9 | **RN-12 al cancelar un grupo** | 3 | |
| 10 | **RN-06 al agregar un bloque** (supuesto 13) | 3 | Reutiliza la búsqueda de choques de 3 |
| 11 | **Eliminar grupos** (RF-46, RN-34) | 3 | |
| 12 | **RN-36 y eliminar estudiantes con su cuenta** (RF-19, RN-34, RN-35) | 3 | Decisión 10 del mapa |
| 13 | **Cierre del módulo** | todos | Recorrido de la tabla de verificación de la spec; último módulo del mapa |

**Paralelismo:** después del paso 3, los pasos 4, 6, 7, 8 y 9 a 12 son independientes entre sí (el 5 necesita el 4). Se hacen en serie porque comparten `MatriculaService`, `GrupoService`, los controllers y `Matricula.Api.http`.

## Decisiones de implementación

- **Servicios:**
  - `Matriculas/MatriculaService`: matricular, retirar, cambiar de grupo y detalle. Usa `ICurrentUser` para RN-18 y para saber si quien opera es administrador (supuesto 1), e `IFechaLocal` para las fechas.
  - `Matriculas/Consultas/MatriculasEstudianteService` (o métodos en `MatriculaService` si quedan cortos): RF-20/RF-54 y RF-49.
  - El horario del estudiante (RF-21) se agrega a `HorarioSemanalService` con un filtro nuevo; reutiliza `BloquesAsync` y `BloqueHorarioResponse`.
- **Quién es el estudiante:** el administrador lo envía en el body (`estudianteId`); el estudiante, nunca: `POST /me/matriculas` usa `ICurrentUser.EstudianteId`. Ambos endpoints llaman al mismo método del servicio con el id ya resuelto, así hay un solo camino de validación.
- **Excepciones del administrador (supuesto 1):** `Grupo.ValidarMovimiento(bool esAdministrador)` lanza `RN-11` para cualquiera en `Finalizado` o `Cancelado`; para el estudiante, además, `RN-02` en `Planificado` y `RN-11` en `Cerrado` o `EnCurso`. El administrador opera en `Planificado`, `Abierto`, `Cerrado` y `EnCurso`. Las fechas (RN-04, RN-10) se omiten en el servicio con el mismo `esAdministrador`. El rol sale de `ICurrentUser.Rol`, nunca del body.
- **Cupos (RN-03):** `Grupo.OcuparCupo()` y `Grupo.LiberarCupo()`. Matricular, retirar, cambiar y cancelar cargan el grupo con seguimiento y lo modifican, así `xmin` protege cada operación.
- **Concurrencia (supuesto 6):** `MatriculaService` envuelve el `SaveChangesAsync` y convierte `DbUpdateConcurrencyException` en `BusinessRuleException("CONCURRENCIA", "El grupo cambió mientras se procesaba la solicitud (por ejemplo, se ocupó el último cupo). Intente de nuevo.")`. Las demás escrituras sobre `Grupo` siguen con el `409 CONCURRENCIA` genérico de `GlobalExceptionHandler`. Sin reintentos.
- **Choques del estudiante (RN-06, supuesto 4):** una extensión en `Matriculas/GuardasMatriculas.cs` (como `GuardasGrupos`) que, dado un estudiante, unos bloques, el periodo del grupo y matrículas a ignorar, carga con `AsNoTracking` los bloques de sus grupos con matrícula `Activa` no `Cancelado` ni `Finalizado` cuyo periodo se traslapa en fechas de clases, y decide en memoria con `ReglasHorario.SeTraslapan`. La usan matricular, cambiar de grupo (ignorando la matrícula de origen) y agregar un bloque (paso 10, una vez por estudiante del grupo, con una sola consulta para todos).
- **Traslape de periodos:** `ReglasPeriodo.ClasesSeTraslapan(inicioA, finA, inicioB, finB)` (inclusivo), probada sin BD. En la consulta se expresa con las mismas comparaciones.
- **Curso repetido (RN-05):** `AnyAsync` sobre matrículas `Activa` del estudiante en grupos del mismo curso y periodo, excluyendo la de origen en el cambio de grupo. Cubre también el mismo grupo, así el índice único filtrado queda solo como red de seguridad (`409 RN-13` genérico si una carrera lo alcanza).
- **Orden de validación al matricular:** RN-01 → RN-02/RN-11 → RN-04 → RN-05 → RN-06 → RN-03. El cupo va al final para que un grupo lleno no oculte un error más útil para el estudiante.
- **Cambio de grupo (RF-53):** carga origen (con su grupo) y destino con seguimiento; valida origen como retiro (RN-10, RN-11), destino como ingreso sin RN-04 (supuesto 3), mismo curso y periodo (`409 RF-53`), RN-05 y RN-06 ignorando el origen, y RN-03. Luego `origen.Retirar(CambioGrupo)`, `LiberarCupo`, `OcuparCupo`, `Add` de la nueva y un solo `SaveChangesAsync`. Destino igual al grupo de origen → `409 RF-53`.
- **RN-18:** `MatriculaService` resuelve la matrícula y, si el rol es Estudiante y `EstudianteId` no coincide, lanza `ForbiddenException("...", "RN-18")`. `GET /grupos/{id}/estudiantes` hace lo mismo con el profesor del grupo.
- **Detalle de grupo para el estudiante (spec §3):** `GrupoService.ObtenerAsync` amplía la condición del estudiante a `Abierto || tiene una matrícula en el grupo`.
- **`/me/horario` (RF-21):** `Politicas.ProfesorOEstudiante`; `HorarioSemanalService.MiHorarioAsync` decide por `ICurrentUser.Rol`.
- **RN-12 (paso 9):** `GrupoService.CambiarEstadoAsync`, si el nuevo estado es `Cancelado` y el grupo no lo estaba, carga sus matrículas `Activa`, las retira con `CancelacionGrupo` y `IFechaLocal.Hoy()`, libera cada cupo y guarda todo junto con el cambio de estado.
- **RN-06 al agregar un bloque (paso 10):** `GrupoService.AgregarHorarioAsync`, después de RN-07/RN-08, si el grupo tiene matrículas `Activa` llama a la guarda de choques con el bloque nuevo y los estudiantes del grupo; el mensaje da la cantidad de estudiantes y un NRC con que chocan.
- **Eliminaciones:** `AnyAsync` sobre `Matriculas` (cualquier estado) → `409 RN-34`. Grupo: `Remove` de sus bloques y del grupo en un solo guardado. Estudiante: `Remove` del estudiante y de su `Usuario` en un solo guardado, como `ProfesorService.EliminarAsync`.
- **RN-36 (supuesto 12):** en `UsuarioService.DesactivarAsync` para cuentas con `Rol.Estudiante`: cuenta las matrículas `Activa` del estudiante en grupos cuyo periodo cumple `InicioMatricula ≤ hoy ≤ FinClases`. `UsuarioService` recibe `IFechaLocal`.
- **Configuración EF:** enums como string; FK `Restrict`; índice único filtrado `(estudiante_id, grupo_id)` con `.HasFilter("estado = 'Activa'")`; índice `(grupo_id, estado)`. En `GrupoConfiguration`, `Version` con `.IsRowVersion()` (Npgsql la mapea a `xmin`, sin columna nueva).

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| El filtro del índice único (`estado = 'Activa'`) no coincide con cómo se guarda el enum | Se revisa el SQL de la migración en el paso 2 y se verifica a mano que un estudiante retirado se vuelve a matricular en el mismo grupo |
| Dos peticiones simultáneas toman el último cupo | `xmin` en `Grupo` y `OcuparCupo` sobre la entidad con seguimiento; verificación manual con peticiones en paralelo en el paso 3 (`CuposOcupados` nunca supera `Cupo`) |
| `xmin` hace fallar ediciones del administrador que antes funcionaban | Solo falla si el grupo cambió entre la lectura y el guardado; se repite a mano la edición y el cambio de estado de un grupo en el paso 2 |
| El cambio de grupo queda a medias | Un solo `SaveChangesAsync`; se verifica que un destino que falla deja la matrícula original `Activa` y los dos `cuposOcupados` igual |
| `CuposOcupados` se desincroniza de las matrículas activas | Toda operación que cambia una matrícula pasa por `OcuparCupo`/`LiberarCupo`; en el cierre se compara `cuposOcupados` con el conteo de `GET /grupos/{id}/estudiantes` en varios grupos |
| RN-06 compara contra periodos que no se traslapan, o no compara contra los que sí | La regla de periodos tiene pruebas unitarias; la verificación manual incluye un choque en otro periodo traslapado y uno en un periodo separado |
| La excepción del administrador se filtra al estudiante | El rol sale del token, no del body; la verificación de los pasos 3 a 5 repite cada caso con los dos roles |
| RN-36 en `UsuarioService` rompe la desactivación de profesores o administradores | Solo se aplica con `Rol.Estudiante`; se repite a mano la desactivación de un profesor sin grupos activos |
| El paso 10 vuelve lento agregar bloques en grupos grandes | Una sola consulta trae los bloques de todos los estudiantes del grupo para los días del bloque nuevo; la comparación es en memoria |

## Puntos de verificación

- **Tras 1:** reglas de dominio en verde, incluido el cupo de `ActualizarDatos` contra `CuposOcupados` (heredada de `grupos` T1).
- **Tras 2:** la migración crea `matricula` con las FK `Restrict` y el índice único filtrado; editar y cambiar de estado un grupo sigue funcionando con `xmin`.
- **Tras 3:** un grupo se llena con matrículas reales: la siguiente responde `409 RN-03`, la oferta lo muestra con `cuposDisponibles = 0` (heredada de `grupos` T8) y dos peticiones simultáneas por el último cupo dejan una sola matrícula.
- **Tras 5:** un cambio que falla no altera nada; uno exitoso deja el historial con la matrícula retirada y la nueva.
- **Tras 8:** las consultas 2, 3, 6 y 10 responden con el acceso de la matriz §2.2.
- **Tras 12:** un estudiante eliminado ya no puede usar su token, y su correo y su cédula se registran de nuevo.
- **Final:** toda la tabla de verificación manual de la spec; los criterios de éxito cumplidos; `dotnet build` sin warnings; `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Lo que la spec deja fuera de alcance: lista de espera, correos de matrícula, cambios de estado automáticos, eliminar matrículas, reportes. Pruebas de integración y de concurrencia automatizadas → diferidas.
