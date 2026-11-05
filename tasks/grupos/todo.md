# Tareas: grupos

Módulo `grupos` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-grupos.md`](../../specs/SPEC-grupos.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-05)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones de cada flujo se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio de grupos y horarios | Pendiente | ⬜ | ⬜ | ➖ | ⬜ |
| T2. Persistencia y migración `AgregaGrupo` | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T3. Crear grupo y detalle | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T4. Horarios: agregar y quitar | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T5. Cambiar estado | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T6. Editar grupo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T7. Listado y visibilidad por rol | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T8. Oferta académica y grupos de un curso | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T9. Consultas de profesor y aula | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T10. RN-37 y RN-09 en otros módulos | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T11. Eliminar cursos, aulas y periodos | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T12. Eliminar profesores con su cuenta | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T13. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ➖ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Verificaciones heredadas

| Origen | Verificación | Tarea |
|---|---|---|
| `fundacion` T3 | La ruta de un controller de nombre compuesto sale en kebab-case: `OfertaAcademicaController` → `/api/v1/oferta-academica` | T8 |

## Detalle

### T1. Dominio de grupos y horarios
- Acceptance:
  - Enums `EstadoGrupo` (`Planificado`, `Abierto`, `Cerrado`, `EnCurso`, `Finalizado`, `Cancelado`), `Modalidad` (`Presencial`, `Virtual`, `Hibrida`) y `DiaSemana` (`Lunes` … `Domingo`).
  - `ReglasHorario` en `Domain/Reglas`: `HorasValidas(inicio, fin)` (RN-15) y `SeTraslapan(...)` (RN-14).
  - `Horario : EntidadBase` con `Dia`, `HoraInicio`, `HoraFin` y `SeTraslapaCon(otro)`; el constructor lanza `BusinessRuleException("RN-15", ...)` si las horas no son válidas.
  - `Grupo : EntidadBase` con las propiedades de la spec: nace `Planificado` con `CuposOcupados = 0`. Métodos `NormalizarNrc` (estático), `ActualizarDatos` (edición por estado, supuesto 4, `RF-45`), `CambiarEstado` (transiciones de RF-47 y abrir con bloques, `RF-47`), `AgregarHorario` (RN-14 interno, no en `Finalizado`/`Cancelado`), `QuitarHorario` (solo `Planificado`, `RF-48`) y `CuposDisponibles`. Invariante de modalidad y aula.
- Verify:
  - `ReglasHorarioTests` y `HorarioTests`: traslape parcial, contenido e idéntico; contiguos sin traslape; días distintos; fin igual o anterior al inicio.
  - `GrupoTests`: normaliza el NRC; estado y cupos iniciales; cada transición permitida y no permitida; cancelar desde cada estado salvo `Finalizado`; repetir el estado no cambia nada; abrir sin bloques falla; agregar un bloque traslapado o en un grupo finalizado o cancelado falla; quitar fuera de `Planificado` falla; edición permitida y rechazada en cada estado; el cupo no baja de `CuposOcupados`; modalidad `Virtual` con aula y `Presencial` sin aula fallan.
- Files: `src/Matricula.Domain/Enums/{EstadoGrupo,Modalidad,DiaSemana}.cs`, `src/Matricula.Domain/Reglas/ReglasHorario.cs`, `src/Matricula.Domain/Entities/{Grupo,Horario}.cs`, `tests/Matricula.UnitTests/Domain/{GrupoTests,HorarioTests,ReglasHorarioTests}.cs`

### T2. Persistencia y migración `AgregaGrupo`
- Acceptance:
  - `GrupoConfiguration`: `nrc` de 10 con índice único; enums como string; FK a curso, profesor, periodo y aula (opcional); índices `(periodo_id, profesor_id)` y `(periodo_id, aula_id)`; `Horarios` por su campo privado.
  - `HorarioConfiguration`: `dia` como string, FK a grupo, índice en `grupo_id`.
  - `DbSet<Grupo>` y `DbSet<Horario>` en `IAppDbContext` y `AppDbContext`. Migración `AgregaGrupo`.
- Verify:
  - `dotnet ef database update` aplica la migración; la salida muestra `ix_grupo_nrc` único, las FK con `ON DELETE RESTRICT` y los índices compuestos.
  - `dotnet ef migrations has-pending-model-changes` → sin cambios pendientes.
- Files: `src/Matricula.Infrastructure/Persistence/Configurations/{GrupoConfiguration,HorarioConfiguration}.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración

### T3. Crear grupo y detalle
- Acceptance:
  - `CrearGrupoRequest` + validador: NRC hasta 10, ids obligatorios, cupo mayor que 0, modalidad válida, `aulaId` según la modalidad (`400`).
  - `GrupoResponse`, `GrupoDetalleResponse` (con horarios, cupo, ocupados y disponibles) y `GrupoProyecciones`.
  - `GrupoService.CrearAsync`: `404` si no existe el curso, profesor, periodo o aula; `409 RN-17` si alguno está inactivo; `409 RN-09` si el cupo supera la capacidad; `409 RN-13` si el NRC existe.
  - `GrupoService.ObtenerAsync` → `200` o `404`.
  - `GruposController`: `POST /api/v1/grupos` (`201` + `Location`) y `GET /api/v1/grupos/{id:int}`, los dos con `Politicas.Administrador` por ahora (T7 abre el detalle). `GrupoService` registrado en `AddApplication`.
- Verify:
  - Grupo presencial y virtual válidos → `201` + `Location`, estado `Planificado`, `cuposDisponibles` igual al cupo; `Location` responde `200`.
  - NRC repetido con otra capitalización → `409 RN-13`.
  - Curso, profesor o aula inactivos → `409 RN-17`; ids inexistentes → `404`.
  - Cupo mayor que la capacidad → `409 RN-09`; igual a la capacidad → `201`.
  - Virtual con aula, presencial o híbrido sin aula, cupo 0, NRC vacío o de 11 caracteres, modalidad inexistente → `400`.
  - Con token de profesor o estudiante → `403`; sin token → `401`.
- Files: `src/Matricula.Application/Grupos/{GrupoService,CrearGrupoRequest,CrearGrupoRequestValidator,GrupoResponse,GrupoDetalleResponse,GrupoProyecciones}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/GruposController.cs`

### T4. Horarios: agregar y quitar
- Acceptance:
  - `AgregarHorarioRequest` + validador: día válido y RN-15 con `ReglasHorario` (`400`).
  - `GrupoService.AgregarHorarioAsync`: `409 RN-14` con otro bloque del grupo; `ValidarTraslapesAsync` → `409 RN-07` (profesor) o `409 RN-08` (aula) con el NRC del grupo con el que choca; excluye grupos `Cancelado`, otros periodos y el propio grupo.
  - `GrupoService.QuitarHorarioAsync`: `404` si el bloque no es del grupo; `409 RF-48` fuera de `Planificado`.
  - `POST /api/v1/grupos/{id:int}/horarios` (`201` + `Location` al detalle) y `DELETE /api/v1/grupos/{id:int}/horarios/{horarioId:int}` (`204`), con `Politicas.Administrador`.
- Verify:
  - Bloque válido → `201`; el detalle lo muestra.
  - `horaFin` igual o anterior a `horaInicio` → `400`.
  - Dos bloques traslapados en el mismo grupo → `409 RN-14`.
  - Choque con otro grupo del mismo profesor / aula en el mismo periodo → `409 RN-07` / `409 RN-08`, con el NRC.
  - Mismo choque con un grupo `Cancelado` (cancelarlo directamente en la BD hasta T5) o de otro periodo → `201`. Bloques contiguos → `201`. Grupo virtual: no se revisa el aula.
  - Quitar en `Planificado` → `204`; bloque de otro grupo → `404`. Fuera de `Planificado` se verifica en T5.
- Files: `src/Matricula.Application/Grupos/{GrupoService,AgregarHorarioRequest,AgregarHorarioRequestValidator}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`

### T5. Cambiar estado
- Acceptance:
  - `CambiarEstadoRequest` + validador (estado válido).
  - `GrupoService.CambiarEstadoAsync` con `Grupo.CambiarEstado`; repetir el estado → sin cambios.
  - `POST /api/v1/grupos/{id:int}/estado` (`204`) con `Politicas.Administrador`.
- Verify:
  - Planificado → Abierto → Cerrado → En curso → Finalizado → `204` en cada paso.
  - Saltar un estado, retroceder o cancelar un grupo finalizado → `409 RF-47`. Abrir sin bloques → `409 RF-47` con su mensaje.
  - Cancelar desde `Planificado`, `Abierto`, `Cerrado` y `EnCurso` → `204`.
  - Repetir el estado actual → `204`.
  - Pendiente de T4: quitar un bloque de un grupo `Abierto` → `409 RF-48`; agregar a un grupo `Finalizado` o `Cancelado` → `409`.
  - ⏳ RN-12 al cancelar la añade `matriculas`.
- Files: `src/Matricula.Application/Grupos/{GrupoService,CambiarEstadoRequest,CambiarEstadoRequestValidator}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`

### T6. Editar grupo
- Acceptance:
  - `ActualizarGrupoRequest` + validador con las reglas de crear.
  - `GrupoService.ActualizarAsync`: NRC de otro grupo → `409 RN-13`; curso, profesor o aula nuevos e inactivos → `409 RN-17`; cupo sobre la capacidad → `409 RN-09`; nuevo profesor o aula con choque → `409 RN-07` / `409 RN-08` (con `ValidarTraslapesAsync` sobre todos los bloques del grupo); reglas por estado en el dominio → `409 RF-45`.
  - `PUT /api/v1/grupos/{id:int}` (`200` con el detalle) con `Politicas.Administrador`.
- Verify:
  - Editar un grupo `Planificado` cambiando todo → `200`; conservar NRC, profesor y aula → `200` (no choca consigo mismo).
  - En `Abierto`: cambiar profesor, aula, modalidad y cupo → `200`; cambiar curso, periodo o NRC → `409 RF-45`.
  - En `Finalizado` o `Cancelado`: cualquier cambio → `409 RF-45`.
  - Nuevo profesor o aula con choque → `409 RN-07` / `409 RN-08`; nuevo profesor inactivo → `409 RN-17`; cupo sobre la capacidad del aula nueva → `409 RN-09`.
  - Pasar a `Virtual` quitando el aula → `200`; virtual con aula → `400`.
- Files: `src/Matricula.Application/Grupos/{GrupoService,ActualizarGrupoRequest,ActualizarGrupoRequestValidator}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`

### T7. Listado y visibilidad por rol
- Acceptance:
  - `ListarGruposRequest` (hereda `PaginacionRequest`): `cursoId`, `profesorId`, `periodoId`, `estado`, `modalidad`, `search` (NRC).
  - `GrupoService.ListarAsync`: orden por `InicioClases` descendente, nombre del curso, NRC e `Id`; `Select` y `AsNoTracking`. Profesor → `profesorId` fijado al suyo; estudiante → `estado = Abierto` (RN-18, supuesto 7).
  - `ObtenerAsync` con visibilidad: profesor con grupo ajeno o estudiante con grupo no abierto → `403 RN-18`.
  - `GET /api/v1/grupos` y `GET /api/v1/grupos/{id:int}` con `[Authorize]`.
- Verify:
  - Cada filtro, búsqueda por NRC y paginación como administrador.
  - Profesor: solo sus grupos aunque envíe otro `profesorId`; detalle ajeno → `403 RN-18`.
  - Estudiante: solo `Abierto` aunque envíe otro `estado`; detalle de un grupo `Planificado` → `403 RN-18`.
  - Sin token → `401`; con cambio pendiente → `403`.
- Files: `src/Matricula.Application/Grupos/{GrupoService,ListarGruposRequest}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`

### T8. Oferta académica y grupos de un curso
- Acceptance:
  - `PeriodoService`: método que devuelve el id del periodo vigente o `null`; `ObtenerVigenteAsync` lo reutiliza.
  - `OfertaAcademicaService` y `OfertaAcademicaResponse` (curso, NRC, profesor solo con nombre, modalidad, aula, horarios, cupos disponibles). Filtros `cursoId`, `modalidad`, `search` (curso o NRC). Sin vigente → página vacía.
  - `OfertaAcademicaController` con `[AllowAnonymous]`: `GET /api/v1/oferta-academica`.
  - `GET /api/v1/cursos/{id:int}/grupos` (`[Authorize]`): `404` si el curso no existe; reutiliza `ListarAsync` con `cursoId` fijado y `estado = Abierto` para profesor y estudiante.
- Verify:
  - Sin token, con periodo vigente → `200` con los abiertos y sus disponibles, incluidos los llenos; sin vigente → `200` vacío.
  - La respuesta no incluye correo, teléfono ni cédula del profesor.
  - Grupos de otro periodo o en otro estado no aparecen.
  - ✅ Verificación heredada de `fundacion` T3: la ruta es `/api/v1/oferta-academica`.
  - `/cursos/{id}/grupos`: administrador filtra por cualquier estado y periodo; profesor y estudiante solo reciben `Abierto`; curso inexistente → `404`.
- Files: `src/Matricula.Application/Periodos/PeriodoService.cs`, `src/Matricula.Application/Grupos/Consultas/{OfertaAcademicaService,OfertaAcademicaResponse,ListarOfertaRequest}.cs`, `src/Matricula.Api/Controllers/{OfertaAcademicaController,CursosController}.cs`

### T9. Consultas de profesor y aula
- Acceptance:
  - `Politicas.Profesor`, combinada con `conSesionSinCambioPendiente`.
  - `HorarioSemanalService` y `BloqueHorarioResponse`: horario de un profesor y ocupación de un aula en un periodo; sin `periodoId` usa el vigente, y sin vigente → `404` pidiendo el `periodoId`. Excluye cancelados; orden por día y hora de inicio.
  - `GET /api/v1/profesores/{id:int}/grupos` y `/horario` (`Politicas.Administrador` de clase); `GET /api/v1/me/grupos` y `/me/horario` (`Politicas.Profesor`, con `ICurrentUser.ProfesorId`); `GET /api/v1/aulas/{id:int}/ocupacion` (`Politicas.Administrador`).
- Verify:
  - `/profesores/{id}/grupos` con y sin `periodoId`; profesor inexistente → `404`.
  - `/me/horario` y `/profesores/{id}/horario` dan lo mismo para el mismo profesor; sin cancelados; ordenado.
  - `/aulas/{id}/ocupacion` con `periodoId`; sin `periodoId` y sin vigente → `404` con el mensaje.
  - `/me/grupos` con token de estudiante o administrador → `403`.
  - ⏳ `matriculas` amplía `/me/horario` a estudiantes (RF-21).
- Files: `src/Matricula.Api/Seguridad/Politicas.cs`, `src/Matricula.Application/Grupos/Consultas/{HorarioSemanalService,BloqueHorarioResponse}.cs`, `src/Matricula.Api/Controllers/{ProfesoresController,MeController,AulasController}.cs`

### T10. RN-37 y RN-09 en otros módulos
- Acceptance:
  - `UsuarioService.DesactivarAsync`: cuenta con rol Profesor y grupos `Planificado`, `Abierto`, `Cerrado` o `EnCurso` → `409 RN-37` con la cantidad.
  - `CursoService.DesactivarAsync` y `AulaService.DesactivarAsync`: la misma guarda.
  - `AulaService.ActualizarAsync`: capacidad menor que el mayor cupo de sus grupos no `Finalizado` ni `Cancelado` → `409 RN-09`.
- Verify:
  - Desactivar un profesor por `/profesores/{id}/desactivar` y por `/usuarios/{id}/desactivar`, un curso y un aula con grupos activos → `409 RN-37`.
  - Con todos sus grupos `Finalizado` o `Cancelado` → `204`.
  - Regresión: desactivar un estudiante y un administrador sigue funcionando (RN-30 incluida).
  - Reducir la capacidad por debajo del cupo de un grupo → `409 RN-09`; hasta el cupo exacto → `200`.
- Files: `src/Matricula.Application/Cuentas/UsuarioService.cs`, `src/Matricula.Application/Cursos/CursoService.cs`, `src/Matricula.Application/Aulas/AulaService.cs`

### T11. Eliminar cursos, aulas y periodos
- Acceptance:
  - `EliminarAsync` en `CursoService`, `AulaService` y `PeriodoService`: con grupos → `409 RN-34` con un mensaje que sugiere desactivar (o corregir, en el periodo); sin grupos → elimina.
  - `DELETE /api/v1/cursos/{id:int}`, `/aulas/{id:int}` y `/periodos/{id:int}` (`204`) con `Politicas.Administrador`.
- Verify:
  - Sin grupos → `204` y el detalle responde `404`.
  - Con grupos, incluidos cancelados → `409 RN-34`; nunca `500`.
  - Id inexistente → `404`. Profesor o estudiante → `403`.
  - Eliminado un curso o aula, su código se puede registrar de nuevo → `201`.
- Files: `src/Matricula.Application/{Cursos/CursoService,Aulas/AulaService,Periodos/PeriodoService}.cs`, `src/Matricula.Api/Controllers/{CursosController,AulasController,PeriodosController}.cs`

### T12. Eliminar profesores con su cuenta
- Acceptance:
  - `ProfesorService.EliminarAsync`: con grupos → `409 RN-34`; sin grupos → `Remove` del profesor y de su `Usuario` en un solo `SaveChangesAsync` (decisión 4 de la spec, RN-35).
  - `DELETE /api/v1/profesores/{id:int}` (`204`).
- Verify:
  - Profesor sin grupos → `204`; `GET /profesores/{id}` → `404`; la cuenta ya no existe.
  - Su token vigente → `401`.
  - Su correo y su cédula se registran de nuevo → `201`.
  - Con grupos, incluidos cancelados → `409 RN-34`. Id inexistente → `404`.
- Files: `src/Matricula.Application/Profesores/ProfesorService.cs`, `src/Matricula.Api/Controllers/ProfesoresController.cs`

### T13. Cierre del módulo
- Acceptance:
  - `Matricula.Api.http` con el recorrido completo de la spec.
  - Recorrido de la tabla de verificación manual de la spec, con los tres roles, sin token y con cambio pendiente.
  - Estados de la spec, el mapa y esta lista al día; la verificación heredada de `fundacion` T3 marcada como cumplida en el mapa.
- Verify: prueba de humo final con datos nuevos sobre toda la tabla de verificación de la spec; `dotnet build` sin warnings; pruebas unitarias en verde.
- Files: `src/Matricula.Api/Matricula.Api.http`, `specs/mapa-modulos.md`, `specs/SPEC-grupos.md`, `tasks/grupos/todo.md`
