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
| T5. Cambiar de grupo | Completada | ✅ | ➖ | ✅ | ✅ |
| T6. Matrículas e historial de un estudiante | Completada | ✅ | ➖ | ✅ | ✅ |
| T7. Horario del estudiante | Completada | ✅ | ➖ | ✅ | ✅ |
| T8. Estudiantes de un grupo y detalle para el matriculado | Completada | ✅ | ➖ | ✅ | ✅ |
| T9. RN-12 al cancelar un grupo | Completada | ✅ | ➖ | ✅ | ✅ |
| T10. RN-06 al agregar un bloque | Completada | ✅ | ➖ | ✅ | ✅ |
| T11. Eliminar grupos | Completada | ✅ | ➖ | ✅ | ✅ |
| T12. RN-36 y eliminar estudiantes con su cuenta | Completada | ✅ | ➖ | ✅ | ✅ |
| T13. Cierre del módulo | Completada | ✅ | ➖ | ✅ | ✅ |

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
  - Resultado (2026-11-10): 31/31 verificaciones correctas contra la API local. Además de la lista: el `Location` apunta a la matrícula nueva, que tiene la fecha de hoy y el mismo estudiante; el origen queda `Retirada` con `CambioGrupo` y la fecha de hoy; tras cada rechazo (lleno, `Planificado` → `RN-02`, `Cerrado` → `RN-11`, choque con otro grupo → `RN-06` con su NRC) la matrícula original sigue `Activa` y ningún cupo cambia; destino inexistente → `404`; cambiar desde una matrícula ya retirada → `RF-52`; admin desde un origen `Finalizado` → `RN-11`; la matrícula ajena sigue `Activa` tras el `403 RN-18`; profesor → `403`; sin token → `401`; sin destino → `400`. Para el supuesto 3, el estudiante no puede matricularse en el periodo con la matrícula cerrada (`RN-04`), pero sí cambiar de grupo dentro de la fecha límite (`201`). La BD queda sin periodo vigente.
  - En la primera corrida dos verificaciones del administrador fallaron por el script (el estudiante elegido ya tenía otro grupo del curso y la API respondió bien `RN-05`); se usó otro estudiante. Una corrida intermedia se cortó por el límite de 10 logins por minuto (`429`); se repitió completa.
  - Detalles que la spec no fijaba:
    - El cambio también revisa RN-01 sobre el estudiante, como matricular; se extrajo a `ValidarEstudianteActivoAsync`.
    - Orden: RN-18 → origen (`RF-52`, `RN-11`, `RN-10`) → destino igual al actual (`RF-53`) → `404` del destino → mismo curso y periodo (`RF-53`) → RN-01 → destino (`RN-02`/`RN-11`) → RN-05 → RN-06 → RN-03.
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
  - Resultado (2026-11-10): 24/24 verificaciones correctas contra la API local, con un estudiante que se matriculó, se retiró, cambió de grupo y tiene matrículas en dos periodos. Además de la lista: la retirada trae fecha y motivo (`CambioGrupo`); cada fila trae grupo, curso, periodo, profesor y fechas; la búsqueda encuentra por NRC (sin distinguir mayúsculas), por parte del nombre del curso y por su código; `periodoId` y `estado` se combinan; estado inexistente → `400`; estudiante inexistente → `404`; las matrículas de un estudiante inactivo se siguen viendo (RN-40); un profesor en cualquiera de los dos → `403`. La BD queda sin periodo vigente.
  - Detalles que la spec no fijaba:
    - Orden: inicio de clases del periodo descendente, nombre del curso y, dentro del mismo curso, la matrícula más nueva primero (`Id` descendente): tras un cambio de grupo la activa queda antes que la retirada.
    - Los dos endpoints viven en `MatriculaService` (`ListarDeEstudianteAsync`, `ListarMiasAsync`) con un `ListarAsync` común; no hizo falta un servicio de consultas aparte.
- Files: `src/Matricula.Application/Matriculas/{MatriculaService o Consultas/...,ListarMatriculasRequest}.cs`, `src/Matricula.Api/Controllers/{EstudiantesController,MeController}.cs`, `src/Matricula.Api/Matricula.Api.http`

### T7. Horario del estudiante
- Acceptance:
  - `HorarioSemanalService`: horario de un estudiante (grupos con matrícula `Activa`, sin cancelados) para el administrador y para el estudiante de la sesión; `MiHorarioAsync` decide por rol.
  - `Politicas.ProfesorOEstudiante`. `GET /api/v1/estudiantes/{id:int}/horario` (`404` si no existe) y `GET /api/v1/me/horario` con la política nueva.
- Verify:
  - `/me/horario` como estudiante y `/estudiantes/{id}/horario` dan los mismos bloques, ordenados por día y hora; sin los grupos de los que se retiró.
  - Sin `periodoId` y sin periodo vigente → `404` pidiendo el `periodoId`.
  - Regresión: `/me/horario` como profesor sigue igual; como admin → `403`.
  - Resultado (2026-11-10): 18/18 verificaciones correctas contra la API local. Un estudiante con cuatro grupos en el periodo vigente (uno de ellos con dos bloques en días distintos), uno retirado y otro cancelado: el horario trae solo los tres bloques de los grupos activos, ordenados por día y hora, igual en `/me/horario` y en `/estudiantes/{id}/horario`. Además: con `periodoId` de otro periodo trae solo los bloques de ese periodo; otro estudiante ve solo los suyos; sin matrículas → `200` con lista vacía; estudiante o periodo inexistente → `404`; sin periodo vigente → `404` pidiendo el `periodoId` (estudiante y admin), y con `periodoId` explícito sigue respondiendo. Regresión: `/me/horario` como profesor y `/profesores/{id}/horario` siguen igual; admin en `/me/horario`, estudiante o profesor en `/estudiantes/{id}/horario` → `403`; sin token → `401`. La BD queda sin periodo vigente.
  - Detalles que la spec no fijaba:
    - `Grupo` no tiene navegación a sus matrículas; el filtro del estudiante es una subconsulta sobre `db.Matriculas` (`DelEstudiante`), y el resto de la consulta se comparte con el profesor y el aula.
    - Un grupo cancelado no aparece aunque la matrícula siga `Activa` (hasta T9 cancelar no retira las matrículas).
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
  - Resultado (2026-11-10): 24/24 verificaciones correctas contra la API local. Lista de estudiantes: por defecto los 3 activos, ordenados por apellidos, con matrícula, cédula, nombre, apellidos, correo, fecha y estado, sin teléfono ni dirección; `estado=Retirada` trae al retirado con fecha y motivo; búsqueda por cédula (sin distinguir mayúsculas) y por apellidos; paginación; estado inexistente → `400`; grupo inexistente → `404` (también para un profesor); otro grupo trae solo los suyos. Acceso: su profesor → `200`, otro profesor → `403 RN-18`, estudiante → `403`, sin token → `401`. Detalle: grupo `Cerrado` donde está matriculado o del que se retiró → `200`; donde no → `403 RN-18`; un `Abierto` sin matrícula sigue en `200`; regresión del profesor (`403 RN-18` el ajeno, `200` el suyo). La BD queda sin periodo vigente.
  - Detalles que la spec no fijaba:
    - La lista vive en `MatriculaService.ListarEstudiantesGrupoAsync` (consulta sobre matrículas); `GruposController` recibe `MatriculaService`. El grupo inexistente se revisa antes que la propiedad, así que un profesor recibe `404` y no `403` con un id que no existe.
    - `Estado` del request no es anulable: sin parámetro vale `Activa`, y no hay forma de pedir activos y retirados juntos (la spec no lo pide).
- Files: `src/Matricula.Application/Matriculas/{...,ListarEstudiantesGrupoRequest,EstudianteGrupoResponse}.cs`, `src/Matricula.Application/Grupos/GrupoService.cs`, `src/Matricula.Api/Controllers/GruposController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T9. RN-12 al cancelar un grupo
- Acceptance:
  - `GrupoService.CambiarEstadoAsync`: al pasar a `Cancelado`, retira todas las matrículas `Activa` con `CancelacionGrupo` y la fecha de hoy, y libera sus cupos, en el mismo guardado que el estado (supuesto 10). `GrupoService` recibe `IFechaLocal`.
- Verify:
  - Cancelar un grupo con matrículas activas y retiradas → `204`; las activas quedan `Retirada` con `CancelacionGrupo`; las ya retiradas no cambian; `cuposOcupados = 0`.
  - Funciona aunque haya pasado la fecha límite de retiro.
  - Repetir la cancelación → `204` sin cambios. Cancelar un grupo sin matrículas sigue igual.
  - Resultado (2026-11-10): 20/20 verificaciones correctas contra la API local, en un periodo con la fecha límite de retiro ya pasada (el estudiante recibe `RN-10` al retirarse). Cancelar el grupo con dos activas y una retirada por el admin → `204`: las activas quedan `Retirada` con `CancelacionGrupo` y la fecha de hoy, la retirada conserva `RetiroVoluntario`, `cuposOcupados = 0` y los tres aparecen en `/grupos/{id}/estudiantes?estado=Retirada`. Repetir → `204` sin cambios; sin matrículas → `204`; un `Finalizado` sigue en `409 RF-47`. Después: retirar la matrícula cancelada → `409 RF-52`; matricularse en el grupo cancelado → `409 RN-11`; matricularse en otro grupo del mismo curso → `201` (sin `RN-05`); el horario ya no trae el grupo. Cancelar desde `EnCurso` también retira. La BD queda sin periodo vigente.
  - Detalles que la spec no fijaba:
    - Solo se retira al pasar a `Cancelado` desde otro estado; repetir la cancelación no vuelve a consultar las matrículas.
    - Si alguien se matricula mientras se cancela, el token de concurrencia del grupo (`xmin`) hace fallar a uno de los dos; el `GlobalExceptionHandler` ya traduce `DbUpdateConcurrencyException` a `409 CONCURRENCIA`, así que `GrupoService` no la captura.
- Files: `src/Matricula.Application/Grupos/GrupoService.cs`, `src/Matricula.Api/Matricula.Api.http`

### T10. RN-06 al agregar un bloque
- Acceptance:
  - `GrupoService.AgregarHorarioAsync`: después de RN-14, RN-07 y RN-08, si el grupo tiene matrículas `Activa`, usa `GuardasMatriculas` con el bloque nuevo y sus estudiantes; si alguno chocaría → `409 RN-06` con la cantidad de estudiantes y un NRC con que chocan, sin nombres (supuesto 13).
- Verify:
  - Bloque que choca con otro grupo de un estudiante matriculado → `409 RN-06` y el bloque no se guarda; también como admin.
  - Bloque sin choque, o que choca solo con un grupo del que el estudiante se retiró → `201`.
  - Grupo sin matrículas: se comporta como antes.
  - Resultado (2026-11-10): 14/14 verificaciones correctas contra la API local (los bloques solo los agrega el admin, así que todos los casos son como admin). Un bloque que choca con el grupo de dos matriculados → `409 RN-06`, con "2 estudiantes" y el NRC, sin nombres, y el bloque no se guarda; con un solo afectado el mensaje dice "1 estudiante". `RN-14` sigue respondiendo antes que `RN-06`. Chocar solo con un grupo del que el estudiante se retiró, o un bloque contiguo → `201`, y el horario del estudiante muestra los bloques nuevos. Grupo sin matrículas, o cuyo único estudiante se retiró → `201` aunque el bloque coincida con otro grupo. Tras cancelar el grupo con que chocaba (RN-12 retira esas matrículas), el mismo bloque → `201`. La BD queda sin periodo vigente.
  - Detalles que la spec no fijaba:
    - La guarda es `GuardasMatriculas.ValidarBloquesSinChoquesAsync`, que reusa `BuscarChoquesAsync` con todos los estudiantes activos del grupo; si no hay ninguno, no consulta el periodo.
    - El NRC del mensaje es el del primer choque encontrado; si hay varios grupos distintos, solo se nombra uno.
- Files: `src/Matricula.Application/Grupos/GrupoService.cs`, `src/Matricula.Application/Matriculas/GuardasMatriculas.cs`, `src/Matricula.Api/Matricula.Api.http`

### T11. Eliminar grupos
- Acceptance:
  - `GrupoService.EliminarAsync`: con matrículas (cualquier estado) → `409 RN-34` sugiriendo cancelar; sin matrículas → `Remove` de sus bloques y del grupo en un solo guardado (supuesto 9).
  - `DELETE /api/v1/grupos/{id:int}` (`204`) con `Politicas.Administrador`.
- Verify:
  - Grupo sin matrículas (con bloques, en `Planificado` y en `Abierto`) → `204`; el detalle → `404`; su NRC se registra de nuevo → `201`.
  - Con matrículas, aunque estén retiradas → `409 RN-34`; nunca `500`.
  - Id inexistente → `404`; profesor o estudiante → `403`.
  - Resultado (2026-11-10): 19/19 verificaciones correctas contra la API local. Sin matrículas, se eliminan grupos en `Planificado` (con 2 bloques), `Abierto`, `Finalizado` y `Cancelado` → `204`; el detalle → `404`; su NRC se registra de nuevo → `201` y su bloque ya no ocupa al profesor (sin `RN-07`); al quedar sin grupos, su curso se puede eliminar. Con una matrícula activa, retirada, o con el grupo ya cancelado → `409 RN-34` sugiriendo cancelar, y el grupo sigue con sus bloques. Inexistente → `404`; profesor o estudiante → `403`; sin token → `401`. La BD queda sin periodo vigente.
  - Detalles que la spec no fijaba:
    - Si alguien se matricula mientras se elimina el grupo, el token de concurrencia del grupo (`xmin`) hace fallar a uno de los dos con `409 CONCURRENCIA` en lugar de una violación de llave foránea. No se reprodujo manualmente.
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
  - Resultado (2026-11-10): 24/24 verificaciones correctas contra la API local, con tres periodos: uno vigente, otro en curso que no es el vigente y uno terminado. RN-36: matrícula activa en el vigente o en el otro periodo en curso → `409 RN-36` por `/estudiantes/{id}/desactivar` y por `/usuarios/{id}/desactivar`; con dos activas el mensaje da "2 matrículas"; el estudiante rechazado sigue activo. Solo con una retirada, o activa solo en el periodo terminado → `204`; tras retirarlo se desactiva y se reactiva. Regresión: profesor sin grupos → `204`, con un grupo abierto → `409 RN-37`; un administrador que no es el último → `204`. Eliminar: sin matrículas → `204`, el detalle → `404`, su token → `401`, su correo y cédula se registran de nuevo → `201`; con una matrícula retirada o activa → `409 RN-34` sugiriendo desactivar; inexistente → `404`; estudiante → `403`; sin token → `401`. La BD queda sin periodo vigente.
  - Detalles que la spec no fijaba:
    - RN-36 cuenta las matrículas con una sola consulta por `Estudiante.UsuarioId`, así no hace falta buscar primero el estudiante de la cuenta.
    - Igual que con los profesores, la eliminación usa `Include(e => e.Usuario)` y quita los dos registros de forma explícita, sin cascada.
- Files: `src/Matricula.Application/Cuentas/UsuarioService.cs`, `src/Matricula.Application/Estudiantes/EstudianteService.cs`, `src/Matricula.Api/Controllers/EstudiantesController.cs`, `src/Matricula.Api/Matricula.Api.http`

### T13. Cierre del módulo
- Acceptance:
  - `Matricula.Api.http` con el recorrido completo de la spec.
  - Recorrido de la tabla de verificación manual de la spec, con los tres roles, sin token y con cambio pendiente.
  - `cuposOcupados` coincide con el conteo de matrículas activas en los grupos usados en las pruebas.
  - Estados de la spec, el mapa y esta lista al día; las verificaciones heredadas de `grupos` marcadas como cumplidas en el mapa. Con este módulo se completan todos los del mapa.
- Verify: prueba de humo final con datos nuevos sobre toda la tabla de verificación de la spec; `dotnet build` sin warnings; pruebas unitarias en verde; `has-pending-model-changes` sin cambios.
  - Resultado (2026-11-10): prueba de humo final con datos nuevos: 69/69 verificaciones correctas contra la API local, una o más por cada fila de la tabla de la spec, con tokens de administrador, profesor (el del grupo y otro), estudiante, sin token y con cambio pendiente (`403`, RN-24). Usa tres periodos (vigente, con la fecha límite de retiro pasada y con la matrícula cerrada) e incluye las dos peticiones simultáneas por el último cupo (una `201`, otra `409`), la oferta con un grupo `Abierto` lleno (heredada de `grupos` T8) y, al final, el horario sin periodo vigente (`404`). `cuposOcupados` coincide con el conteo de matrículas activas en los 15 grupos usados. `dotnet build --no-incremental` con 0 advertencias y 0 errores; 252/252 pruebas unitarias en verde; `has-pending-model-changes` sin cambios. `Matricula.Api.http` tiene las peticiones de todos los endpoints del módulo; las verificaciones heredadas de `grupos` T1 y T8 ya estaban marcadas como cumplidas en el mapa (T1 y T3).
  - Datos de prueba: los scripts de verificación dejaron en la BD de desarrollo periodos (movidos 40 años adelante para que no haya vigente), cursos, profesores, estudiantes, grupos y matrículas con correos `@prueba.local`, y un administrador de prueba desactivado. No se borraron: las matrículas nunca se eliminan (RN-35) y RN-34 impide eliminar lo que tiene datos relacionados.
- Files: `src/Matricula.Api/Matricula.Api.http`, `specs/mapa-modulos.md`, `specs/SPEC-matriculas.md`, `tasks/matriculas/todo.md`
