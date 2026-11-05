# Plan: grupos

Módulo `grupos` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-grupos.md`](../../specs/SPEC-grupos.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-11-05)

## Enfoque

Igual que `catalogos`: primero la base sin endpoints (dominio y persistencia), cubierta con pruebas unitarias y verificada contra la BD. Después **rebanadas verticales**: cada tarea deja un flujo de la spec funcionando de punta a punta (DTO → validador → servicio → controller), verificable a mano con `Matricula.Api.http` y Scalar. Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño que cite los RF/RN.

El orden va de la gestión del administrador a las consultas y termina con las guardas sobre otros módulos:

1. **Dominio** (`Grupo`, `Horario`, reglas puras). Es donde viven RN-14, RN-15, RF-47 y la edición por estado, así que se prueba primero y sin BD.
2. **Gestión del grupo:** crear, horarios, estados y edición. Los horarios van antes que el cambio de estado, porque abrir un grupo exige bloques (supuesto 6), y antes que la edición, porque esta reutiliza la validación de traslapes.
3. **Lecturas:** listado con visibilidad por rol (RN-18), oferta pública y consultas de profesor y aula.
4. **Guardas sobre otros módulos:** RN-37, RN-09 en la capacidad del aula y las eliminaciones. Van al final porque necesitan grupos reales para verificarse y no cambian nada de lo anterior.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Dominio:** enums, `ReglasHorario`, `Horario`, `Grupo` | — | Solo pruebas unitarias |
| 2 | **Persistencia:** configuraciones, `DbSet`, migración `AgregaGrupo` | 1 | |
| 3 | **Crear grupo y detalle** (RF-42, RF-44, RN-09, RN-13, RN-17) | 2 | El detalle es solo para el administrador hasta el paso 7 |
| 4 | **Horarios: agregar y quitar** (RF-48, RN-07, RN-08, RN-14, RN-15) | 3 | Crea la validación de traslapes que reutiliza el paso 6 |
| 5 | **Cambiar estado** (RF-47) | 4 | |
| 6 | **Editar grupo** (RF-45) | 4 | |
| 7 | **Listado y visibilidad por rol** (RF-43, RN-18) | 3 | Abre el detalle a todos los roles |
| 8 | **Oferta académica y grupos de un curso** (RF-50, RF-34) | 7 | Verificación heredada de `fundacion` T3 |
| 9 | **Consultas de profesor y aula** (RF-27, RF-28, RF-41) y `Politicas.Profesor` | 7 | |
| 10 | **RN-37 y RN-09 en otros módulos** | 3 | Profesor, curso y aula |
| 11 | **Eliminar cursos, aulas y periodos** (RF-33, RF-37, RF-40, RN-34) | 3 | |
| 12 | **Eliminar profesores con su cuenta** (RF-26, RN-34, RN-35) | 3 | Decisión 4 de la spec |
| 13 | **Cierre del módulo** | todos | Recorrido de la tabla de verificación de la spec |

**Paralelismo:** después del paso 4, los pasos 5, 6 y 7 son independientes, y los pasos 10, 11 y 12 solo dependen de que existan grupos. Se hacen en serie porque comparten `GrupoService`, los controllers y `Matricula.Api.http`.

## Decisiones de implementación

- **Servicios:**
  - `GrupoService` (gestión, listado y detalle). Usa `ICurrentUser` para la visibilidad.
  - `Grupos/Consultas/OfertaAcademicaService` (RF-50).
  - `Grupos/Consultas/HorarioSemanalService` (horario del profesor y ocupación del aula). Los dos devuelven la misma forma de bloque.
  - Los grupos de un curso (RF-34) y de un profesor (RF-27) reutilizan `GrupoService.ListarAsync` con el filtro fijado, después de comprobar que el curso o el profesor existen (`404`).
- **Traslapes (RN-07, RN-08):** un método privado `ValidarTraslapesAsync(grupo, bloques, profesorId, aulaId)` en `GrupoService`. Carga con `AsNoTracking` los bloques de los otros grupos del mismo periodo, no cancelados, del mismo profesor o la misma aula (los índices `(periodo_id, profesor_id)` y `(periodo_id, aula_id)` acotan la consulta), y los compara en memoria con `Horario.SeTraslapaCon`. Así RN-14 tiene una sola implementación, probada sin BD. Lo usan agregar horario (los bloques nuevos) y editar (todos los bloques del grupo, con el nuevo profesor o aula).
- **Edición por estado (supuesto 4):** la decide `Grupo.ActualizarDatos`, que lanza `BusinessRuleException("RF-45", ...)` si cambia un dato que el estado no permite. El servicio valida antes lo que necesita la BD (RN-13, RN-17, RN-09, traslapes) y después llama al dominio.
- **Estados (RF-47):** `Grupo.CambiarEstado` con una tabla de transiciones permitidas. Repetir el estado actual no hace nada (`204`). La cancelación no toca matrículas: RN-12 la añade `matriculas`.
- **Horarios:** `Grupo.AgregarHorario` devuelve el `Horario` creado y `QuitarHorario(horarioId)` busca en la colección del grupo, así que un `horarioId` de otro grupo responde `404`. El `201` de agregar horario apunta al detalle del grupo.
- **Modalidad y aula:** el validador rechaza las combinaciones inválidas (`400`) y la entidad lo comprueba como invariante, igual que RN-16 en `Periodo`.
- **RN-09:** al crear y editar, el servicio carga la capacidad del aula. En la edición del aula (paso 10), `AulaService` compara con el `MAX(cupo)` de sus grupos no `Finalizado` ni `Cancelado`.
- **RN-17:** solo se comprueba si el curso, profesor o aula son nuevos en el grupo. Al editar sin cambiarlos no se exige que sigan activos (RN-37 ya impide desactivarlos con grupos activos).
- **Visibilidad (RN-18):** `ListarAsync` fija `profesorId` con `ICurrentUser.ProfesorId` si el rol es Profesor, y fija `estado = Abierto` si es Estudiante. El detalle lanza `ForbiddenException("RN-18", ...)`. `/cursos/{id}/grupos` fija `estado = Abierto` para profesor y estudiante (supuesto 7).
- **Oferta:** `OfertaAcademicaService` reutiliza el criterio de `PeriodoService.ObtenerVigenteAsync`. Para no convertir el `404` en una excepción esperada, se extrae un método que devuelve el id del vigente o `null`, y lo usan los dos. Sin vigente → página vacía.
- **Horario y ocupación sin `periodoId`:** usan el mismo método; sin vigente → `NotFoundException` pidiendo el `periodoId`.
- **RN-37:** en `UsuarioService.DesactivarAsync` (cuentas con rol Profesor, cubre las dos rutas), `CursoService.DesactivarAsync` y `AulaService.DesactivarAsync`. El mensaje incluye la cantidad de grupos activos.
- **Eliminaciones:** cada servicio comprueba con `AnyAsync` que no haya grupos y lanza `BusinessRuleException("RN-34", ...)` con un mensaje que sugiere desactivar. Así nunca llega a la BD un `DELETE` que la FK `Restrict` rechazaría con un `500`.
- **Eliminar profesor (decisión 4 de la spec):** `ProfesorService.EliminarAsync` carga el profesor con su `Usuario`, hace `Remove` de los dos y guarda una sola vez. La regla de RN-35 queda escrita en el comentario del método.
- **Configuraciones EF:** enums con `HasConversion<string>()`, índice único en `nrc`, índices `(periodo_id, profesor_id)`, `(periodo_id, aula_id)` y `horario(grupo_id)`. Sin check constraints, igual que `catalogos`. La colección `Horarios` se mapea por su campo privado.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| La validación de traslapes compara mal en los límites (bloques contiguos) | Una sola implementación (`SeTraslapaCon`), con pruebas unitarias de los límites, y verificación manual de bloques contiguos |
| Al editar, se revisan los traslapes del grupo contra sí mismo | La consulta excluye `GrupoId == grupo.Id`; se verifica editando un grupo con bloques sin cambiar el profesor |
| La visibilidad por rol se olvida en alguna de las rutas que listan grupos | Todas pasan por `GrupoService.ListarAsync`; la verificación de los pasos 7 a 9 se hace con los tres roles |
| `/me/horario` choca más adelante con el horario del estudiante (RF-21) | Se documenta en el controller que `matriculas` ampliará la política a Profesor o Estudiante |
| Dos administradores agregan a la vez bloques que chocan entre sí | Se acepta: no hay restricción de BD para traslapes y la concurrencia de grupos la añade `matriculas` con `xmin`. Probabilidad baja: solo administradores escriben |
| La guarda RN-37 en `UsuarioService` rompe la desactivación de estudiantes o administradores | Solo se aplica con `Rol.Profesor`; se repite a mano la desactivación de un estudiante y de un administrador |
| Eliminar un profesor con un token vigente deja sesiones abiertas | `ValidacionSecurityStamp` no encuentra la cuenta y rechaza el token; se verifica en el paso 12 |
| La oferta pública filtra datos personales | La proyección de la oferta solo incluye el nombre del profesor (Boundaries de la spec) |

## Puntos de verificación

- **Tras 1:** todas las reglas de dominio en verde, incluidas las transiciones de RF-47 y los límites de RN-14.
- **Tras 2:** la migración crea `grupo` y `horario` con las FK `Restrict` y los índices.
- **Tras 4:** un profesor y un aula con bloques en dos grupos: los choques responden `409` con el NRC del otro grupo; los contiguos y los de grupos cancelados o de otro periodo se aceptan.
- **Tras 7:** el mismo `GET /grupos` devuelve resultados distintos para administrador, profesor y estudiante.
- **Tras 9:** `/me/horario` y `/profesores/{id}/horario` devuelven lo mismo para el mismo profesor.
- **Tras 12:** un profesor eliminado ya no puede usar su token, y su correo y su cédula se registran de nuevo.
- **Final:** toda la tabla de verificación manual de la spec; los criterios de éxito cumplidos; `dotnet build` sin warnings; `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Eliminar grupos (RF-46) y RN-12 → `matriculas` (decisión 1 de la spec). Mantener `CuposOcupados` y `xmin` → `matriculas` (decisión 2). RF-49 y las consultas 2, 3, 6 y 10 → `matriculas`. Eliminar estudiantes con su cuenta → `matriculas` (decisión 10 del mapa). Pruebas de integración → diferidas.
