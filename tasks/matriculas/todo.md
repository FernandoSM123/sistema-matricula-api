# Tareas: matriculas

Módulo `matriculas` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-matriculas.md`](../../specs/SPEC-matriculas.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-10)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño que cite los RF/RN. Las peticiones de cada flujo se agregan a `src/Matricula.Api/Matricula.Api.http` en la misma tarea.

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Dominio de matrículas y cupos | Completada | ✅ | ✅ | ➖ | ✅ |
| T2. Persistencia, `xmin` y migración `AgregaMatricula` | Completada | ✅ | ➖ | ✅ | ✅ |
| T3. Matricular, detalle y concurrencia | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Retirar | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. Cambiar de grupo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T6. Matrículas e historial de un estudiante | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T7. Horario del estudiante | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T8. Estudiantes de un grupo y detalle para el matriculado | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T9. RN-12 al cancelar un grupo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T10. RN-06 al agregar un bloque | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T11. Eliminar grupos | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T12. RN-36 y eliminar estudiantes con su cuenta | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T13. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

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
| `grupos` T1 | Una prueba unitaria de que `Grupo.ActualizarDatos` rechaza un cupo menor que `CuposOcupados` (`RF-45`) | T1 — ✅ cumplida (2026-11-10) |
| `grupos` T8 | La oferta académica incluye los grupos `Abierto` sin cupos disponibles (`cuposDisponibles = 0`) | T3 — ✅ cumplida (2026-11-10) |

## Detalle

### T1. Dominio de matrículas y cupos
- Acceptance:
  - Enums `EstadoMatricula` (`Activa`, `Retirada`) y `MotivoRetiro` (`RetiroVoluntario`, `CambioGrupo`, `CancelacionGrupo`).
  - `Matricula : EntidadBase` con `EstudianteId`, `GrupoId`, `FechaMatricula` (`DateOnly`), `Estado`, `FechaRetiro`, `MotivoRetiro` y navegaciones a `Estudiante` y `Grupo`. Nace `Activa`. `Retirar(fecha, motivo)`; retirar una `Retirada` → `BusinessRuleException("RF-52", ...)`.
  - `Grupo`: `uint Version`; `OcuparCupo()` (`RN-03` si no hay disponibles); `LiberarCupo()` (no baja de 0); `ValidarMovimiento(bool esAdministrador)`:
    - `Finalizado` o `Cancelado` → `RN-11` para cualquiera.
    - Estudiante: `Planificado` → `RN-02`; `Cerrado` o `EnCurso` → `RN-11`.
    - Administrador: `Planificado`, `Abierto`, `Cerrado` y `EnCurso` permitidos (supuesto 1).
  - `ReglasPeriodo.ClasesSeTraslapan(inicioA, finA, inicioB, finB)` (supuesto 4).
- Verify:
  - `MatriculaTests`: estado y fecha iniciales; `Retirar` asigna estado, fecha y motivo; retirar dos veces → `RF-52`.
  - `GrupoTests` (ampliado): `OcuparCupo` hasta llenar y `RN-03` al pasarse; `LiberarCupo` en 0 no baja; `ValidarMovimiento` en cada estado para estudiante y administrador; `ActualizarDatos` con un cupo menor que `CuposOcupados` → `RF-45` (✅ heredada de `grupos` T1).
  - `ReglasPeriodoTests`: periodos traslapados, contenidos, que comparten un día, contiguos sin compartir día y separados.
  - Resultado (2026-11-10): `dotnet build` sin warnings; 252/252 pruebas unitarias en verde (33 nuevas: 5 de `Matricula`, 18 de `Grupo` y 10 casos de `ClasesSeTraslapan`, cada uno comprobado en los dos sentidos). ✅ Heredada de `grupos` T1: `ActualizarDatos` con un cupo menor que `CuposOcupados` → `RF-45`, e igual a los ocupados se acepta.
  - Detalles que la spec no fijaba:
    - La entidad se llama `Matricula`, como en `docs/arquitectura.md`, pero ese nombre a secas resuelve al espacio de nombres raíz `Matricula` desde cualquier otro espacio de nombres (`CS0118`). Quien la use fuera de `Matricula.Domain.Entities` declara un alias: `using MatriculaEntidad = Matricula.Domain.Entities.Matricula;` (así lo hacen las pruebas). Aplica a T2 en adelante.
    - `Grupo.Version` ya existe pero todavía no está configurada como `xmin`: hasta T2, EF la ve como una columna nueva y `has-pending-model-changes` lo reporta. No se debe correr la API contra la BD entre T1 y T2.
    - Mensajes: `RN-02` dice que el grupo todavía no está abierto; `RN-11` nombra el estado y, para el estudiante, sugiere contactar a la administración.
- Files: `src/Matricula.Domain/Enums/{EstadoMatricula,MotivoRetiro}.cs`, `src/Matricula.Domain/Entities/{Matricula,Grupo}.cs`, `src/Matricula.Domain/Reglas/ReglasPeriodo.cs`, `tests/Matricula.UnitTests/Domain/{MatriculaTests,GrupoTests,ReglasPeriodoTests}.cs`

### T2. Persistencia, `xmin` y migración `AgregaMatricula`
- Acceptance:
  - `MatriculaConfiguration`: tabla `matricula`; enums como string; FK a estudiante y grupo; índice único `(estudiante_id, grupo_id)` filtrado `estado = 'Activa'`; índice `(grupo_id, estado)`.
  - `GrupoConfiguration`: `Version` con `.IsRowVersion()` (`xmin`).
  - `DbSet<Matricula>` en `IAppDbContext` y `AppDbContext`. Migración `AgregaMatricula`.
- Verify:
  - `dotnet ef database update` aplica la migración; el SQL muestra el índice único con `WHERE estado = 'Activa'`, las FK con `ON DELETE RESTRICT` y ninguna columna nueva en `grupo`.
  - `dotnet ef migrations has-pending-model-changes` → sin cambios pendientes.
  - Regresión: editar un grupo, agregar un bloque y cambiar su estado siguen respondiendo `200`/`201`/`204`.
  - Resultado (2026-11-10): `AgregaMatricula` aplicada a la BD de desarrollo y sin cambios de modelo pendientes. El script de la migración crea `matricula` con `fk_matricula_estudiante_estudiante_id` y `fk_matricula_grupo_grupo_id` (`ON DELETE RESTRICT`), `ix_matricula_estudiante_id_grupo_id` único con `WHERE estado = 'Activa'` e `ix_matricula_grupo_id_estado`. Aunque el `Up` de la migración lleva un `AddColumn("xmin")`, Npgsql no genera SQL para las columnas de sistema: `grupo` no cambia. Regresión: 13/13 verificaciones correctas contra la API local (crear grupo, agregar y quitar bloques, editar dos veces seguidas, recorrer los estados hasta `Finalizado`, cancelar, detalle sin `version` y oferta pública). 252/252 pruebas unitarias en verde.
  - Detalles que la spec no fijaba:
    - `IAppDbContext`, `AppDbContext` y `MatriculaConfiguration` usan el alias `MatriculaEntidad` (ver T1); el `DbSet` se llama `Matriculas`.
    - El filtro del índice se escribe con `nameof(EstadoMatricula.Activa)`, así no se desincroniza del texto que guarda la conversión del enum.
    - Para verificar a mano se usa la cuenta de prueba `humo.adm.742345@correo.local` (creada en la prueba de humo de `estudiantes`), a la que se le pidió una temporal por olvido y se le definió una contraseña de prueba. La cuenta real `admin@instituto.local` no se tocó.
- Files: `src/Matricula.Infrastructure/Persistence/Configurations/{MatriculaConfiguration,GrupoConfiguration}.cs`, `src/Matricula.Application/Abstractions/IAppDbContext.cs`, `src/Matricula.Infrastructure/Persistence/AppDbContext.cs`, migración

### T3. Matricular, detalle y concurrencia
- Acceptance:
  - `MatricularRequest` (administrador: `estudianteId`, `grupoId`) y `MatricularPropioRequest` (estudiante: `grupoId`) con validadores (ids obligatorios y mayores que 0 → `400`).
  - `MatriculaResponse` y `MatriculaProyecciones` (grupo, NRC, curso, periodo, profesor, fechas, estado, motivo).
  - `GuardasMatriculas`: RN-05 y RN-06 (supuesto 4), con matrículas a ignorar para T5 y varios estudiantes para T10.
  - `MatriculaService.MatricularAsync(estudianteId, grupoId)`: en orden RN-01 (`404` / `409`), `ValidarMovimiento` (RN-02/RN-11), RN-04 (omitida para el administrador), RN-05, RN-06 con el NRC del grupo con que choca, RN-03; un solo guardado; `DbUpdateConcurrencyException` → `409 CONCURRENCIA` con el mensaje del supuesto 6.
  - `MatriculaService.ObtenerAsync`: `404`; estudiante con matrícula ajena → `403 RN-18`.
  - `Politicas.AdministradorOEstudiante`. `MatriculasController`: `POST /api/v1/matriculas` (`Politicas.Administrador`, `201` + `Location`) y `GET /api/v1/matriculas/{id:int}` (`Politicas.AdministradorOEstudiante`). `MeController`: `POST /api/v1/me/matriculas` (`Politicas.Estudiante`, `201` + `Location`). `MatriculaService` registrado en `AddApplication`.
- Verify:
  - Estudiante en un grupo `Abierto`, dentro del rango y sin choques → `201` + `Location`; `cuposOcupados` +1; `Location` responde `200`.
  - Admin con estudiante inactivo → `409 RN-01`; estudiante o grupo inexistente → `404`.
  - Estudiante en `Planificado` / `Cerrado` / `Finalizado` → `409 RN-02` / `RN-11` / `RN-11`; fuera del rango → `409 RN-04`.
  - Admin en `Cerrado` o `EnCurso` y fuera del rango → `201`; en `Finalizado` o `Cancelado` → `409 RN-11`.
  - Mismo grupo dos veces o dos grupos del mismo curso y periodo → `409 RN-05` (también como admin).
  - Choque con un grupo matriculado del mismo periodo y de un periodo traslapado → `409 RN-06` con el NRC; contiguos o periodo separado → `201` (también como admin).
  - Grupo lleno → `409 RN-03` (también como admin). ✅ Heredada de `grupos` T8: la oferta lo muestra con `cuposDisponibles = 0`.
  - Dos peticiones en paralelo por el último cupo (varias rondas): una `201` y la otra `409` (`RN-03` o `CONCURRENCIA`); `cuposOcupados` nunca supera `cupo`.
  - Estudiante con la matrícula de otro → `403 RN-18`; profesor en cualquiera de los endpoints → `403`; sin token → `401`; con cambio pendiente → `403`.
  - Resultado (2026-11-10): 50/50 verificaciones correctas contra la API local, con datos nuevos y tokens reales de administrador, estudiante, profesor, sin token y con cambio pendiente. Además de la lista: el estudiante que envía otro `estudianteId` en el body se matricula a sí mismo; la respuesta trae estado `Activa`, la fecha local de hoy, NRC y curso; `Location` responde `200` al dueño; los rechazos no cambian `cuposOcupados`; el mensaje de RN-05 y el de RN-06 nombran el NRC del grupo con que chocan; el mismo curso en otro periodo y el mismo bloque en otro día → `201`; admin en `Planificado` → `201`; admin con un choque → `409 RN-06`; un estudiante en `POST /matriculas` y un admin en `POST /me/matriculas` → `403`; `{}` → `400` con los dos campos.
  - Concurrencia: 7 rondas de dos matrículas simultáneas (`Promise.all`) por el único cupo de un grupo. En todas, una `201` y la otra `409` (6 veces `CONCURRENCIA` por `xmin`, 1 vez `RN-03` porque la segunda leyó el grupo ya lleno) y `cuposOcupados = 1`.
  - ✅ Heredada de `grupos` T8: la oferta académica muestra el grupo `Abierto` lleno con `cuposDisponibles = 0`.
  - El script creó periodos vigentes temporales y al final los movió 40 años: la BD queda sin periodo vigente, como estaba.
  - Detalles que la spec no fijaba:
    - `GuardasMatriculas.BuscarChoquesAsync` ya recibe varios estudiantes y devuelve todos los choques (`ChoqueHorario`), para que T10 la reutilice; `ValidarSinChoquesAsync` es la versión de un estudiante que usan matricular y T5.
    - El conflicto de `xmin` se convierte en `BusinessRuleException("CONCURRENCIA", ...)` dentro de `MatriculaService.GuardarAsync`, con el mensaje del supuesto 6.
    - `MatriculaResponse` incluye la cédula y el nombre del estudiante, la modalidad y el estado del grupo, para que las consultas de T6 la reutilicen.
- Files: `src/Matricula.Application/Matriculas/{MatriculaService,GuardasMatriculas,MatricularRequest,MatricularRequestValidator,MatricularPropioRequest,MatricularPropioRequestValidator,MatriculaResponse,MatriculaProyecciones}.cs`, `src/Matricula.Application/DependencyInjection.cs`, `src/Matricula.Api/Seguridad/Politicas.cs`, `src/Matricula.Api/Controllers/{MatriculasController,MeController}.cs`, `src/Matricula.Api/Matricula.Api.http`

### T4. Retirar
- Acceptance:
  - `MatriculaService.RetirarAsync(id)`: RN-18; `409 RF-52` si ya está retirada; `ValidarMovimiento` sobre el grupo (RN-11) y RN-10 (las dos según el rol, supuesto 1); `Retirar(hoy, RetiroVoluntario)`, `LiberarCupo`, un solo guardado con la conversión de concurrencia.
  - `POST /api/v1/matriculas/{id:int}/retiro` (`204`) con `Politicas.AdministradorOEstudiante`.
- Verify:
  - Retiro dentro de la fecha límite → `204`; el detalle muestra `Retirada`, fecha y motivo; `cuposOcupados` −1.
  - Después de la fecha límite → `409 RN-10` para el estudiante, `204` para el admin.
  - Grupo `Cerrado` → `409 RN-11` para el estudiante, `204` para el admin.
  - Retirar dos veces → `409 RF-52`. Volver a matricularse en el mismo grupo después → `201`.
  - Estudiante con la matrícula de otro → `403 RN-18`; id inexistente → `404`; profesor → `403`.
  - Resultado (2026-11-10): 28/28 verificaciones correctas contra la API local. Además de la lista: el detalle muestra `Retirada`, la fecha de hoy y `RetiroVoluntario`, y conserva la fecha de matrícula; retirar dos veces → `RF-52` también como admin, sin tocar los cupos; volver a matricularse crea otra matrícula y la retirada se conserva (el índice único filtrado lo permite); retirado de un grupo, puede matricular otro del mismo curso y periodo (RN-05 solo con activas); el retiro libera el cupo de un grupo lleno y otro estudiante entra; los rechazos de RN-10 y RN-11 no cambian el estado ni los cupos; estudiante en `EnCurso` y `Finalizado` → `RN-11`; admin en `EnCurso` → `204` y en `Finalizado` → `RN-11`; la matrícula ajena sigue `Activa` tras el `403 RN-18`; sin token → `401`. La BD queda sin periodo vigente.
  - Detalle de implementación: `ValidarRetiro` (RF-52, RN-11 y RN-10 según el rol) y `BuscarPropiaAsync` (carga con seguimiento la matrícula con su grupo, periodo y bloques, y aplica RN-18) quedan listos para el origen del cambio de grupo de T5. `RF-52` se revisa antes que `RN-11`, así una matrícula retirada por la cancelación de su grupo responde que ya está retirada.
- Files: `src/Matricula.Application/Matriculas/MatriculaService.cs`, `src/Matricula.Api/Controllers/MatriculasController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T5. Cambiar de grupo
- Acceptance:
  - `CambioGrupoRequest` (`grupoDestinoId`) + validador.
  - `MatriculaService.CambiarGrupoAsync(id, request)`: RN-18; origen `Activa` (`RF-52`); origen como retiro (RN-10, RN-11); destino distinto, del mismo curso y periodo (`409 RF-53`); destino como ingreso sin RN-04 (supuesto 3); RN-05 y RN-06 ignorando el origen; RN-03. Retira el origen con `CambioGrupo`, mueve los cupos y crea la nueva matrícula en un solo guardado, con la conversión de concurrencia.
  - `POST /api/v1/matriculas/{id:int}/cambio-grupo` (`201` + `Location` a la nueva) con `Politicas.AdministradorOEstudiante`.
- Verify:
  - Cambio válido → `201`; origen `Retirada` con `CambioGrupo`; nueva `Activa`; los dos `cuposOcupados` se mueven.
  - Destino de otro curso o periodo, o el mismo grupo → `409 RF-53`.
  - Destino lleno, con choque (que no sea con el propio origen) o no `Abierto` → `409` con la regla; la matrícula original sigue `Activa` y los cupos no cambian.
  - Un destino que solo choca con el horario del grupo de origen → `201`.
  - Después de la fecha límite de retiro → `409 RN-10` para el estudiante; después del fin de matrícula pero antes de la fecha límite → `201` (supuesto 3).
  - Admin: destino `Cerrado` y fuera de fechas → `201`.
  - Estudiante con la matrícula de otro → `403 RN-18`.
- Files: `src/Matricula.Application/Matriculas/{MatriculaService,CambioGrupoRequest,CambioGrupoRequestValidator}.cs`, `src/Matricula.Api/Controllers/MatriculasController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T6. Matrículas e historial de un estudiante
- Acceptance:
  - `ListarMatriculasRequest` (hereda `PaginacionRequest`): `periodoId`, `estado`, `search` (curso o NRC).
  - Listado con `Select` y `AsNoTracking`, ordenado por inicio de clases descendente, nombre del curso e `Id`; incluye grupos y estudiantes inactivos (RN-40).
  - `GET /api/v1/estudiantes/{id:int}/matriculas` (`Politicas.Administrador` de clase, `404` si no existe) y `GET /api/v1/me/matriculas` (`Politicas.Estudiante`).
- Verify:
  - Sin filtros: activas y retiradas con fechas y motivo (RF-54); `estado=Activa`: solo las activas (RF-20); `periodoId`, `search` y paginación filtran.
  - `/me/matriculas` da lo mismo que `/estudiantes/{id}/matriculas` para el mismo estudiante; otro estudiante solo ve las suyas.
  - Profesor o admin en `/me/matriculas` → `403`; estudiante en `/estudiantes/{id}/matriculas` → `403`; sin token → `401`.
- Files: `src/Matricula.Application/Matriculas/{MatriculaService o Consultas/...,ListarMatriculasRequest}.cs`, `src/Matricula.Api/Controllers/{EstudiantesController,MeController}.cs`, `src/Matricula.Api/Matricula.Api.http`

### T7. Horario del estudiante
- Acceptance:
  - `HorarioSemanalService`: horario de un estudiante (grupos con matrícula `Activa`, sin cancelados) para el administrador y para el estudiante de la sesión; `MiHorarioAsync` decide por rol.
  - `Politicas.ProfesorOEstudiante`. `GET /api/v1/estudiantes/{id:int}/horario` (`404` si no existe) y `GET /api/v1/me/horario` con la política nueva.
- Verify:
  - `/me/horario` como estudiante y `/estudiantes/{id}/horario` dan los mismos bloques, ordenados por día y hora; sin los grupos de los que se retiró.
  - Sin `periodoId` y sin periodo vigente → `404` pidiendo el `periodoId`.
  - Regresión: `/me/horario` como profesor sigue igual; como admin → `403`.
- Files: `src/Matricula.Application/Grupos/Consultas/HorarioSemanalService.cs`, `src/Matricula.Api/Seguridad/Politicas.cs`, `src/Matricula.Api/Controllers/{EstudiantesController,MeController}.cs`, `src/Matricula.Api/Matricula.Api.http`

### T8. Estudiantes de un grupo y detalle para el matriculado
- Acceptance:
  - `ListarEstudiantesGrupoRequest`: `estado` (por defecto `Activa`), `search` (nombre, apellidos o cédula). `EstudianteGrupoResponse`: id de la matrícula, estudiante (id, cédula, nombre, apellidos, correo), fechas, estado y motivo (supuesto 8). Orden por apellidos, nombre e `Id`.
  - Profesor de otro grupo → `403 RN-18`; grupo inexistente → `404`.
  - `GET /api/v1/grupos/{id:int}/estudiantes` con `Politicas.AdministradorOProfesor`.
  - `GrupoService.ObtenerAsync`: el estudiante también ve el detalle de un grupo en el que tiene una matrícula, activa o retirada.
- Verify:
  - Admin y el profesor del grupo → `200` con los activos; `estado=Retirada` → los retirados; otro profesor → `403 RN-18`; estudiante → `403`.
  - La respuesta no trae teléfono ni dirección.
  - Estudiante: detalle de un grupo `Cerrado` donde está matriculado → `200`; donde no → `403 RN-18`.
- Files: `src/Matricula.Application/Matriculas/{...,ListarEstudiantesGrupoRequest,EstudianteGrupoResponse}.cs`, `src/Matricula.Application/Grupos/GrupoService.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T9. RN-12 al cancelar un grupo
- Acceptance:
  - `GrupoService.CambiarEstadoAsync`: al pasar a `Cancelado`, retira todas las matrículas `Activa` con `CancelacionGrupo` y la fecha de hoy, y libera sus cupos, en el mismo guardado que el estado (supuesto 10). `GrupoService` recibe `IFechaLocal`.
- Verify:
  - Cancelar un grupo con matrículas activas y retiradas → `204`; las activas quedan `Retirada` con `CancelacionGrupo`; las ya retiradas no cambian; `cuposOcupados = 0`.
  - Funciona aunque haya pasado la fecha límite de retiro.
  - Repetir la cancelación → `204` sin cambios. Cancelar un grupo sin matrículas sigue igual.
- Files: `src/Matricula.Application/Grupos/GrupoService.cs`, `src/Matricula.Api/Matricula.Api.http`

### T10. RN-06 al agregar un bloque
- Acceptance:
  - `GrupoService.AgregarHorarioAsync`: después de RN-14, RN-07 y RN-08, si el grupo tiene matrículas `Activa`, usa `GuardasMatriculas` con el bloque nuevo y sus estudiantes; si alguno chocaría → `409 RN-06` con la cantidad de estudiantes y un NRC con que chocan, sin nombres (supuesto 13).
- Verify:
  - Bloque que choca con otro grupo de un estudiante matriculado → `409 RN-06` y el bloque no se guarda; también como admin.
  - Bloque sin choque, o que choca solo con un grupo del que el estudiante se retiró → `201`.
  - Grupo sin matrículas: se comporta como antes.
- Files: `src/Matricula.Application/Grupos/GrupoService.cs`, `src/Matricula.Application/Matriculas/GuardasMatriculas.cs`, `src/Matricula.Api/Matricula.Api.http`

### T11. Eliminar grupos
- Acceptance:
  - `GrupoService.EliminarAsync`: con matrículas (cualquier estado) → `409 RN-34` sugiriendo cancelar; sin matrículas → `Remove` de sus bloques y del grupo en un solo guardado (supuesto 9).
  - `DELETE /api/v1/grupos/{id:int}` (`204`) con `Politicas.Administrador`.
- Verify:
  - Grupo sin matrículas (con bloques, en `Planificado` y en `Abierto`) → `204`; el detalle → `404`; su NRC se registra de nuevo → `201`.
  - Con matrículas, aunque estén retiradas → `409 RN-34`; nunca `500`.
  - Id inexistente → `404`; profesor o estudiante → `403`.
- Files: `src/Matricula.Application/Grupos/GrupoService.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T12. RN-36 y eliminar estudiantes con su cuenta
- Acceptance:
  - `UsuarioService.DesactivarAsync`: cuenta `Estudiante` con matrículas `Activa` en un periodo en curso (`InicioMatricula ≤ hoy ≤ FinClases`) → `409 RN-36` con la cantidad (supuesto 12). `UsuarioService` recibe `IFechaLocal`.
  - `EstudianteService.EliminarAsync`: con matrículas → `409 RN-34`; sin matrículas → `Remove` del estudiante y de su `Usuario` en un solo guardado (RN-35, decisión 10 del mapa).
  - `DELETE /api/v1/estudiantes/{id:int}` (`204`).
- Verify:
  - Desactivar por `/estudiantes/{id}/desactivar` y por `/usuarios/{id}/desactivar` con matrículas activas en el periodo vigente y en otro periodo en curso → `409 RN-36`; con todas retiradas o en periodos terminados → `204`.
  - Regresión: desactivar un profesor y un administrador sigue funcionando.
  - Eliminar un estudiante sin matrículas → `204`; su token → `401`; su correo y su cédula se registran de nuevo → `201`.
  - Con matrículas, aunque estén retiradas → `409 RN-34`; id inexistente → `404`.
- Files: `src/Matricula.Application/Cuentas/UsuarioService.cs`, `src/Matricula.Application/Estudiantes/EstudianteService.cs`, `src/Matricula.Api/Controllers/EstudiantesController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T13. Cierre del módulo
- Acceptance:
  - `Matricula.Api.http` con el recorrido completo de la spec.
  - Recorrido de la tabla de verificación manual de la spec, con los tres roles, sin token y con cambio pendiente.
  - `cuposOcupados` coincide con el conteo de matrículas activas en los grupos usados en las pruebas.
  - Estados de la spec, el mapa y esta lista al día; las verificaciones heredadas de `grupos` marcadas como cumplidas en el mapa. Con este módulo se completan todos los del mapa.
- Verify: prueba de humo final con datos nuevos sobre toda la tabla de verificación de la spec; `dotnet build` sin warnings; pruebas unitarias en verde; `has-pending-model-changes` sin cambios.
- Files: `src/Matricula.Api/Matricula.Api.http`, `specs/mapa-modulos.md`, `specs/SPEC-matriculas.md`, `tasks/matriculas/todo.md`
