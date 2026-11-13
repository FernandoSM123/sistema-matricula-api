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
| `auditoria` | Registro automático de cada creación, modificación y eliminación de datos (`registro_auditoria`) en el mismo guardado, con autor, fecha, cambios antes/después e identificador de operación; exclusión de secretos y datos técnicos; consulta paginada solo para el administrador, con filtros por entidad, registro, usuario, acción y rango de fechas (`GET /api/v1/auditoria`) | RF-56, RF-57 | RN-42…RN-46 | acceso (y audita las entidades de todos los módulos) |
| `errores` | Registro automático de los errores inesperados (`registro_error`): los `500` del manejador global y los envíos de correo fallidos, en un guardado aparte que nunca afecta la respuesta; sin datos secretos ni contenido de las peticiones; depuración automática a los 90 días (configurable); consulta paginada y detalle solo para el administrador (`GET /api/v1/errores`, `GET /api/v1/errores/{id}`) | RF-58, RF-59 | RN-47…RN-49 | acceso, auditoria (comparte el identificador de operación) |

**Orden de construcción:** `fundacion` → `acceso` → (`estudiantes`, `profesores`, `catalogos` en cualquier orden) → `grupos` → `matriculas` → `auditoria` → `errores`

## Estado de los módulos

| Módulo | Spec | Plan | Tareas | Avance | Estado del módulo |
|---|---|---|---|---|---|
| `fundacion` | [Aprobada](SPEC-fundacion.md) | [Aprobado](../tasks/fundacion/plan.md) | [Aprobadas](../tasks/fundacion/todo.md) | 6/6 | Completado |
| `acceso` | [Aprobada](SPEC-acceso.md) | [Aprobado](../tasks/acceso/plan.md) | [Aprobadas](../tasks/acceso/todo.md) | 13/13 | Completado |
| `estudiantes` | [Aprobada](SPEC-estudiantes.md) | [Aprobado](../tasks/estudiantes/plan.md) | [Aprobadas](../tasks/estudiantes/todo.md) | 9/9 | Completado |
| `profesores` | [Aprobada](SPEC-profesores.md) | [Aprobado](../tasks/profesores/plan.md) | [Aprobadas](../tasks/profesores/todo.md) | 7/7 | Completado |
| `catalogos` | [Aprobada](SPEC-catalogos.md) | [Aprobado](../tasks/catalogos/plan.md) | [Aprobadas](../tasks/catalogos/todo.md) | 10/10 | Completado |
| `grupos` | [Aprobada](SPEC-grupos.md) | [Aprobado](../tasks/grupos/plan.md) | [Aprobadas](../tasks/grupos/todo.md) | 13/13 | Completado |
| `matriculas` | [Aprobada](SPEC-matriculas.md) | [Aprobado](../tasks/matriculas/plan.md) | [Aprobadas](../tasks/matriculas/todo.md) | 13/13 | Completado |
| `auditoria` | [Aprobada](SPEC-auditoria.md) | [Aprobado](../tasks/auditoria/plan.md) | [Aprobadas](../tasks/auditoria/todo.md) | 5/5 | Completado |
| `errores` | [Aprobada](SPEC-errores.md) | [Aprobado](../tasks/errores/plan.md) | [Aprobadas](../tasks/errores/todo.md) | 2/6 | En implementación |

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
| `fundacion` T3 | primer módulo con un controller de nombre compuesto | Su ruta sale en kebab-case (por ejemplo, `/api/v1/cambio-grupo`) — ✅ cumplida en `grupos` T8 (2026-11-05): `/api/v1/oferta-academica` |
| `acceso` T10 | `estudiantes` | Un endpoint de administrador (por ejemplo, `POST /api/v1/administradores`) con el token de un estudiante responde `403`, no `401` ni `200` — ✅ cumplida en `estudiantes` T3 (2026-10-27) |
| `grupos` T1 | `matriculas` | Una prueba unitaria de que `Grupo.ActualizarDatos` rechaza un cupo menor que `CuposOcupados` (`409 RF-45`), cuando exista el método que incrementa `CuposOcupados` — ✅ cumplida en `matriculas` T1 (2026-11-10) |
| `grupos` T8 | `matriculas` | La oferta académica incluye los grupos `Abierto` sin cupos disponibles (`cuposDisponibles = 0`) — ✅ cumplida en `matriculas` T3 (2026-11-10) |

## Decisiones aprobadas

1. **Esquema por módulo.** Cada módulo define sus propias entidades, configuraciones EF y migración. `fundacion` no crea entidades. (Se aparta del paso 2 de `docs/arquitectura.md` §9, que es un orden sugerido.)
2. **Estado activo único en `Usuario` (RN-38).** `Estudiante`, `Profesor` y `Administrador` no guardan su propio `Activo`: su estado se lee del `Usuario` asociado. Desactivar a la persona o a su cuenta es la misma operación, sin sincronización. La API sigue exponiendo el estado del estudiante o profesor (§5.2 y §5.3 de los requerimientos) como un dato derivado. Así `acceso` no depende de `estudiantes` ni de `profesores`.
3. **Consultas en el módulo dueño de los datos.** No hay un módulo de "consultas" al final. Las consultas de la sección 6 se reparten así: 1 → estudiantes; 4, 5, 7, 8, 9 → grupos; 2, 3, 6, 10 → matriculas.
4. **Guardas que dependen de módulos posteriores las añade el módulo dependiente:**
   - `grupos` añade RN-37 y "eliminar solo si nunca tuvo grupos" a profesores, cursos y aulas, y RF-37 a periodos. También impide reducir la capacidad de un aula por debajo del cupo de sus grupos (RN-09).
   - `matriculas` añade RN-36 y la eliminación de RF-19 a estudiantes, la de RF-46 a grupos y RN-12 a la cancelación de grupos.
5. **Pruebas.** Por ahora solo pruebas unitarias de las reglas puras del dominio; los servicios y endpoints se verifican manualmente. Las pruebas de integración sobre `matricula_tests` quedan diferidas (ver `AGENTS.md` y `docs/arquitectura.md` §1).
6. **Datos personales de los administradores** (2026-10-19). Los administradores tienen su propia entidad `Administrador` (cédula, nombre, apellidos, teléfono), igual que `Estudiante` y `Profesor`, ligada 1:1 a su `Usuario` (§5.4 y RF-55 de los requerimientos). La cédula es única dentro de cada tabla (`estudiante`, `profesor`, `administrador`), no entre ellas.
7. **El correo vive solo en `Usuario`** (2026-10-19, RN-31). `Estudiante`, `Profesor` y `Administrador` no tienen columna de correo: lo leen de su cuenta, y editar el correo de la persona actualiza su `Usuario`. Así no hay que sincronizar dos copias.
8. **El rol de una cuenta no cambia** (2026-10-19, RN-31). No hay cambio de rol en ningún módulo. Si una persona pasa a cumplir otro rol, se le crea una cuenta nueva (con otro correo) y el registro de persona de ese rol; la cuenta anterior se desactiva si deja de cumplirlo. RN-20 y RN-30 ya no mencionan el cambio de rol.
9. **"Hoy" es la fecha local del instituto** (2026-11-02). Las comparaciones con fechas sin hora (periodo vigente, RN-04, RN-10, fecha de registro, fecha de nacimiento) usan `IFechaLocal.Hoy()`, que aplica la zona de `Instituto:ZonaHoraria` (`America/Costa_Rica`). Ningún módulo calcula la fecha de hoy desde UTC. Las marcas de tiempo siguen en UTC. Se implementa en `catalogos`.
10. **Eliminar un estudiante o profesor elimina también su cuenta** (2026-11-05, RN-35). Solo cuando la persona se puede eliminar por no tener datos relacionados (sin matrículas o sin grupos), y en el mismo guardado, sin cascada. Una cuenta nunca se elimina por sí sola, y la de un administrador nunca. Así no quedan cuentas sin persona (RN-31) y el correo se puede volver a registrar. Se corrigieron RF-14, RF-19, RF-26, RN-35 y la tabla de §4.2. Lo implementa `grupos` para profesores y `matriculas` para estudiantes.
11. **Auditoría de cambios de datos** (2026-11-10, RF-56, RF-57, RN-42 a RN-46). El punto "Auditoría detallada de cambios" sale de "Fuera del alcance" y pasa a ser el módulo `auditoria`, con estas decisiones:
    - **Solo cambios de datos.** Los eventos que no cambian datos (logins fallidos, logout, envío de correos) quedan fuera del alcance.
    - **Captura genérica** en `AppDbContext.SaveChangesAsync`, con la auditoría y el cambio en la misma transacción (`docs/arquitectura.md` §5.1). Ningún servicio registra la auditoría a mano, y queda prohibido escribir con `ExecuteUpdate`, `ExecuteDelete` o SQL manual.
    - **Sin FK** desde `registro_auditoria`, para no bloquear las eliminaciones de RN-35. Los registros se conservan después de eliminar el dato y nunca se depuran.
    - **Un único endpoint de consulta** para el administrador, con filtros combinables por entidad, registro, usuario, acción y rango de fechas.
    - El interceptor actual `AuditoriaInterceptor`, que solo asigna fechas, se renombra (por ejemplo, a `FechasInterceptor`) para no confundirlo con la auditoría.
    - No requiere paquetes NuGet nuevos.
    - **Inmutable solo en la aplicación** (RN-43): Application lee los registros como `IQueryable` y no hay endpoints que los modifiquen. La BD no los protege; se acepta que alguien con acceso directo pueda alterarlos.
12. **Registro de errores inesperados** (2026-11-13, RF-58, RF-59, RN-47 a RN-49). Nuevo módulo `errores`, que amplía el alcance de §1.3 de los requerimientos. Diseño en `docs/arquitectura.md` §5.2. Se decidió:
    - **Qué se registra:** los `500` no controlados (rama final de `GlobalExceptionHandler`) y los fallos de envío de correo (`EnvioCorreos`). No se registran los `4xx` ni los errores al arrancar (seed, migraciones), que quedan solo en el log de consola.
    - **Un registro por ocurrencia**, sin agrupar errores iguales, para conservar el identificador de operación de cada uno y poder buscar el error que reporta un usuario.
    - **Sin estado** (`Pendiente`/`Revisado`): el módulo solo permite ver los errores.
    - **Conservación de 90 días**, configurable con `Errores:DiasRetencion`. Un `BackgroundService` depura al arrancar y cada 24 horas. A diferencia de la auditoría, estos registros sí se eliminan.
    - **La depuración usa `ExecuteDeleteAsync`**, como única excepción a la prohibición de escribir fuera del change tracker: la tabla no se audita y así no se cargan los registros en memoria. Queda acotada a `registro_error` en la arquitectura (§5) y, al implementar el módulo, en AGENTS.md.
    - **Guardado aparte** en un scope y un `AppDbContext` nuevos, nunca en la transacción de la petición, y sin lanzar excepciones: si el registro falla, queda solo el log (RN-48).
    - No requiere paquetes NuGet nuevos.
