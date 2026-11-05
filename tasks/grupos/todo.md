# Tareas: grupos

Módulo `grupos` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-grupos.md`](../../specs/SPEC-grupos.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-05)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones de cada flujo se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio de grupos y horarios | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia y migración `AgregaGrupo` | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Crear grupo y detalle | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Horarios: agregar y quitar | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. Cambiar estado | Completada | ✅ | ➖ | ⏳ | ✅ |
| T6. Editar grupo | Completada | ✅ | ➖ | ✅ | ✅ |
| T7. Listado y visibilidad por rol | Completada | ✅ | ➖ | ✅ | ✅ |
| T8. Oferta académica y grupos de un curso | Completada | ✅ | ➖ | ⏳ | ✅ |
| T9. Consultas de profesor y aula | Completada | ✅ | ➖ | ⏳ | ✅ |
| T10. RN-37 y RN-09 en otros módulos | Completada | ✅ | ➖ | ✅ | ✅ |
| T11. Eliminar cursos, aulas y periodos | Completada | ✅ | ➖ | ✅ | ✅ |
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
| `fundacion` T3 | La ruta de un controller de nombre compuesto sale en kebab-case: `OfertaAcademicaController` → `/api/v1/oferta-academica` | T8 — ✅ cumplida (2026-11-05) |

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
  - Resultado (2026-11-05): `dotnet build` sin warnings; 219/219 pruebas unitarias en verde (83 nuevas). Detalles que la spec no fijaba:
    - `Grupo` lleva navegaciones a `Curso`, `Profesor`, `Periodo` y `Aula` para las proyecciones de T3 en adelante.
    - El invariante modalidad/aula usa el código `RN-09`, que es la regla que dice que en virtual no aplica el aula.
    - `QuitarHorario` devuelve el bloque quitado, o `null` si no es del grupo. El servicio de T4 debe hacer además `db.Horarios.Remove`, porque con FK `Restrict` EF no borra el huérfano.
    - ⏳ "El cupo no baja de `CuposOcupados`" está en `ActualizarDatos`, pero no tiene prueba: `CuposOcupados` siempre es 0 hasta que `matriculas` agregue el método que lo incrementa. La prueba se agrega entonces.
- Files: `src/Matricula.Domain/Enums/{EstadoGrupo,Modalidad,DiaSemana}.cs`, `src/Matricula.Domain/Reglas/ReglasHorario.cs`, `src/Matricula.Domain/Entities/{Grupo,Horario}.cs`, `tests/Matricula.UnitTests/Domain/{GrupoTests,HorarioTests,ReglasHorarioTests}.cs`

### T2. Persistencia y migración `AgregaGrupo`
- Acceptance:
  - `GrupoConfiguration`: `nrc` de 10 con índice único; enums como string; FK a curso, profesor, periodo y aula (opcional); índices `(periodo_id, profesor_id)` y `(periodo_id, aula_id)`; `Horarios` por su campo privado.
  - `HorarioConfiguration`: `dia` como string, FK a grupo, índice en `grupo_id`.
  - `DbSet<Grupo>` y `DbSet<Horario>` en `IAppDbContext` y `AppDbContext`. Migración `AgregaGrupo`.
- Verify:
  - `dotnet ef database update` aplica la migración; la salida muestra `ix_grupo_nrc` único, las FK con `ON DELETE RESTRICT` y los índices compuestos.
  - `dotnet ef migrations has-pending-model-changes` → sin cambios pendientes.
  - Resultado (2026-11-05): `AgregaGrupo` aplicada a la BD de desarrollo y sin cambios de modelo pendientes. La salida de `dotnet ef database update` muestra `CREATE UNIQUE INDEX ix_grupo_nrc`, las cinco FK con `ON DELETE RESTRICT` (grupo → curso, profesor, periodo y aula; horario → grupo), `ix_grupo_periodo_id_profesor_id`, `ix_grupo_periodo_id_aula_id` e `ix_horario_grupo_id`. 219/219 pruebas unitarias en verde.
  - Cambio fuera de la lista de archivos: la migración generada no compilaba por CA1861 (arrays de los índices compuestos, la primera migración que los tiene). En `.editorconfig` se marcó `**/Persistence/Migrations/*.cs` como `generated_code = true`, así los analizadores no revisan código generado por EF y no hay que editarlo a mano.
- Files: `src/Matricula.Infrastructure/Persistence/Configurations/{GrupoConfiguration,HorarioConfiguration}.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración, `.editorconfig`

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
  - Resultado (2026-11-05): 34/34 verificaciones correctas con un script contra la API local y tokens reales de administrador, profesor, estudiante y de una cuenta con cambio pendiente (`403`); los datos de prueba se crearon con correos `@prueba.local`. También se comprobó: híbrido con aula y cupo igual a la capacidad → `201`; un virtual con cupo 100 no se limita por aula; el detalle trae los datos del curso, profesor, periodo y aula; el cuerpo vacío → `400` con un mensaje por campo.
  - Detalles que la spec no fijaba:
    - Las reglas de formato viven en `DatosGrupoValidator` con `IDatosGrupo`, como `DatosPeriodoValidator`, para que T6 las reutilice. Ids, cupo y modalidad son anulables en el DTO para que un dato omitido responda `400`.
    - `HorarioResponse` es un archivo aparte. Los horarios del detalle se ordenan en memoria (`OrdenarHorarios`), porque el día se guarda como texto y en SQL quedaría en orden alfabético.
    - `ValidarReferenciasAsync` ya recibe el grupo actual para que T6 revise RN-17 solo con lo que cambia.
- Files: `src/Matricula.Application/Grupos/{GrupoService,IDatosGrupo,DatosGrupoValidator,CrearGrupoRequest,CrearGrupoRequestValidator,GrupoResponse,GrupoDetalleResponse,HorarioResponse,GrupoProyecciones}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

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
  - Resultado (2026-11-05): 34/34 verificaciones correctas contra la API local. Además de la lista: el bloque rechazado no se guarda; los horarios del detalle salen de lunes a domingo y por hora; se aceptan `HH:mm` y `HH:mm:ss`; `25:00` → `400`; el mismo profesor y aula en un periodo traslapado con otro no chocan (supuesto 3); quitar un bloque ya quitado → `404`; profesor y estudiante → `403`, sin token → `401`.
  - Pendiente para T5: el choque con un grupo `Cancelado` → `201`. No se pudo cancelar en la BD (`psql` pide una contraseña que no está disponible), así que se verifica con el endpoint de estado.
  - Detalle de implementación: `ValidarTraslapesAsync` trae en una consulta los bloques de los mismos días de los otros grupos no cancelados del periodo que comparten profesor o aula, y decide el traslape en memoria con `ReglasHorario.SeTraslapan`, la misma función que usa `Horario.SeTraslapaCon`.
- Files: `src/Matricula.Application/Grupos/{GrupoService,AgregarHorarioRequest,AgregarHorarioRequestValidator}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

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
  - Pendiente de T4: quitar un bloque de un grupo `Abierto` → `409 RF-48`; agregar a un grupo `Finalizado` o `Cancelado` → `409`; un bloque que choca con un grupo `Cancelado` del mismo profesor o aula → `201`.
  - ⏳ RN-12 al cancelar la añade `matriculas`.
  - Resultado (2026-11-05): 40/40 verificaciones correctas contra la API local, incluidas las pendientes de T4. Además de la lista: el detalle refleja cada estado; un cambio rechazado no altera el estado; un cancelado no vuelve a ningún estado; agregar un bloque a un grupo `Abierto` → `201` (supuesto 6); estado inexistente u omitido → `400`; grupo inexistente → `404`; profesor y estudiante → `403`, sin token → `401`. El choque con un grupo `Cancelado` se comprobó con un control: el mismo bloque responde `409 RN-07` mientras el otro grupo está activo y `201` después de cancelarlo.
- Files: `src/Matricula.Application/Grupos/{GrupoService,CambiarEstadoRequest,CambiarEstadoRequestValidator}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

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
  - Resultado (2026-11-05): 45/45 verificaciones correctas contra la API local. Además de la lista: el cambio se guarda y el detalle lo refleja; NRC de otro grupo con otra capitalización → `409 RN-13`; en `Abierto`, el mismo NRC en minúsculas no cuenta como cambio → `200`; `Cerrado` y `EnCurso` se editan como `Abierto`; `Finalizado` responde `409 RF-45` aunque no cambie nada; el mensaje de RN-07/RN-08 trae el NRC y el grupo rechazado no cambia; pasar a otro periodo evita el choque; curso o aula nuevos inactivos → `409 RN-17`, pero un aula que el grupo ya tenía y se desactivó después no bloquea la edición; cupo igual a la capacidad → `200`; ids inexistentes → `404`; cupo 0, NRC de 11, presencial sin aula y cuerpo vacío → `400`; profesor y estudiante → `403`, sin token → `401`.
  - Orden en `ActualizarAsync`: referencias (404, RN-17, RN-09) antes de modificar el grupo, porque RN-17 compara con las referencias que tenía; luego `ActualizarDatos` (RF-45), RN-13 y los traslapes con todos los bloques, el periodo, el profesor y el aula que quedan.
  - El caso "aula que se desactivó después" se pudo armar porque RN-37 aún no existe; desde T10 ya no se podrá desactivar un aula con grupos activos.
- Files: `src/Matricula.Application/Grupos/{GrupoService,ActualizarGrupoRequest,ActualizarGrupoRequestValidator}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

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
  - Resultado (2026-11-05): 30/30 verificaciones correctas contra la API local con tokens reales de administrador, profesor, estudiante y de una cuenta con cambio pendiente. Además de la lista: el orden es periodo más reciente, nombre del curso y NRC; la página trae `totalCount` y `totalPages`; la búsqueda por NRC no distingue mayúsculas y acepta parte del NRC; el administrador ve los cancelados si los filtra y el estudiante no; el profesor ve el detalle de sus grupos en cualquier estado; un grupo inexistente responde `404` también al profesor; estado inexistente en el filtro → `400`; las escrituras siguen solo para el administrador.
  - La visibilidad se resuelve en el servicio con `ICurrentUser`: el listado sustituye `profesorId` o `estado` según el rol, y `ObtenerAsync` revisa el detalle ya proyectado. Un token de profesor sin el claim `profesor_id` responde `403` pidiendo iniciar sesión de nuevo, como en `estudiantes`.
- Files: `src/Matricula.Application/Grupos/{GrupoService,ListarGruposRequest}.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

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
  - Resultado (2026-11-05): 32/32 verificaciones correctas contra la API local. Como la BD de desarrollo no tenía periodo vigente, el script creó uno que abarca hoy, verificó con él y al final lo movió a 2041 con `PUT /periodos/{id}`; la BD queda sin periodo vigente, como estaba. Además de la lista: la oferta trae curso, NRC, nombre del profesor, modalidad, aula, cupo, disponibles y los horarios de lunes a domingo; no aparecen los `Planificado`, `Cerrado`, `Cancelado` ni los de otro periodo; `search` encuentra por nombre o código del curso y por NRC sin distinguir mayúsculas; el orden es curso y NRC; con token también responde; modalidad inexistente → `400`. En `/cursos/{id}/grupos`, un profesor que no da el curso ve los `Abierto` del curso, no solo los suyos (supuesto 7); estado inexistente o curso sin grupos → `200`; sin token → `401`. `GET /periodos/vigente` sigue respondiendo `200` con el periodo vigente y `404` sin él tras el cambio en `PeriodoService`.
  - ✅ Verificación heredada de `fundacion` T3: `GET /api/v1/oferta-academica` → `200`; `/api/v1/ofertaacademica` → `404` con token. Sin token cualquier ruta desconocida responde `401`, porque la API exige sesión por defecto.
  - ⏳ "Incluidos los llenos": la consulta no filtra por disponibles, pero no se puede llenar un grupo hasta que `matriculas` incremente `CuposOcupados`. Anotado en las verificaciones heredadas del mapa.
  - Detalles que la spec no fijaba:
    - `GrupoService` separa los filtros comunes en `ConsultarAsync`; `ListarAsync` y `ListarPorCursoAsync` solo deciden la visibilidad. En `/cursos/{id}/grupos` el filtro es `ListarGruposCursoRequest` (`periodoId`, `estado`, `search` por NRC), como en la tabla de endpoints de la spec.
    - `OfertaAcademicaResponse` lleva `grupoId` y `cupo` además de los disponibles.
- Files: `src/Matricula.Application/Periodos/PeriodoService.cs`, `src/Matricula.Application/Grupos/Consultas/{OfertaAcademicaService,OfertaAcademicaResponse,ListarOfertaRequest}.cs`, `src/Matricula.Application/Grupos/{GrupoService,ListarGruposCursoRequest}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/{OfertaAcademicaController,CursosController}.cs`, `src/Matricula.Api/Matricula.Api.http`

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
  - Resultado (2026-11-05): 37/37 verificaciones correctas contra la API local. Además de la lista: los grupos de un profesor incluyen todos los estados (también `Cancelado`) y aceptan `search` por NRC; `/me/grupos` da lo mismo que `/profesores/{id}/grupos` y otro profesor solo ve los suyos; el horario y la ocupación excluyen los cancelados y los de otros periodos, y la ocupación junta los bloques de todos los profesores del aula; cada bloque trae grupo, NRC, curso, profesor, aula, modalidad y estado; `periodoId` inexistente, profesor o aula inexistentes → `404`. Sin `periodoId` y con un periodo vigente, `/me/horario` y `/aulas/{id}/ocupacion` usan el vigente: el script lo creó y al final lo movió a 2043, así que la BD sigue sin periodo vigente. Acceso: `/me/grupos` y `/me/horario` con estudiante, administrador o cambio pendiente → `403`, sin token → `401`; los de `/profesores` y `/aulas/{id}/ocupacion` con profesor → `403`; `GET /me` sigue abierto a cualquier rol.
  - Detalles que la spec no fijaba:
    - Los grupos de un profesor se listan en `GrupoService` (`ListarDeProfesorAsync`, `ListarMisGruposAsync`) con el mismo `ConsultarAsync` de T7 y T8, y el filtro `ListarGruposProfesorRequest` (`periodoId`, `search` por NRC). Sin `periodoId` se listan todos los periodos; solo el horario y la ocupación usan el vigente por defecto, como dice la spec.
    - `BloqueHorarioResponse` trae a la vez profesor y aula, así sirve igual para el horario del profesor y la ocupación del aula. El orden se desempata por NRC solo para que sea estable: RN-07 y RN-08 impiden dos bloques a la misma hora del mismo profesor o aula en un periodo.
    - Un `periodoId` que no existe responde `404` en vez de una lista vacía.
- Files: `src/Matricula.Api/Seguridad/Politicas.cs`, `src/Matricula.Application/Grupos/Consultas/{HorarioSemanalService,BloqueHorarioResponse}.cs`, `src/Matricula.Application/Grupos/{GrupoService,ListarGruposProfesorRequest}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Controllers/{ProfesoresController,MeController,AulasController}.cs`, `src/Matricula.Api/Matricula.Api.http`

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
  - Resultado (2026-11-05): 33/33 verificaciones correctas contra la API local. RN-37 responde `409` con la cantidad de grupos (profesor) o el código (curso, aula), con grupos en cada estado activo: `Planificado`, `Abierto`, `Cerrado` y `EnCurso`, y por las dos rutas del profesor; el registro sigue activo. Con sus grupos `Finalizado` o `Cancelado`, o sin grupos → `204`; repetir → `204`; reactivar un profesor sigue funcionando. RN-09: el mensaje nombra el grupo de mayor cupo; un grupo `Finalizado` con más cupo no cuenta; subir la capacidad o editar otros datos → `200`. Regresión: desactivar y reactivar un estudiante y desactivar otro administrador → `204`.
  - No se ejecutó la regresión de RN-30: la BD de desarrollo tiene 9 administradores activos y la prueba habría desactivado la cuenta real. La guarda de RN-30 no se tocó (la de RN-37 va después, solo para la cuenta `Profesor`) y ya se verificó en `acceso`.
  - Detalles que la spec no fijaba:
    - `GuardasGrupos` (en `Application/Grupos`) define qué es un grupo activo para RN-37 y RN-09 (ni `Finalizado` ni `Cancelado`) y la comprobación de RN-37 que usan los tres servicios.
    - Curso y aula solo revisan RN-37 si están activos, para que repetir la desactivación siga respondiendo `204`; la cuenta ya lo hacía.
    - RN-09 solo se consulta cuando la capacidad baja.
  - Mientras el script corría se superó el límite de 10 logins por minuto; se agregó `crearProfesorSinSesion` a los helpers para los profesores que no necesitan token.
- Files: `src/Matricula.Application/Cuentas/UsuarioService.cs`, `src/Matricula.Application/Cursos/CursoService.cs`, `src/Matricula.Application/Aulas/AulaService.cs`, `src/Matricula.Application/Grupos/GuardasGrupos.cs`, `src/Matricula.Api/Matricula.Api.http`

### T11. Eliminar cursos, aulas y periodos
- Acceptance:
  - `EliminarAsync` en `CursoService`, `AulaService` y `PeriodoService`: con grupos → `409 RN-34` con un mensaje que sugiere desactivar (o corregir, en el periodo); sin grupos → elimina.
  - `DELETE /api/v1/cursos/{id:int}`, `/aulas/{id:int}` y `/periodos/{id:int}` (`204`) con `Politicas.Administrador`.
- Verify:
  - Sin grupos → `204` y el detalle responde `404`.
  - Con grupos, incluidos cancelados → `409 RN-34`; nunca `500`.
  - Id inexistente → `404`. Profesor o estudiante → `403`.
  - Eliminado un curso o aula, su código se puede registrar de nuevo → `201`.
  - Resultado (2026-11-05): 35/35 verificaciones correctas contra la API local. Además de la lista: eliminar dos veces → `404` la segunda; un curso inactivo sin grupos también se elimina; con un grupo `Cancelado`, `Planificado` o `Finalizado` → `409 RN-34` y el registro sigue; el mensaje sugiere desactivar (curso, aula) o corregir o conservar (periodo); sin token → `401`.
  - Cambio fuera de la lista de archivos: `GlobalExceptionHandler` convierte una violación de llave foránea en `409 RN-34`, como ya hacía con la de unicidad (`RN-13`). Cubre el caso en que se crea un grupo entre la validación y el `DELETE`: las FK son `Restrict` y la BD rechaza el borrado, que antes habría salido como `500`. No se pudo provocar a mano (es una carrera); el camino normal lo cubren las validaciones de los servicios.
- Files: `src/Matricula.Application/{Cursos/CursoService,Aulas/AulaService,Periodos/PeriodoService}.cs`, `src/Matricula.Api/Controllers/{CursosController,AulasController,PeriodosController}.cs`, `src/Matricula.Api/Common/GlobalExceptionHandler.cs`, `src/Matricula.Api/Matricula.Api.http`

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
