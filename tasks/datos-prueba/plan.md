# Plan: datos-prueba

Módulo `datos-prueba` del [mapa](../../specs/mapa-modulos.md). Spec: [`specs/SPEC-datos-prueba.md`](../../specs/SPEC-datos-prueba.md) · Tareas: [`todo.md`](todo.md)

**Estado del plan:** Aprobado (2026-11-13)

## Enfoque

El módulo no cambia el esquema ni agrega endpoints: todo pasa al arrancar. El orden es:
1. La base: paquete, opciones, validación y la llamada desde `Program.cs` con las tres guardas (flag, entorno y BD vacía), con un servicio que todavía no siembra nada. Así lo que decide **si** se siembra queda verificado antes de sembrar.
2. El `PlanificadorHorarios`, que es la parte con más lógica, probado con pruebas unitarias antes de usarlo.
3. La siembra por capas, en el orden de las FK. Cada tarea agrega una capa y se verifica contra la BD recién recreada:
   - catálogos y personas;
   - grupos con horarios;
   - matrículas y estados finales.
4. El cierre.

Cada tarea termina con `dotnet build` sin warnings, las pruebas unitarias en verde, su verificación manual y un commit pequeño.

Lo delicado de este módulo es que escribe saltándose los servicios, así que las reglas que normalmente valida el servicio (RN-05 a RN-09) dependen de que el planificador sea correcto. Por eso el planificador tiene pruebas unitarias propias (T2), y cada tarea de siembra se verifica con consultas `psql` que buscan violaciones: cupos inconsistentes, traslapes y cupos mayores que la capacidad del aula.

1. **Base:** Bogus, `DatosPruebaOptions` con su validación, `appsettings.json`, `SeedExtensions` y `Program.cs`. El servicio solo comprueba las guardas y escribe en el log.
2. **Planificador y fechas:** franjas, ocupación por profesor, aula y estudiante, elección al azar con semilla, y las fechas de los tres periodos.
3. **Catálogos y personas:** cursos, aulas, periodos, profesores y estudiantes con sus cuentas, en la transacción.
4. **Grupos y horarios:** los grupos de los tres periodos, con sus bloques y estados iniciales.
5. **Matrículas y estados finales:** matrículas, retiros, cambio de grupo, cancelaciones, cierres, finalizaciones y el resumen en el log.
6. **Cierre:** README, `AGENTS.md`, la tabla de verificación completa de la spec y los estados.

## Componentes y orden

| # | Componente | Depende de | Notas |
|---|---|---|---|
| 1 | **Base:** paquete, opciones, guardas, llamada desde `Program.cs` | — | Verificación con el log, sin datos |
| 2 | **`PlanificadorHorarios` y `FechasPrueba`** | — | Solo pruebas unitarias |
| 3 | **Catálogos y personas** | 1 | Primera siembra real; drop + update en cada verificación |
| 4 | **Grupos y horarios** | 2, 3 | Consultas de traslapes de profesor y aula |
| 5 | **Matrículas y estados finales** | 2, 4 | Consultas de cupos y traslapes de estudiante; login con las cuentas fijas |
| 6 | **Cierre del módulo** | todos | README, `AGENTS.md`, tabla de verificación, estados |

**Paralelismo:** T1 y T2 son independientes, pero los dos tocan los `.csproj` (T1 agrega Bogus y T2 la referencia a Infrastructure en las pruebas). Se hacen en serie, en ese orden.

## Decisiones de implementación

- **Dónde vive cada cosa** (`Infrastructure/Seed/DatosPrueba/`):
  - `DatosPruebaOptions`: las claves de la spec y un método puro `Validar()` que devuelve la lista de errores (rangos y contraseña).
  - `DatosPruebaSeedService` (scoped): las guardas, la transacción y un método privado por capa.
  - `PlanificadorHorarios`: lógica pura sobre franjas, sin EF ni Bogus. Recibe un `Random` para ser determinista.
  - `Franja`: un `record` con día, hora de inicio y hora de fin, y la lista fija de franjas de la spec.
  - `FechasPrueba`: lógica pura que, a partir de hoy, calcula las fechas y el nombre de los tres periodos.
  - `CatalogosPrueba`: listas estáticas de cursos (nombre y área), áreas con su prefijo de código y especialidades.
  - `GeneradorPersonas`: envuelve el `Faker` y garantiza cédulas y correos únicos con `HashSet`.
- **La validación de las opciones no usa `ValidateOnStart`:**
  - `IValidateOptions` no sabe en qué entorno corre, y con `ValidateOnStart` un flag mal puesto en producción impediría arrancar.
  - El servicio llama a `Validar()` **solo después** de confirmar el flag y el entorno, y si hay errores lanza `InvalidOperationException` con todos los mensajes, que nombran la clave (por ejemplo, `'DatosPrueba:Estudiantes' debe estar entre 0 y 5000`).
  - `Contrasena` se valida con `PoliticaContrasena.Cumple` (RN-26).
  - `Validar()` es pura, así que se prueba con pruebas unitarias.
- **El entorno lo pasa `Program.cs`:** `await app.Services.SembrarDatosPruebaAsync(app.Environment.IsDevelopment(), app.Lifetime.ApplicationStopping)`, después de `SembrarAdministradorInicialAsync`. Infrastructure no depende de `IHostEnvironment`.
- **Guardas, en este orden:**
  1. `Habilitado = false` → termina sin escribir en el log (es el caso normal).
  2. Fuera de `Development` → `LogWarning`.
  3. Opciones inválidas → excepción.
  4. BD con datos de negocio → `LogInformation`. Se comprueba con un `AnyAsync` por tabla: cursos, aulas, periodos, profesores, estudiantes, grupos y matrículas.
- **Semilla y Bogus:**
  - `semilla = Semilla ?? Random.Shared.Next()`.
  - **Sin el `Randomizer.Seed` global** que muestra el ejemplo de la spec: un `new Faker("es") { Random = new Randomizer(semilla) }` y un `new Random(semilla)` para el planificador. Así no se toca estado estático y el resultado es el mismo. Es un ajuste menor al ejemplo de código.
- **Contraseña:**
  - Se hashea **una sola vez** con `IHasherContrasenas` y se reutiliza el mismo hash en todas las cuentas. Es válido: el hash ya incluye su sal, y la verificación funciona igual.
  - Hashear miles de veces costaría segundos o minutos (PBKDF2) y pondría en riesgo el criterio de éxito 1.
  - Las cuentas fijas `profesor2` y `estudiante2` usan `AsignarTemporal(hash, MotivoTemporal.CuentaNueva, ahora)` en lugar de `EstablecerContrasena`.
- **Guardados dentro de la transacción** (`BeginTransactionAsync`; `AppDbContext.SaveChangesAsync` reutiliza la transacción en curso para la auditoría):
  1. Catálogos y personas: cursos, aulas, periodos, usuarios, profesores y estudiantes.
  2. Grupos con sus horarios y su estado inicial (necesitan los Ids del paso 1).
  3. Matrículas, retiros y estados finales (necesitan los Ids de los grupos).
- **Fechas:**
  - Todas salen de `IFechaLocal.Hoy()` con los desplazamientos de la tabla de periodos de la spec.
  - El nombre del periodo es `$"{inicio.Year}-T{(inicio.Month - 1) / 3 + 1}"`. Con los desplazamientos de la spec, los tres inicios caen en trimestres distintos.
  - Fecha de registro de los estudiantes: al azar entre hoy−365 y el día anterior al inicio de la matrícula del periodo pasado.
- **Datos generados:**
  - Cédula: 9 dígitos con el primero entre 1 y 7, como las de Costa Rica.
  - Teléfono: 8 dígitos que empiezan con 2, 6, 7 u 8.
  - Correo: nombre y apellido sin tildes ni ñ (`string.Normalize(FormD)` sin las marcas diacríticas), en minúsculas, con un número de secuencia: `nombre.apellido.N@prueba.test`. Se normaliza con `Usuario.NormalizarCorreo`.
  - Las longitudes se recortan a los máximos de las configuraciones EF, para no depender de lo que genere Bogus.
- **`PlanificadorHorarios`:**
  - Un planificador por periodo. Guarda, por profesor, aula y estudiante, el conjunto de franjas ocupadas, y por estudiante, los cursos que ya tiene.
  - `IntentarAsignarGrupo(cantidadBloques, profesores, aulas | null)`:
    - Elige al azar `cantidadBloques` franjas en días distintos y, en orden aleatorio, el primer profesor y la primera aula (si no es virtual) libres en todas ellas.
    - Recorre en orden aleatorio todas las combinaciones de franjas (32 con un bloque, 420 con dos) y devuelve "sin espacio" solo si ninguna tiene profesor y aula libres. (Ajustado en T2: antes eran 20 intentos al azar.)
    - Al asignar, marca las franjas como ocupadas.
  - `PuedeMatricular(estudiante, curso, franjas)` y `RegistrarMatricula(...)`: RN-05 y RN-06.
  - Trabaja con Ids (`int`) y franjas, no con entidades. Así se prueba sin BD.
- **Grupos:**
  - El cupo de un grupo con aula es `Math.Min(capacidad, aleatorio 10–30)` (RN-09).
  - El rol de cada grupo del periodo vigente (abierto, lleno, cancelado, planificado, cerrado) se decide con la lista de prioridades de la spec.
  - Estados iniciales en T4: los grupos que recibirán matrículas se crean y se abren (`Planificado → Abierto`). Los que deben quedar `Planificado` se quedan así, y el del periodo futuro sin horarios no recibe bloques.
  - Los estados finales (`Cerrado`, `Cancelado`, `Finalizado`) se aplican en T5, después de matricular, como en la operación normal.
- **Matrículas:**
  - Por estudiante activo y periodo: un número al azar entre 0 y `MaxMatriculasPorEstudiante`. Para cada una, se recorren en orden aleatorio los grupos abiertos que admiten matrículas y se toma el primero que cumpla las reglas.
  - El grupo "lleno" del periodo vigente se llena después, con estudiantes activos que todavía puedan tomarlo. Si no hay suficientes, queda casi lleno y se anota en el resumen.
  - **Cancelación** (RN-12): cada matrícula activa se retira con `Retirar(fecha, CancelacionGrupo)` y `LiberarCupo()`, y después `CambiarEstado(Cancelado)`.
  - **Finalización:** `Abierto → Cerrado → EnCurso → Finalizado`, un paso a la vez.
  - **Cambio de grupo:** se busca un estudiante con una matrícula activa en un grupo `Abierto` del periodo vigente, con otro grupo `Abierto` del mismo curso que tenga cupo y no le genere traslapes. Se retira la primera con `CambioGrupo` y se crea la nueva.
- **Desactivados:** se eligen antes de matricular, para que no reciban matrículas en el periodo vigente. Los estudiantes desactivados sí pueden tener matrículas en el periodo pasado. El profesor desactivado se excluye de la asignación de grupos de los periodos vigente y futuro.
- **Resumen:** un `[LoggerMessage]` con la semilla y los totales por tipo, más un `LogWarning` por cada grupo o matrícula que no se pudo crear por falta de espacio.

## Riesgos y mitigación

| Riesgo | Mitigación |
|---|---|
| El planificador deja traslapes y la BD queda con datos que el sistema nunca permitiría | Pruebas unitarias en T2. En T4 y T5, consultas `psql` de traslapes por profesor, aula y estudiante que deben devolver 0 filas |
| `CuposOcupados` no coincide con las matrículas activas | Cada matrícula pasa por `OcuparCupo()` y cada retiro por `LiberarCupo()`. En T5, una consulta compara el contador con el conteo real |
| Con cantidades grandes no hay franjas suficientes y se crean menos grupos | Comportamiento esperado de la spec: `LogWarning` y totales reales en el resumen. Se prueba en T4 con `Profesores = 1` y `GruposPorPeriodo = 50` |
| La siembra tarda demasiado | Un solo hash de contraseña y tres guardados. En T5 se mide el tiempo con los valores por defecto (meta: menos de 10 s) y con `Estudiantes = 2000` como referencia |
| Bogus genera textos que rompen las validaciones (largo, caracteres) | Recorte a las longitudes de EF, y cédulas, teléfonos y correos con formato propio, no de Bogus |
| Una siembra a medias deja la BD inconsistente | Una sola transacción. En T4 se renombra `horario` con `psql` para que falle el segundo guardado, y se verifica que tampoco quedan los cursos ni las personas del primero |
| La contraseña aparece en el log | El resumen no la incluye. En T6 se revisa el log completo de una siembra |
| Las fechas de algún día del año dan periodos incoherentes o dos con el mismo nombre | `FechasPrueba` es pura: en T2, una prueba recorre todos los días de un año bisiesto y verifica RN-16, que las clases no se traslapen y que los tres nombres sean distintos |
| La auditoría crece mucho con cantidades grandes | Es lo esperado (RN-42). Se anota el número de filas en el resumen de T5 |

## Puntos de verificación

- **Tras 1:** con el flag apagado no pasa nada; fuera de `Development` hay una advertencia; con opciones inválidas la API no arranca y nombra la clave; con datos existentes, el log lo indica.
- **Tras 2:** el planificador nunca da dos grupos a la misma franja de un profesor o un aula, y respeta RN-05 y RN-06 para los estudiantes. Las fechas de los periodos son coherentes cualquiera sea el día.
- **Tras 3:** la BD recreada tiene las cantidades configuradas de cursos, aulas, periodos, profesores y estudiantes, con cuentas que inician sesión. Una misma semilla da los mismos datos.
- **Tras 4:** grupos con horarios, sin traslapes de profesor ni de aula, sin cupos mayores que la capacidad, y con los estados iniciales esperados.
- **Tras 5:** matrículas coherentes con los cupos y sin traslapes, todos los estados y motivos de retiro cubiertos, y operaciones normales sobre el periodo vigente.
- **Final:** toda la tabla de verificación manual de la spec, los criterios de éxito cumplidos, `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Fuera de este plan

Lo que la spec deja fuera de alcance: sembrar `registro_error`, administradores adicionales, grupos `EnCurso`, un endpoint o comando para borrar datos, otros entornos y pruebas de integración.
