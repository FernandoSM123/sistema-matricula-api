# Tareas: datos-prueba

Módulo `datos-prueba` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-datos-prueba.md`](../../specs/SPEC-datos-prueba.md) · Plan: [`plan.md`](plan.md)

**Estado de la lista:** Aprobada (2026-11-13)

Cada tarea termina con `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde, la verificación indicada y un commit pequeño. Las verificaciones con datos empiezan con la BD recreada:

```bash
dotnet ef database drop --force --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
```

## Estado

| Tarea | Estado | Código | Pruebas unitarias | Verificación manual | Commit |
|---|---|:---:|:---:|:---:|:---:|
| T1. Base: paquete, opciones y guardas | Completada | ✅ | ✅ | ✅ | ✅ |
| T2. `PlanificadorHorarios` y `FechasPrueba` | Completada | ✅ | ✅ | ➖ | ✅ |
| T3. Catálogos y personas | Completada | ✅ | ➖ | ✅ | ✅ |
| T4. Grupos y horarios | Completada | ✅ | ➖ | ✅ | ✅ |
| T5. Matrículas y estados finales | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T6. Cierre del módulo | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |

**Estados de tarea:**
- **Pendiente:** no se ha empezado.
- **En progreso:** se está trabajando en ella.
- **Implementada:** el código está hecho, pero falta alguna columna (pruebas, verificación o commit).
- **Bloqueada:** no puede avanzar; se indica el motivo debajo de la tabla.
- **Completada:** todas las columnas en ✅, ➖ o ⏳.

**Columnas:** ✅ hecho · ⬜ pendiente · ➖ no aplica · ⏳ parte diferida a otro módulo (anotada en sus tareas)

## Verificaciones heredadas

Ninguna.

## Detalle

### T1. Base: paquete, opciones y guardas
- Acceptance:
  - `Bogus` 35.6.5 en `Matricula.Infrastructure.csproj`.
  - Referencia de `tests/Matricula.UnitTests` a `Matricula.Infrastructure` (supuesto 6 de la spec).
  - `DatosPruebaOptions` (sección `DatosPrueba`):
    - Las propiedades de la spec, con sus valores por defecto.
    - `Validar()`: devuelve la lista de errores, con un mensaje por clave que la nombra y su rango. `Contrasena` es obligatoria y debe cumplir `PoliticaContrasena.Cumple`.
  - Sección `DatosPrueba` en `appsettings.json` con `Habilitado = false`, sin `Contrasena`.
  - `DatosPruebaSeedService.EjecutarAsync(bool esDesarrollo, CancellationToken)` con las guardas del plan, en orden:
    1. flag apagado → nada;
    2. fuera de `Development` → `LogWarning`;
    3. opciones inválidas → `InvalidOperationException` con todos los errores;
    4. datos de negocio → `LogInformation`.
  - Si pasa las guardas, por ahora solo escribe "Se sembrarían datos de prueba con la semilla {Semilla}".
  - `SeedExtensions.SembrarDatosPruebaAsync`, registro en `AddInfrastructure` y llamada en `Program.cs` después del administrador inicial.
- Verify:
  - `dotnet test`, con `DatosPruebaOptionsTests`:
    - los valores por defecto con una contraseña válida no tienen errores;
    - cada clave fuera de rango, en ambos extremos, da un error que la nombra;
    - sin contraseña, o con una sin mayúscula, sin número o con menos de 8 caracteres, da error.
  - Manual, con la API:
    - Flag apagado: arranca normal y el log no menciona los datos de prueba.
    - `DatosPrueba__Habilitado=true` sin contraseña: no arranca, y el mensaje nombra `DatosPrueba:Contrasena`.
    - Con contraseña y `DatosPrueba__Estudiantes=-1`: no arranca, y el mensaje nombra la clave.
    - Con todo válido en `Development` y la BD recién recreada: el log muestra la semilla.
    - El mismo caso con `ASPNETCORE_ENVIRONMENT=Production` (con la cadena de conexión por variable de entorno): `LogWarning` y no hace nada.
    - Con un curso creado desde Scalar: "La BD ya tiene datos".
    - La contraseña no aparece en el log.
  - Resultado (2026-11-16): `dotnet build` sin warnings; 352/352 pruebas unitarias en verde (20 nuevas de `DatosPruebaOptionsTests`). Verificación manual sobre una BD temporal `matricula_semilla_t1`, creada con `dotnet ef database update` y borrada al terminar, para no tocar la BD de desarrollo. La contraseña de prueba se pasó como variable de entorno del proceso:

    | Escenario | Resultado |
    |---|---|
    | Flag apagado (valor por defecto) | Arranca normal: crea el administrador inicial, y el log no menciona los datos de prueba |
    | `DatosPrueba__Habilitado=true` sin contraseña | No arranca: `InvalidOperationException` "La configuración de los datos de prueba no es válida: - La opción 'DatosPrueba:Contrasena' es obligatoria y debe cumplir RN-26…" |
    | Con contraseña y `DatosPrueba__Estudiantes=-1` | No arranca: "La opción 'DatosPrueba:Estudiantes' debe estar entre 0 y 5000." |
    | Todo válido, `Development`, BD sin datos de negocio | Arranca, con "Se sembrarían datos de prueba con la semilla 1630291738". Con `DatosPrueba__Semilla=123`: "…con la semilla 123" |
    | Todo válido con `ASPNETCORE_ENVIRONMENT=Production` | Arranca, con el `LogWarning` "'DatosPrueba:Habilitado' está activo fuera de Development; no se siembran datos de prueba" |
    | Un curso insertado con `psql` en la BD temporal | "La BD ya tiene datos; no se siembran datos de prueba" |
    | La contraseña de prueba en los logs de todos los escenarios | No aparece |

  - Detalles que la tarea no fijaba:
    - El curso del último escenario se insertó con `psql` en la BD temporal, en lugar de crearlo desde Scalar: solo hacía falta que existiera un dato de negocio.
    - La API toma `appsettings.json` del directorio actual: el `.exe` se arranca desde `src/Matricula.Api`.
- Files: `src/Matricula.Infrastructure/Matricula.Infrastructure.csproj`, `src/Matricula.Infrastructure/Seed/DatosPrueba/{DatosPruebaOptions,DatosPruebaSeedService}.cs`, `src/Matricula.Infrastructure/Seed/SeedExtensions.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`, `src/Matricula.Api/{Program.cs,appsettings.json}`, `tests/Matricula.UnitTests/Matricula.UnitTests.csproj`, `tests/Matricula.UnitTests/Seed/DatosPruebaOptionsTests.cs`

### T2. `PlanificadorHorarios` y `FechasPrueba`
- Acceptance:
  - `Franja` (`record`) y la lista fija de franjas de la spec: lunes a viernes, seis franjas; sábado, dos.
  - `FechasPrueba.Calcular(DateOnly hoy)`: las fechas y el nombre de los periodos pasado, vigente y futuro, con los desplazamientos de la spec y el formato `<año>-T<trimestre>`.
  - `PlanificadorHorarios(Random random)`:
    - `IntentarAsignarGrupo(int cantidadBloques, IReadOnlyList<int> profesores, IReadOnlyList<int>? aulas)`: franjas en días distintos y un profesor y un aula libres en todas ellas, o `null` si no queda ninguna combinación.
    - `PuedeMatricular(int estudianteId, int cursoId, IReadOnlyCollection<Franja> franjas)` y `RegistrarMatricula(...)`, con RN-05 y RN-06.
    - `LiberarMatricula(...)`, para el cambio de grupo y los retiros del periodo vigente.
- Verify: `dotnet test`, con:
  - `PlanificadorHorariosTests`:
    - Con un profesor y un aula, asigna grupos hasta agotar las franjas sin repetir ninguna, y el siguiente devuelve `null`.
    - Con varios profesores y aulas, ningún par (profesor, franja) ni (aula, franja) se repite en 200 asignaciones.
    - Un grupo virtual (`aulas = null`) no ocupa aulas.
    - Los bloques de un mismo grupo caen en días distintos.
    - `PuedeMatricular` rechaza el mismo curso (RN-05) y una franja compartida (RN-06), y acepta otra franja de otro curso.
    - `LiberarMatricula` vuelve a permitir la franja y el curso.
    - La misma semilla da la misma secuencia de asignaciones.
  - `FechasPruebaTests`:
    - Para cada día de 2028, que es bisiesto: los tres periodos cumplen `ReglasPeriodo.Coherente`, sus clases no se traslapan (`ClasesSeTraslapan`) y sus nombres son distintos.
    - El vigente contiene `hoy` en su matrícula (`EnRangoMatricula`) y permite el retiro (`PermiteRetiro`).
    - Un caso fijo: el nombre para un inicio en mayo de 2027 es `2027-T2`.
  - Resultado (2026-11-16): `dotnet build` sin warnings; 1104/1104 pruebas unitarias en verde (752 nuevas: 732 son las dos teorías que recorren los 366 días de 2028, y 20 son de `PlanificadorHorariosTests` y `FechasPruebaTests`). No requiere verificación manual.
  - Detalles que la tarea no fijaba:
    - **Sin el límite de 20 intentos.** `IntentarAsignarGrupo` recorre en orden aleatorio **todas** las combinaciones de franjas: 32 con un bloque y 420 con dos en días distintos. Así `null` significa que de verdad no queda espacio. Con 20 intentos al azar, la prueba "hasta agotar las franjas" fallaba cerca del final: con 1 franja libre de 32, 20 intentos la encuentran solo la mitad de las veces. El costo sigue siendo bajo: a lo sumo 420 combinaciones por grupo.
    - `cantidadBloques` va de 1 a `MaxBloques` (2), como dice la spec. Fuera de ese rango lanza `ArgumentOutOfRangeException`.
    - `RegistrarMatricula` lanza `InvalidOperationException` si la matrícula no se permite. Así un error del orquestador se detecta en lugar de dejar datos que violan RN-05 o RN-06.
    - `PeriodoPrueba`, `PeriodosPrueba` y `AsignacionGrupo` son `record` en su propio archivo, por la convención de un archivo por clase.
    - Pruebas adicionales: la lista de franjas (32, sin domingo y sin repetidas), sin profesores devuelve `null`, una semilla distinta da otra secuencia, el periodo pasado ya terminó y el futuro no ha empezado, y los extremos del nombre por trimestre.
- Files: `src/Matricula.Infrastructure/Seed/DatosPrueba/{Franja,FechasPrueba,PeriodoPrueba,PeriodosPrueba,AsignacionGrupo,PlanificadorHorarios}.cs`, `tests/Matricula.UnitTests/Seed/{PlanificadorHorariosTests,FechasPruebaTests}.cs`

### T3. Catálogos y personas
- Acceptance:
  - `CatalogosPrueba` (unos 25 cursos con su área, prefijos de código y especialidades) y `GeneradorPersonas` (Bogus `es`, cédula, teléfono y correo únicos, recorte a las longitudes de EF).
  - La transacción y el primer guardado:
    - cursos, aulas y los tres periodos de `FechasPrueba`;
    - profesores y estudiantes con sus cuentas, la contraseña hasheada una sola vez y las cuentas fijas `profesor1`, `profesor2`, `estudiante1` y `estudiante2`;
    - temporal pendiente en `profesor2` y `estudiante2`;
    - alrededor de un 10 % de estudiantes desactivados y un profesor desactivado, nunca de las cuentas fijas.
  - El log de la tarea escribe los totales de esta capa.
- Verify: manual, con la BD recreada, el flag activo y una contraseña en user-secrets:
  - `psql`: las cantidades por tabla coinciden con la configuración; hay 3 periodos con fechas relativas a hoy, y `usuario` tiene `profesores + estudiantes + 1` filas.
  - Ninguna cédula, correo, código ni nombre de periodo repetido. Todos los correos terminan en `@prueba.test`.
  - Login de `estudiante1` y `profesor1` → `200`. Login de `estudiante2` → cambio obligatorio pendiente (RN-24). Login de un estudiante desactivado → rechazado.
  - Dos siembras con `Semilla = 123`, con drop en medio: las mismas cédulas y correos. Con `Semilla = null`: distintos.
  - `GET /api/v1/auditoria?entidad=Curso` → creaciones con autor Sistema.
  - smtp4dev sin correos nuevos.
  - Resultado (2026-11-16): `dotnet build` sin warnings; 1104/1104 pruebas unitarias en verde. Verificación manual sobre la BD de desarrollo `matricula`, borrada y recreada en cada siembra (autorizado por el usuario), con el flag y la contraseña en sus user-secrets:

    | Escenario | Resultado |
    |---|---|
    | Drop, update y arranque con los valores por defecto | El log muestra "Datos de prueba sembrados con la semilla 907205199: 8 cursos, 5 aulas, 3 periodos, 6 profesores y 30 estudiantes (3 desactivados)". El arranque completo tardó unos 4 s |
    | Cantidades en `psql` | 8 cursos, 5 aulas, 3 periodos, 6 profesores, 30 estudiantes y 37 usuarios (36 + el administrador inicial); 0 grupos y 0 matrículas |
    | Periodos (hoy = 2026-11-16) | `2026-T2` (pasado: clases del 20/05 al 17/09), `2026-T4` (vigente: matrícula del 06/11 al 26/11, retiro hasta el 16/12) y `2027-T2` (futuro) |
    | Repetidos: cédulas de estudiantes y de profesores, correos, códigos de curso y de aula, nombres de periodo | 0 en todos. Todos los correos de prueba terminan en `@prueba.test` |
    | Cuentas | `profesor1` y `estudiante1` con definitiva; `profesor2` y `estudiante2` con temporal y sin definitiva; 1 profesor y 3 estudiantes desactivados, ninguno de las cuentas fijas. Los registrados por el administrador tienen `creado_por_id = 1` |
    | Login de `estudiante1` y `profesor1` | `200`, `debeCambiarContrasena: false` |
    | Login de `estudiante2` y `profesor2` | `200`, `debeCambiarContrasena: true` (RN-24) |
    | Login de un estudiante desactivado | `403`, `RN-29` "La cuenta está desactivada…" |
    | Dos siembras con `DatosPrueba__Semilla=123`, con drop en medio, y una sin semilla | El hash MD5 de cédulas, correos y cursos es igual en las dos primeras (`b1e6e640…`) y distinto en la tercera |
    | Reiniciar la API sin borrar la BD | "La BD ya tiene datos; no se siembran datos de prueba" |
    | `registro_auditoria` | Una `Creacion` por cada dato: 8 `Curso`, 5 `Aula`, 3 `Periodo`, 6 `Profesor`, 30 `Estudiante` y 37 `Usuario` (con el administrador), todas sin `usuario_id` (autor Sistema) |
    | Correos | smtp4dev no estaba corriendo, así que un envío habría fallado y dejado un registro en `registro_error`: 0 registros y ningún "No se pudo enviar" en el log |
    | La contraseña de prueba en los logs | No aparece |

  - Detalles que la tarea no fijaba:
    - **Direcciones costarricenses:** la configuración regional `es` de Bogus genera direcciones de España ("Marbella", "Mataró"). `GeneradorPersonas.Direccion` las arma con un cantón y su provincia de una lista fija y señas: "Montes de Oca, San José. 400 m al sur del supermercado".
    - **Descripciones de curso:** usan el nombre del curso tal cual ("Aprenda Excel básico…"). Pasarlo a minúsculas dañaba nombres propios y siglas ("excel", "sql").
    - Los correos generados llevan un número de secuencia común a profesores y estudiantes, así no chocan entre roles.
    - `GeneradorPersonas` recibe el `Faker`; la clase `DatosSembrados` lleva las entidades de una capa a la siguiente.
    - La auditoría se revisó con `psql`. No se usó `GET /api/v1/auditoria` porque exigía cambiar la contraseña del administrador inicial, que quedó con su temporal.
- Files: `src/Matricula.Infrastructure/Seed/DatosPrueba/{CatalogosPrueba,GeneradorPersonas,DatosSembrados,DatosPruebaSeedService}.cs`

### T4. Grupos y horarios
- Acceptance:
  - Segundo guardado, con un `PlanificadorHorarios` por periodo:
    - grupos con NRC único, modalidad, cupo (RN-09) y 1 o 2 bloques con `AgregarHorario`;
    - los roles del periodo vigente según la prioridad de la spec;
    - los grupos que recibirán matrículas, abiertos (`Planificado → Abierto`);
    - en el periodo futuro, todos `Planificado` y uno sin horarios;
    - el profesor desactivado, sin grupos en los periodos vigente y futuro.
  - `LogWarning` por cada grupo que no se pudo crear.
- Verify: manual, con la BD recreada:
  - `psql`: hay `GruposPorPeriodo` grupos por periodo, y estas consultas devuelven 0 filas:
    - dos horarios del mismo profesor, día y hora en el mismo periodo (RN-07);
    - lo mismo para el aula (RN-08);
    - grupos con `cupo > capacidad` del aula (RN-09);
    - grupos virtuales con aula.
  - El grupo futuro sin horarios: `POST /api/v1/grupos/{id}/estado` con `Abierto` → `409 RF-47`.
  - `GET /api/v1/oferta-academica` del periodo vigente muestra los grupos abiertos.
  - Con `Profesores = 1` y `GruposPorPeriodo = 50`: se crean menos grupos, con un `LogWarning` por cada faltante, y la API arranca.
  - Atomicidad: con `horario` renombrada en `psql`, la siembra falla y no quedan cursos ni personas. Se revierte el `ALTER`.
  - Resultado (2026-11-16): `dotnet build` sin warnings; 1104/1104 pruebas unitarias en verde. Verificación manual sobre la BD de desarrollo `matricula`, recreada en cada siembra:

    | Escenario | Resultado |
    |---|---|
    | Siembra con los valores por defecto | "Grupos de prueba: 15 (2026-T2: 4 Finalizado 1 Cancelado, 2026-T4: 2 Abierto 1 Cancelado 1 Planificado 1 Cerrado, 2027-T2: 5 Planificado); 0 sin crear por falta de franjas". Son los estados **finales**: en la BD, los grupos que recibirán matrículas están `Abierto`, y T5 los lleva a su estado final |
    | Consultas `psql`: traslapes de profesor (RN-07), de aula (RN-08) y dentro del mismo grupo (RN-14), cupo mayor que la capacidad (RN-09), virtual con aula o presencial sin aula, profesor desactivado en los periodos vigente o futuro | 0 en todas. 5 grupos por periodo. Un solo grupo sin horarios: el primero del periodo futuro |
    | El grupo lleno del periodo vigente | Cupo 8 (de 5 a 8, para que se pueda llenar) y el mismo curso que el primer grupo abierto |
    | `GET /api/v1/oferta-academica` | Los 4 grupos `Abierto` del periodo vigente con sus cupos disponibles; no aparece el `Planificado` |
    | `Profesores = 1` y `GruposPorPeriodo = 50` | La API arranca. 69 grupos creados y 81 sin crear, con un `LogWarning` por cada uno. Las consultas de traslapes siguen en 0 |
    | Atomicidad: BD recreada, `horario` renombrada con `psql` y arranque | La siembra falla con `42P01` al guardar los grupos, y la API no arranca. No quedan cursos, aulas, periodos, estudiantes ni grupos: solo el administrador inicial y sus 2 registros de auditoría, que se crean antes. Se revirtió el `ALTER` |
    | `registro_auditoria` | 15 `Grupo` y 19 `Horario` con `Creacion` y autor Sistema |
    | Abrir el grupo futuro sin horarios por API (`409 RF-47`) | **Pendiente, pasa a T6.** Hace falta un token de administrador, y el administrador inicial tiene su temporal pendiente. El clasificador de permisos de la sesión no permitió hacerle el cambio obligatorio sin consultar al usuario. La regla la cubre la prueba unitaria `GrupoTests.CambiarEstado_AbrirSinHorarios_LanzaRf47` |

  - Detalles que la tarea no fijaba:
    - **`SembradorGrupos`** arma los grupos en memoria, en su propia clase, para que el servicio quede como orquestador: agrega, guarda y escribe el log. `GrupoSembrado` guarda el grupo con sus franjas y su estado final, y `DatosSembrados.Planificadores` guarda un planificador por periodo, que T5 reutiliza para los estudiantes.
    - **El estado de cada grupo depende de cuántos se crearon**, no del número de intento, para no perder un rol cuando un grupo no cabe. El cancelado del periodo pasado es el tercero creado; antes era el último intento, y con `GruposPorPeriodo = 50` no se creaba.
    - **El grupo lleno** tiene un cupo de 5 a 8, sin pasar la capacidad del aula, y el mismo curso que el primer abierto, para que T5 pueda hacer el cambio de grupo.
    - El profesor desactivado solo puede tener grupos en el periodo pasado, que terminan finalizados o cancelados: RN-37 impide desactivar a un profesor con grupos activos.
- Files: `src/Matricula.Infrastructure/Seed/DatosPrueba/{SembradorGrupos,GrupoSembrado,DatosSembrados,DatosPruebaSeedService}.cs`

### T5. Matrículas y estados finales
- Acceptance:
  - Tercer guardado:
    - matrículas en los periodos pasado y vigente, con `OcuparCupo`, fechas dentro del rango de matrícula y, en el vigente, no posteriores a hoy;
    - el grupo lleno;
    - alrededor de un 10 % de retiros voluntarios;
    - un cambio de grupo cuando sea posible;
    - cancelaciones con RN-12;
    - cierre del grupo `Cerrado`;
    - finalización de los grupos del periodo pasado.
  - Resumen final en el log, con la semilla, los totales por tabla, los grupos por estado y las matrículas por estado y motivo.
- Verify: manual, con la BD recreada y los valores por defecto:
  - Estas consultas `psql` devuelven 0 filas:
    - grupos con `cupos_ocupados` distinto del conteo de matrículas activas;
    - dos matrículas activas del mismo estudiante en el mismo curso y periodo (RN-05);
    - matrículas activas del mismo estudiante con horarios traslapados (RN-06);
    - matrículas activas de estudiantes desactivados en el periodo vigente;
    - matrículas activas en grupos `Cancelado`;
    - fechas de matrícula fuera del rango de su periodo o posteriores a hoy.
  - Hay grupos `Finalizado`, `Abierto` (uno lleno), `Cerrado`, `Cancelado` y `Planificado`, y matrículas `Retirada` por los tres motivos.
  - `estudiante1`: `GET /api/v1/me/matriculas` con datos de los dos periodos. `profesor1`: `GET /api/v1/me/grupos` con datos.
  - Sobre el periodo vigente, como `estudiante1` o el admin: matricular, retirar y cambiar de grupo responden como en la operación normal.
  - Tiempo de la siembra con los valores por defecto: menos de 10 s. Se anota también el de `Estudiantes = 2000`.
- Files: `src/Matricula.Infrastructure/Seed/DatosPrueba/DatosPruebaSeedService.cs`

### T6. Cierre del módulo
- Acceptance:
  - `README.md`: cómo activar los datos de prueba (user-secrets), qué cuentas se crean y cómo reiniciar la BD.
  - `AGENTS.md`: los comandos de reinicio de la BD y la mención de los datos de prueba en la estructura.
  - Toda la tabla de verificación manual de la spec recorrida. Lo que no cubran T1 a T5 se cubre aquí.
  - Pendiente de T4: abrir por API el grupo del periodo futuro sin horarios → `409 RF-47`. Necesita un token de administrador (el usuario decide cómo obtenerlo).
  - Revisión del log completo de una siembra: no contiene la contraseña.
  - Criterios de éxito de la spec cumplidos.
  - Estados al día en la spec, este `todo.md` y `specs/mapa-modulos.md` (módulo **Completado**).
- Verify: `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde y la tabla de verificación completa.
- Files: `README.md`, `AGENTS.md`, `specs/SPEC-datos-prueba.md`, `specs/mapa-modulos.md`, `tasks/datos-prueba/todo.md`
