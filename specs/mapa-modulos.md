# Mapa de módulos — API Sistema de Matrícula

Índice de los módulos del sistema. Cada módulo tiene su spec en `specs/SPEC-<id>.md`, su plan y sus tareas en `tasks/<id>/plan.md` y `tasks/<id>/todo.md`, y se construye en el orden indicado (Specify → Plan → Tasks → Implement). Los ids son estables: no se renombran.

| Id | Responsabilidad | RF | RN principales | Depende de |
|---|---|---|---|---|
| `fundacion` | Infraestructura transversal: mapeo de excepciones → `ProblemDetails`, CORS, convenciones de la API | — (RNF de validación e integridad) | RN-34/35 a nivel de esquema (`Restrict`) | — |
| `acceso` | `Usuario`, JWT + `SecurityStamp`, login/logout, contraseñas temporales, cambio obligatorio, olvido, cambio propio, perfil, reenvío, activar/desactivar cuentas, registro de administradores, seed del primer admin, rate limiting | RF-04…RF-14 | RN-20, RN-22…RN-33 | fundacion |
| `estudiantes` | Registro público y por admin, CRUD, desactivar/reactivar/eliminar, edición de datos de contacto propios (`/api/me`) | RF-01, 02, 15…19 | RN-13, 19, 21, 31, 38, 39, 41 | acceso |
| `profesores` | Registro por admin, CRUD, desactivar/reactivar/eliminar | RF-03, 22…26 | RN-13, 31, 38, 39, 41 | acceso |
| `catalogos` | Cursos, periodos académicos (incluido el vigente) y aulas | RF-29…33, 35…40 | RN-13, 16, 39, 41 | acceso |
| `grupos` | Grupos, horarios, estados, oferta académica y consultas propias de grupos | RF-27, 28, 34, 41…48, 50 | RN-07…09, 14, 15, 17, 37 | catalogos, profesores |
| `matriculas` | Matricular, retirar, cambiar de grupo, concurrencia del cupo (`xmin`) y consultas que requieren matrículas | RF-20, 21, 49, 51…54 | RN-01…06, 10…12, 18, 36 | grupos, estudiantes |

**Orden de construcción:** `fundacion` → `acceso` → (`estudiantes`, `profesores`, `catalogos` en cualquier orden) → `grupos` → `matriculas`

## Estado de los módulos

| Módulo | Spec | Plan | Tareas | Avance | Estado del módulo |
|---|---|---|---|---|---|
| `fundacion` | [Aprobada](SPEC-fundacion.md) | [Aprobado](../tasks/fundacion/plan.md) | [Aprobadas](../tasks/fundacion/todo.md) | 4/6 | En implementación |
| `acceso` | — | — | — | — | Sin empezar |
| `estudiantes` | — | — | — | — | Sin empezar |
| `profesores` | — | — | — | — | Sin empezar |
| `catalogos` | — | — | — | — | Sin empezar |
| `grupos` | — | — | — | — | Sin empezar |
| `matriculas` | — | — | — | — | Sin empezar |

**Estados de un documento** (spec, plan o lista de tareas):
- **Borrador:** se está escribiendo.
- **En revisión:** espera la aprobación del usuario.
- **Aprobado/a:** aprobado y vigente.
- **Obsoleto/a:** reemplazado; queda solo como historial.

**Estados de un módulo:**
- **Sin empezar.**
- **Planificación:** spec, plan o tareas sin aprobar.
- **En implementación:** tareas aprobadas y al menos una iniciada.
- **Completado:** todas las tareas completadas y los criterios de éxito de la spec cumplidos.

**Avance** = tareas completadas / total de tareas del `todo.md` del módulo.

## Decisiones aprobadas

1. **Esquema por módulo.** Cada módulo define sus propias entidades, configuraciones EF y migración. `fundacion` no crea entidades. (Se aparta del paso 2 de `docs/arquitectura.md` §9, que es un orden sugerido.)
2. **Estado activo único en `Usuario` (RN-38).** `Estudiante` y `Profesor` no guardan su propio `Activo`: su estado se lee del `Usuario` asociado. Desactivar a la persona o a su cuenta es la misma operación, sin sincronización. La API sigue exponiendo el estado del estudiante o profesor (§5.2 y §5.3 de los requerimientos) como un dato derivado. Así `acceso` no depende de `estudiantes` ni de `profesores`.
3. **Consultas en el módulo dueño de los datos.** No hay un módulo de "consultas" al final. Las consultas de la sección 6 se reparten así: 1 → estudiantes; 4, 5, 7, 8, 9 → grupos; 2, 3, 6, 10 → matriculas.
4. **Guardas que dependen de módulos posteriores las añade el módulo dependiente:**
   - `grupos` añade RN-37 y "eliminar solo si nunca tuvo grupos" a profesores, cursos y aulas, y RF-37 a periodos.
   - `matriculas` añade RN-36 y la eliminación de RF-19 a estudiantes, la de RF-46 a grupos y RN-12 a la cancelación de grupos.
5. **Pruebas.** Por ahora solo pruebas unitarias de las reglas puras del dominio; los servicios y endpoints se verifican manualmente. Las pruebas de integración sobre `matricula_tests` quedan diferidas (ver `AGENTS.md` y `docs/arquitectura.md` §1).
