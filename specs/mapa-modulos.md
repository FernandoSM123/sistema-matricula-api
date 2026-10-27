# Mapa de módulos — API Sistema de Matrícula

Índice de los módulos del sistema. Cada módulo tiene su spec en `specs/SPEC-<id>.md`, su plan y sus tareas en `tasks/<id>/plan.md` y `tasks/<id>/todo.md`, y se construye en el orden indicado (Specify → Plan → Tasks → Implement). Los ids son estables: no se renombran.

| Id | Responsabilidad | RF | RN principales | Depende de |
|---|---|---|---|---|
| `fundacion` | Infraestructura transversal: mapeo de excepciones → `ProblemDetails`, CORS, convenciones de la API | — (RNF de validación e integridad) | RN-34/35 a nivel de esquema (`Restrict`) | — |
| `acceso` | `Usuario`, JWT + `SecurityStamp`, login/logout, contraseñas temporales, cambio obligatorio, olvido, cambio propio, perfil, reenvío, activar/desactivar cuentas, `Administrador` (registro, listado, detalle y edición de sus datos personales), seed del primer admin, rate limiting | RF-04…RF-14, RF-55 | RN-13, RN-20, RN-22…RN-33, RN-39 | fundacion |
| `estudiantes` | Registro público y por admin, CRUD, desactivar/reactivar/eliminar, edición de datos de contacto propios (`/api/me`) | RF-01, 02, 15…19 | RN-13, 19, 21, 31, 38, 39, 41 | acceso |
| `profesores` | Registro por admin, CRUD, desactivar/reactivar/eliminar | RF-03, 22…26 | RN-13, 31, 38, 39, 41 | acceso |
| `catalogos` | Cursos, periodos académicos (incluido el vigente) y aulas | RF-29…33, 35…40 | RN-13, 16, 39, 41 | acceso |
| `grupos` | Grupos, horarios, estados, oferta académica y consultas propias de grupos | RF-27, 28, 34, 41…48, 50 | RN-07…09, 14, 15, 17, 37 | catalogos, profesores |
| `matriculas` | Matricular, retirar, cambiar de grupo, concurrencia del cupo (`xmin`) y consultas que requieren matrículas | RF-20, 21, 49, 51…54 | RN-01…06, 10…12, 18, 36 | grupos, estudiantes |

**Orden de construcción:** `fundacion` → `acceso` → (`estudiantes`, `profesores`, `catalogos` en cualquier orden) → `grupos` → `matriculas`

## Estado de los módulos

| Módulo | Spec | Plan | Tareas | Avance | Estado del módulo |
|---|---|---|---|---|---|
| `fundacion` | [Aprobada](SPEC-fundacion.md) | [Aprobado](../tasks/fundacion/plan.md) | [Aprobadas](../tasks/fundacion/todo.md) | 6/6 | Completado |
| `acceso` | [Aprobada](SPEC-acceso.md) | [Aprobado](../tasks/acceso/plan.md) | [Aprobadas](../tasks/acceso/todo.md) | 13/13 | Completado |
| `estudiantes` | [Aprobada](SPEC-estudiantes.md) | [Aprobado](../tasks/estudiantes/plan.md) | [Aprobadas](../tasks/estudiantes/todo.md) | 6/9 | En implementación |
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

## Verificaciones heredadas

Verificaciones que un módulo no pudo hacer por falta de datos o endpoints. Se copian al `todo.md` del módulo destino cuando se crea.

| Origen | Destino | Verificación |
|---|---|---|
| `fundacion` T1 | `acceso` | Un dato duplicado que llegue a la BD (por ejemplo, el correo de `usuario`) responde `409` con `code: "RN-13"`, sin el nombre de la restricción — ✅ cumplida en `acceso` T10 (2026-10-19) |
| `fundacion` T1 | `acceso` | El `400` de validación (primer DTO con validador, por ejemplo el login) incluye `traceId` — ✅ cumplida en `acceso` T5 (2026-10-19) |
| `fundacion` T3 | primer módulo con un controller de nombre compuesto | Su ruta sale en kebab-case (por ejemplo, `/api/v1/cambio-grupo`) |
| `acceso` T10 | `estudiantes` | Un endpoint de administrador (por ejemplo, `POST /api/v1/administradores`) con el token de un estudiante responde `403`, no `401` ni `200` — ✅ cumplida en `estudiantes` T3 (2026-10-27) |

## Decisiones aprobadas

1. **Esquema por módulo.** Cada módulo define sus propias entidades, configuraciones EF y migración. `fundacion` no crea entidades. (Se aparta del paso 2 de `docs/arquitectura.md` §9, que es un orden sugerido.)
2. **Estado activo único en `Usuario` (RN-38).** `Estudiante`, `Profesor` y `Administrador` no guardan su propio `Activo`: su estado se lee del `Usuario` asociado. Desactivar a la persona o a su cuenta es la misma operación, sin sincronización. La API sigue exponiendo el estado del estudiante o profesor (§5.2 y §5.3 de los requerimientos) como un dato derivado. Así `acceso` no depende de `estudiantes` ni de `profesores`.
3. **Consultas en el módulo dueño de los datos.** No hay un módulo de "consultas" al final. Las consultas de la sección 6 se reparten así: 1 → estudiantes; 4, 5, 7, 8, 9 → grupos; 2, 3, 6, 10 → matriculas.
4. **Guardas que dependen de módulos posteriores las añade el módulo dependiente:**
   - `grupos` añade RN-37 y "eliminar solo si nunca tuvo grupos" a profesores, cursos y aulas, y RF-37 a periodos.
   - `matriculas` añade RN-36 y la eliminación de RF-19 a estudiantes, la de RF-46 a grupos y RN-12 a la cancelación de grupos.
5. **Pruebas.** Por ahora solo pruebas unitarias de las reglas puras del dominio; los servicios y endpoints se verifican manualmente. Las pruebas de integración sobre `matricula_tests` quedan diferidas (ver `AGENTS.md` y `docs/arquitectura.md` §1).
6. **Datos personales de los administradores** (2026-10-19). Los administradores tienen su propia entidad `Administrador` (cédula, nombre, apellidos, teléfono), igual que `Estudiante` y `Profesor`, ligada 1:1 a su `Usuario` (§5.4 y RF-55 de los requerimientos). La cédula es única dentro de cada tabla (`estudiante`, `profesor`, `administrador`), no entre ellas.
7. **El correo vive solo en `Usuario`** (2026-10-19, RN-31). `Estudiante`, `Profesor` y `Administrador` no tienen columna de correo: lo leen de su cuenta, y editar el correo de la persona actualiza su `Usuario`. Así no hay que sincronizar dos copias.
8. **El rol de una cuenta no cambia** (2026-10-19, RN-31). No hay cambio de rol en ningún módulo. Si una persona pasa a cumplir otro rol, se le crea una cuenta nueva (con otro correo) y el registro de persona de ese rol; la cuenta anterior se desactiva si deja de cumplirlo. RN-20 y RN-30 ya no mencionan el cambio de rol.
