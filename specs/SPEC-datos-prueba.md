# Spec: datos-prueba

Módulo `datos-prueba` del [mapa de módulos](mapa-modulos.md). Siembra datos de prueba en todas las tablas de negocio, así que depende de todos los módulos de datos (`acceso`, `estudiantes`, `profesores`, `catalogos`, `grupos`, `matriculas`) y se construye después de `errores`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/datos-prueba/plan.md) | [Aprobadas](../tasks/datos-prueba/todo.md) | 5/6 tareas completadas |

**Historial:**
- 2026-11-13: v1 en revisión. Decisiones del usuario antes de redactarla: módulo aparte con el flujo spec-driven, datos variados con **Bogus**, activación con un flag en la configuración, cantidades configurables en `appsettings` y reinicio de la BD con `drop` + `update`.
- 2026-11-13: v1 aprobada con sus siete supuestos. Plan y lista de 6 tareas en revisión. Correcciones menores sin cambio de alcance:
  - El origen de registro se llama `RegistroPublico`, no `Publico`.
  - La versión de Bogus queda fijada en 35.6.5, la estable más reciente en NuGet.
- 2026-11-13: plan y lista de tareas aprobados. Las tareas aún no comienzan.
- 2026-11-16: T1 (base: paquete, opciones y guardas) completada, sin cambios de alcance.
- 2026-11-16: T2 (planificador y fechas) completada, sin cambios de alcance. El planificador recorre todas las combinaciones de franjas en lugar de 20 intentos al azar, para que "sin espacio" sea exacto (detalle en `todo.md`).
- 2026-11-16: T3 (catálogos y personas) completada, sin cambios de alcance. Las direcciones se arman con cantones de Costa Rica y señas, porque las de Bogus en español son de España (detalle en `todo.md`). La BD de desarrollo queda con los datos sembrados.
- 2026-11-16: T4 (grupos y horarios) completada, sin cambios de alcance. La verificación por API de RF-47 (abrir el grupo sin horarios) pasa a T6, porque necesita un token de administrador.
- 2026-11-16: **cambio de alcance aprobado por el usuario.** La siembra también asigna `DatosPrueba:Contrasena` como contraseña definitiva del administrador inicial (punto 6), para usarlo de inmediato en desarrollo. Se descartó cambiar el `AdminSeed` general, porque los requerimientos piden que el administrador inicial nazca con una temporal y eso protege a producción. Se implementa en T5.
- 2026-11-16: T5 (matrículas y estados finales) completada, con el cambio del administrador inicial. Se cumplió la verificación de RF-47 pendiente de T4. Ajuste menor: `profesor1` da el primer grupo de los periodos pasado y vigente, para garantizar la tabla de cuentas fijas del punto 6.

## Objetivo

Tener la BD de desarrollo poblada con datos realistas y coherentes, sin crearlos a mano desde Scalar. Hoy, después de recrear la BD, solo existe el administrador inicial. Hay que registrar cursos, aulas, periodos, profesores, estudiantes, grupos con horarios y matrículas antes de poder probar algo. Con este módulo, al arrancar la API en desarrollo con el flag activo, la BD vacía se llena con datos que cubren los estados principales del sistema y que se pueden usar de inmediato, incluso para iniciar sesión.

No cubre ningún RF ni RN nuevo. Es una herramienta de desarrollo que **respeta** las reglas existentes (RN-01 a RN-16, RN-13, RN-26, RN-31, RN-38, RN-42).

**Usuarios:** el desarrollador. No hay endpoints ni cambios visibles para los roles del sistema.

### Alcance

1. **Activación** (supuesto 1):
   - `DatosPruebaOptions` (sección `DatosPrueba`) en `Infrastructure/Seed/DatosPrueba/`.
   - `Program.cs` llama a `SembrarDatosPruebaAsync` **después** de `SembrarAdministradorInicialAsync`.
   - Solo se ejecuta si `DatosPrueba:Habilitado = true` **y** el entorno es `Development`. Si el flag está activo en otro entorno, escribe un `LogWarning` y no hace nada.
   - `Habilitado` es `false` por defecto en `appsettings.json`. Se activa en `appsettings.Development.json` o en user-secrets.

2. **Configuración** (`appsettings.json`, con estos valores por defecto):

   ```json
   "DatosPrueba": {
     "Habilitado": false,
     "Semilla": null,
     "Cursos": 8,
     "Aulas": 5,
     "Profesores": 6,
     "Estudiantes": 30,
     "GruposPorPeriodo": 5,
     "MaxMatriculasPorEstudiante": 2
   }
   ```

   | Clave | Rango | Significado |
   |---|---|---|
   | `Habilitado` | — | Flag de activación (punto 1) |
   | `Semilla` | `int?` | `null`: datos distintos en cada siembra. Un número: los mismos datos siempre (supuesto 2). La semilla usada se escribe en el log |
   | `Cursos` | 1–200 | Cursos activos |
   | `Aulas` | 1–100 | Aulas activas |
   | `Profesores` | 1–200 | Profesores con su cuenta |
   | `Estudiantes` | 0–5000 | Estudiantes con su cuenta |
   | `GruposPorPeriodo` | 0–200 | Grupos que se intentan crear en cada uno de los 3 periodos (punto 5) |
   | `MaxMatriculasPorEstudiante` | 0–6 | Máximo de matrículas por estudiante **en cada periodo**. Cada estudiante recibe un número al azar entre 0 y el máximo |
   | `Contrasena` | Cumple RN-26 | **Solo en user-secrets** (`DatosPrueba:Contrasena`). La contraseña de todas las cuentas de prueba (punto 6) |

   - Los rangos y `Contrasena` se validan al arrancar **solo si `Habilitado = true`**. Si fallan, la API no arranca y el mensaje indica la clave. `Contrasena` se valida con `PoliticaContrasena` (RN-26).
   - Con los valores por defecto se crean unos 15 grupos y unas 60 matrículas.

3. **Solo en una BD sin datos de negocio** (idempotente, supuesto 3):
   - Si existe al menos un curso, aula, periodo, profesor, estudiante, grupo o matrícula, escribe un `LogInformation` ("La BD ya tiene datos; no se siembran datos de prueba") y termina sin cambios.
   - El administrador inicial no cuenta: lo crea `AdminSeedService` antes.
   - Para volver a sembrar, se borra y se recrea la BD (ver Commands).

4. **Todo o nada:**
   - Toda la siembra ocurre en **una transacción** (`BeginTransactionAsync`), con varios `SaveChangesAsync` porque los grupos necesitan los Ids de cursos, profesores y periodos. Si algo falla, no queda nada sembrado y la excepción detiene el arranque con un mensaje claro, como el seed del administrador.
   - Todas las escrituras pasan por el change tracker y `SaveChangesAsync`: la auditoría registra cada dato creado con autor **Sistema** (RN-42). No se usa SQL ni `ExecuteUpdate`.
   - Al terminar, escribe un `LogInformation` con lo creado: cursos, aulas, periodos, profesores, estudiantes, grupos por estado y matrículas activas y retiradas, más la semilla usada.

5. **Qué se siembra**, en el orden de las FK. Las entidades se crean con sus constructores y métodos de dominio, nunca asignando propiedades. Los textos cumplen las mismas validaciones que los DTO de entrada (longitudes, formato de cédula y teléfono).

   **Cursos.** Nombres de un catálogo fijo de unos 25 cursos plausibles de un instituto (por ejemplo, "Excel intermedio" o "Inglés conversacional"), elegidos al azar sin repetir. Si se piden más que el catálogo, se agrega un sufijo ("II", "III"…). Además:
   - código único derivado del área, como `INF-101`;
   - descripción generada por Bogus;
   - duración de 20 a 80 horas, en múltiplos de 4;
   - costo de ₡25 000 a ₡150 000, en múltiplos de 5000;
   - nivel al azar.

   **Aulas.** Código único (`A-101`, `B-204`…), ubicación ("Edificio A, piso 1") y capacidad de 15 a 40.

   **Periodos.** Siempre son **3**, con fechas relativas a `IFechaLocal.Hoy()` para que los datos sigan siendo útiles con el tiempo. Las clases de los tres no se traslapan (RN-06 entre periodos) y todos cumplen RN-16:

   | Periodo | Matrícula | Retiro hasta | Clases | Estados de sus grupos |
   |---|---|---|---|---|
   | Pasado | hoy−200 a hoy−175 | hoy−150 | hoy−180 a hoy−60 | `Finalizado`; uno `Cancelado` si hay 3 o más grupos |
   | Vigente | hoy−10 a hoy+10 | hoy+30 | hoy+15 a hoy+120 | Ver abajo |
   | Futuro | hoy+130 a hoy+145 | hoy+170 | hoy+150 a hoy+260 | `Planificado`; uno sin horarios |

   - Nombre único con el formato `<año>-T<trimestre>` de su inicio de clases, por ejemplo `2027-T1`.
   - **Grupos del periodo vigente**, en este orden de prioridad hasta agotar `GruposPorPeriodo`:
     1. `Abierto` con cupos;
     2. `Abierto` lleno (`CuposOcupados = Cupo`);
     3. `Cancelado`, con sus matrículas retiradas por `CancelacionGrupo` (RN-12);
     4. `Planificado`;
     5. `Cerrado`, con matrículas;
     6. los demás, `Abierto`.
   - El periodo vigente queda dentro del rango de matrícula y de retiro, así que se puede matricular, retirar y cambiar de grupo de inmediato.

   **Profesores.** Para cada uno:
   - nombre, apellidos y teléfono generados por Bogus, con la configuración regional `es`;
   - cédula única de 9 dígitos;
   - especialidad de una lista fija;
   - fecha de contratación en los últimos 10 años;
   - una cuenta con rol `Profesor` y origen `Administrador`.

   **Estudiantes.** Para cada uno:
   - nombre, apellidos, teléfono y dirección generados por Bogus;
   - cédula única;
   - fecha de nacimiento entre 16 y 60 años atrás;
   - fecha de registro anterior al inicio de la matrícula del periodo pasado;
   - una cuenta con rol `Estudiante` y origen `RegistroPublico` o `Administrador`, al azar.

   **Grupos y horarios** (punto 7). Cada grupo tiene:
   - NRC único de 5 dígitos;
   - curso y profesor al azar;
   - modalidad: alrededor de un 20 % `Virtual` sin aula; el resto `Presencial` o `Hibrida`, con aula;
   - cupo: de 10 a 30 sin pasar la capacidad del aula (RN-09); de 15 a 40 si es virtual;
   - 1 o 2 bloques de horario en días distintos.

   Avanzan de estado paso a paso con `CambiarEstado` (RF-47).

   **Matrículas.** En los periodos pasado y vigente, cada estudiante activo recibe de 0 a `MaxMatriculasPorEstudiante` matrículas en grupos que se lo permitan. Se crean con el constructor de `Matricula` y `Grupo.OcuparCupo()`, para que `CuposOcupados` coincida siempre con las matrículas activas. Además:
   - La fecha de matrícula cae dentro del rango de matrícula del periodo y, en el vigente, no después de hoy.
   - Alrededor de un 10 % se retiran con `Retirar(fecha, RetiroVoluntario)` y `LiberarCupo()`, con una fecha anterior al límite de retiro y, en el vigente, no posterior a hoy.
   - Si en el periodo vigente hay dos grupos `Abierto` del mismo curso, un estudiante tiene un cambio de grupo: una matrícula retirada por `CambioGrupo` y la nueva activa en el otro grupo.
   - Los grupos `Cancelado` retiran sus matrículas con `CancelacionGrupo` (RN-12). Los grupos del periodo pasado se finalizan después de matricular.

6. **Cuentas para iniciar sesión** (supuesto 4):
   - Todas las cuentas de prueba tienen como contraseña definitiva `DatosPrueba:Contrasena`, asignada con `EstablecerContrasena(hasher.Hashear(...))`. No se generan temporales ni se envían correos.
   - **Cuentas fijas** para probar sin buscar correos:

     | Correo | Caso |
     |---|---|
     | `profesor1@prueba.test` | Profesor activo con grupos en los periodos pasado y vigente |
     | `estudiante1@prueba.test` | Estudiante activo con matrículas en los periodos pasado y vigente |
     | `profesor2@prueba.test`, `estudiante2@prueba.test` | Cambio obligatorio pendiente (RN-24): sin definitiva y con una temporal (`CuentaNueva`) igual a `DatosPrueba:Contrasena`. Vence a las 72 horas; para renovarla, se vuelve a sembrar |

   - El resto de los correos se generan a partir del nombre, sin tildes, con un número que garantiza que sean únicos: `nombre.apellido.N@prueba.test`. El dominio `.test` está reservado (RFC 6761) y nunca corresponde a un buzón real.
   - Alrededor de un 10 % de los estudiantes (nunca `estudiante1` ni `estudiante2`) y un profesor sin grupos en los periodos vigente y futuro quedan **desactivados** (`Usuario.Desactivar()`, RN-38). Los estudiantes desactivados no tienen matrículas activas en el periodo vigente.
   - **Administrador inicial** (cambio del 2026-11-16, aprobado por el usuario): la siembra también le asigna `DatosPrueba:Contrasena` como contraseña definitiva, con `EstablecerContrasena`, que anula su temporal. Así `admin@instituto.local` se usa de inmediato, igual que las cuentas de prueba.
     - Solo ocurre cuando se siembran datos de prueba, es decir, con las guardas del punto 1 y del punto 3. El `AdminSeed` no cambia: fuera de la siembra, el administrador inicial sigue naciendo con su temporal, como piden los requerimientos (§5.4 y "Datos iniciales").
     - Como el administrador ya tiene contraseña definitiva, el `AdminSeed` deja de renovarle la temporal en los siguientes arranques.
     - Va en el mismo guardado y la misma transacción que el resto de la siembra, y la auditoría lo registra como una modificación del `Usuario` con autor Sistema.

7. **Asignación de horarios sin traslapes** (RN-06, RN-07, RN-08, RN-14), con un `PlanificadorHorarios` en memoria por periodo:
   - Usa **franjas fijas** de 2 horas: lunes a viernes de 08:00 a 10:00, 10:00 a 12:00, 13:00 a 15:00, 15:00 a 17:00, 18:00 a 20:00 y 20:00 a 22:00, y el sábado de 08:00 a 10:00 y 10:00 a 12:00. Como las franjas no se cruzan, "traslaparse" equivale a compartir día y franja.
   - Para cada grupo elige franjas libres al azar y un profesor y un aula libres en esas franjas. Si no encuentra ninguna combinación, el grupo no se crea, se escribe un `LogWarning` y el total del resumen lo refleja.
   - Para cada matrícula elige un grupo `Abierto`, `Cerrado`, `Cancelado` o `Finalizado` del periodo que:
     - tenga cupo;
     - sea de un curso distinto a los demás del estudiante en el periodo (RN-05);
     - no comparta franjas con sus otros grupos (RN-06).

     Si no hay ninguno, el estudiante se queda con menos matrículas.
   - Los grupos `Abierto` y `Cerrado` se matriculan antes de cerrarse, igual que en la operación normal.

8. **Generación con Bogus:**
   - Un `Faker` con la configuración regional `es` y `Randomizer.Seed` inicializado con la semilla (punto 2), para que una misma semilla dé los mismos datos.
   - Bogus solo genera textos y números; las entidades se crean con los constructores del dominio.
   - La unicidad de cédulas, correos, códigos y NRC se garantiza con un `HashSet` y reintentos, no confiando en el azar.

### Fuera de alcance

- Sembrar `registro_error`: solo se llena con errores reales.
- Escribir registros de auditoría a mano: los genera la captura automática.
- Administradores adicionales: solo existe el administrador inicial.
- Grupos `EnCurso`: el periodo vigente aún no empezó clases. Se puede probar avanzando un grupo `Cerrado` desde la API.
- Un endpoint o comando para borrar los datos: se reinicia la BD (ver Commands).
- Sembrar en `Production` o en la BD de pruebas `matricula_tests`.
- Pruebas de integración.

## Tech Stack

.NET 10, EF Core 10 + Npgsql 10.0.3, xUnit 2.9.3. **Un paquete nuevo, aprobado por el usuario:** **Bogus 35.6.5** (licencia MIT) en Infrastructure.

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet user-secrets set "DatosPrueba:Contrasena" "<contraseña RN-26>" --project src/Matricula.Api
dotnet user-secrets set "DatosPrueba:Habilitado" "true" --project src/Matricula.Api

# Empezar de cero: borrar la BD, recrearla con las migraciones y arrancar (siembra el admin y los datos)
dotnet ef database drop --force --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api
```

## Project Structure

```
src/Matricula.Infrastructure/
  Seed/DatosPrueba/DatosPruebaOptions.cs           nuevo, con su validación condicionada a Habilitado
  Seed/DatosPrueba/DatosPruebaSeedService.cs       nuevo: orquesta la siembra en una transacción
  Seed/DatosPrueba/PlanificadorHorarios.cs         nuevo: franjas libres por profesor, aula y estudiante
  Seed/DatosPrueba/CatalogosPrueba.cs              nuevo: nombres de cursos, áreas y especialidades
  Seed/SeedExtensions.cs                           + SembrarDatosPruebaAsync
  DependencyInjection.cs                           + opciones y servicio
  Matricula.Infrastructure.csproj                  + Bogus
src/Matricula.Api/
  Program.cs                                       + llamada después del seed del administrador
  appsettings.json                                 + sección DatosPrueba (Habilitado = false)
tests/Matricula.UnitTests/
  Matricula.UnitTests.csproj                       + referencia a Infrastructure (supuesto 6)
  Seed/PlanificadorHorariosTests.cs
README.md                                          cómo activar los datos de prueba y reiniciar la BD
AGENTS.md                                          comando de reinicio y mención del módulo
```

## Code Style

Igual que los módulos anteriores: constructores primarios, un archivo por clase, `[LoggerMessage]` para los logs, `async` con `CancellationToken`, `TimeProvider` e `IFechaLocal` para las fechas y pruebas con el formato `Metodo_Escenario_Resultado`. Un método privado por bloque en el orden de las FK:

```csharp
public async Task EjecutarAsync(CancellationToken cancellationToken)
{
    if (await HayDatosDeNegocioAsync(cancellationToken))
    {
        LogBdConDatos(logger);
        return;
    }

    var semilla = _options.Semilla ?? Random.Shared.Next();
    Randomizer.Seed = new Random(semilla);
    var hoy = fechaLocal.Hoy();

    await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);
    var cursos = await SembrarCursosAsync(cancellationToken);
    // … aulas, periodos, profesores, estudiantes, grupos y matrículas
    await transaccion.CommitAsync(cancellationToken);

    LogResumen(logger, semilla, /* totales */);
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD), sobre `PlanificadorHorarios`:
  - nunca asigna a un profesor ni a un aula dos grupos en la misma franja;
  - devuelve "sin espacio" cuando todas las franjas del profesor o del aula están ocupadas;
  - rechaza para un estudiante un grupo que comparte una franja o el curso con otro suyo;
  - con la misma semilla produce la misma asignación.
- **Las reglas del dominio** (RN-16, RF-47, RN-03, RN-14) ya se aplican al construir las entidades: si la siembra las violara, fallaría al arrancar.
- **Manual**, con la API y PostgreSQL en marcha, usando `psql`, Scalar y `Matricula.Api.http`:

| Qué | Esperado |
|---|---|
| Drop, update y arranque con `Habilitado = true` | La API arranca. El log muestra el resumen y la semilla. Las tablas tienen las cantidades configuradas; hay menos grupos o matrículas solo si el log lo advierte |
| Reiniciar la API sin borrar la BD | "La BD ya tiene datos"; las cantidades no cambian |
| `Habilitado = false` (por defecto) en una BD vacía | Solo se crea el administrador inicial |
| `Habilitado = true` sin `Contrasena`, con una que no cumple RN-26 o con `Estudiantes = -1` | La API no arranca, con un mensaje que nombra la clave |
| `Habilitado = true` con `ASPNETCORE_ENVIRONMENT=Production` | `LogWarning` y no siembra |
| Dos siembras con la misma `Semilla` (con drop en medio) | Los mismos nombres, cédulas y correos. Con `Semilla = null`, distintos |
| `SELECT g.id FROM grupo g WHERE g.cupos_ocupados <> (SELECT count(*) FROM matricula m WHERE m.grupo_id = g.id AND m.estado = 'Activa')` | Sin filas |
| Consultas de traslapes por profesor, aula y estudiante en el mismo periodo (RN-06 a RN-08) y de grupos con cupo mayor que la capacidad del aula (RN-09) | Sin filas |
| Login como `estudiante1@prueba.test` y `profesor1@prueba.test` | `200` con token. `/api/v1/me/matriculas` y `/api/v1/me/grupos` devuelven datos |
| Login como `estudiante2@prueba.test` | Token con cambio obligatorio pendiente (RN-24) |
| Login como `admin@instituto.local` con `DatosPrueba:Contrasena`, y reiniciar la API | `200` sin cambio obligatorio. Después del reinicio, el log del `AdminSeed` ya no dice "Temporal del administrador inicial … renovada" |
| Arrancar con el flag apagado sobre una BD recreada | El administrador inicial nace con su temporal, como siempre |
| Login de un estudiante desactivado | Rechazado como cualquier cuenta inactiva |
| `GET /oferta-academica` del periodo vigente; matricular a `estudiante1`, retirar y cambiar de grupo | Hay grupos abiertos, uno sin cupos; las operaciones responden como en la operación normal |
| Abrir el grupo del periodo futuro sin horarios | `409 RF-47` |
| `GET /auditoria?entidad=Curso` | Registros de creación con autor Sistema |
| smtp4dev | Ningún correo enviado por la siembra |

## Boundaries

- **Siempre:**
  - `dotnet build` sin warnings y pruebas unitarias en verde.
  - Crear las entidades con su dominio y escribir con el change tracker y `SaveChangesAsync` en una transacción.
  - Fechas relativas a `IFechaLocal.Hoy()`.
  - La contraseña solo en user-secrets y nunca en el log.
  - Sembrar solo en `Development` y en una BD sin datos de negocio.
- **Preguntar antes:**
  - Cambiar la versión de Bogus o agregar otros paquetes.
  - Cambiar el esquema de la BD (este módulo no lo cambia).
  - Agregar endpoints o comandos para borrar o resembrar.
  - Sembrar en otros entornos.
- **Nunca:**
  - Usar `HasData`, SQL manual, `ExecuteUpdate` o `ExecuteDelete`.
  - Asignar propiedades saltándose los métodos del dominio.
  - Enviar correos.
  - Escribir contraseñas en `appsettings*.json` o en el log.
  - Apuntar a `matricula_tests`.

## Success Criteria

1. Con el flag activo, la BD recién recreada queda poblada al arrancar, con las cantidades de la configuración, sin intervención manual y en menos de 10 segundos con los valores por defecto.
2. Los datos cumplen las reglas del sistema: cupos consistentes, sin traslapes de profesor, aula ni estudiante, sin cupos mayores que la capacidad del aula y con fechas de periodo coherentes.
3. Los tres periodos cubren los estados principales: grupos `Finalizado`, `Abierto` (uno lleno), `Cerrado`, `Cancelado` y `Planificado`, y matrículas `Activa` y `Retirada` por los tres motivos cuando la cantidad de grupos lo permite.
4. Se puede iniciar sesión de inmediato con las cuentas fijas y con el administrador inicial, y operar sobre el periodo vigente (matricular, retirar, cambiar de grupo).
5. La misma semilla reproduce los mismos datos, y sin semilla cada siembra es distinta.
6. Con el flag desactivado, con datos existentes o fuera de `Development`, la siembra no hace nada.
7. Ningún correo enviado y ninguna contraseña en el log ni en `appsettings*.json`.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **Se ejecuta al arrancar la API, no con un comando aparte,** igual que el seed del administrador. El flag basta para controlarlo y no hace falta otro punto de entrada.
2. **Sin semilla, los datos cambian en cada siembra** (lo que pediste para tener variedad). Con una semilla fija se puede reproducir un caso concreto. La semilla usada siempre queda en el log.
3. **No se siembra sobre datos existentes,** ni siquiera para agregar más. Mezclar con datos creados a mano obligaría a revisar unicidad y traslapes contra la BD. Reiniciar la BD es más simple y predecible.
4. **Una sola contraseña para todas las cuentas de prueba,** en user-secrets, y cuentas fijas `profesor1`, `profesor2`, `estudiante1` y `estudiante2` para no tener que buscar correos.
5. **Siempre 3 periodos** (pasado, vigente y futuro). No se configuran porque la estructura de estados depende de los tres; lo configurable es cuántos grupos tiene cada uno.
6. **Las pruebas unitarias referencian Infrastructure** para probar `PlanificadorHorarios`, que no es una regla de negocio y por eso no va en Domain. La alternativa es no tener pruebas unitarias del módulo y verificarlo solo con las consultas de la tabla manual.
7. **Franjas fijas de 2 horas** en lugar de horas arbitrarias: simplifican garantizar que no haya traslapes y se parecen a un horario real de instituto.

## Decisiones

Se aplican las decisiones 1, 2, 7, 9, 10, 11 y 13 del mapa.

## Preguntas abiertas

Ninguna: las de alcance se resolvieron antes de redactar la spec. Los supuestos 1 a 7 quedan para la revisión.
