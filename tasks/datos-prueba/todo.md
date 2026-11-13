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
| T1. Base: paquete, opciones y guardas | Pendiente | ⬜ | ⬜ | ⬜ | ⬜ |
| T2. `PlanificadorHorarios` y `FechasPrueba` | Pendiente | ⬜ | ⬜ | ➖ | ⬜ |
| T3. Catálogos y personas | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
| T4. Grupos y horarios | Pendiente | ⬜ | ➖ | ⬜ | ⬜ |
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
- Files: `src/Matricula.Infrastructure/Matricula.Infrastructure.csproj`, `src/Matricula.Infrastructure/Seed/DatosPrueba/{DatosPruebaOptions,DatosPruebaSeedService}.cs`, `src/Matricula.Infrastructure/Seed/SeedExtensions.cs`, `src/Matricula.Infrastructure/DependencyInjection.cs`, `src/Matricula.Api/{Program.cs,appsettings.json}`, `tests/Matricula.UnitTests/Matricula.UnitTests.csproj`, `tests/Matricula.UnitTests/Seed/DatosPruebaOptionsTests.cs`

### T2. `PlanificadorHorarios` y `FechasPrueba`
- Acceptance:
  - `Franja` (`record`) y la lista fija de franjas de la spec: lunes a viernes, seis franjas; sábado, dos.
  - `FechasPrueba.Calcular(DateOnly hoy)`: las fechas y el nombre de los periodos pasado, vigente y futuro, con los desplazamientos de la spec y el formato `<año>-T<trimestre>`.
  - `PlanificadorHorarios(Random random)`:
    - `IntentarAsignarGrupo(int cantidadBloques, IReadOnlyList<int> profesores, IReadOnlyList<int>? aulas)`: franjas en días distintos y un profesor y un aula libres en todas ellas, o `null` después de 20 intentos.
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
- Files: `src/Matricula.Infrastructure/Seed/DatosPrueba/{Franja,FechasPrueba,PlanificadorHorarios}.cs`, `tests/Matricula.UnitTests/Seed/{PlanificadorHorariosTests,FechasPruebaTests}.cs`

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
- Files: `src/Matricula.Infrastructure/Seed/DatosPrueba/{CatalogosPrueba,GeneradorPersonas,DatosPruebaSeedService}.cs`

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
- Files: `src/Matricula.Infrastructure/Seed/DatosPrueba/DatosPruebaSeedService.cs`

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
  - Revisión del log completo de una siembra: no contiene la contraseña.
  - Criterios de éxito de la spec cumplidos.
  - Estados al día en la spec, este `todo.md` y `specs/mapa-modulos.md` (módulo **Completado**).
- Verify: `dotnet build` sin warnings, `dotnet test tests/Matricula.UnitTests` en verde y la tabla de verificación completa.
- Files: `README.md`, `AGENTS.md`, `specs/SPEC-datos-prueba.md`, `specs/mapa-modulos.md`, `tasks/datos-prueba/todo.md`
