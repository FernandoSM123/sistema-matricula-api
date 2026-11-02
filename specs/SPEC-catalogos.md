# Spec: catalogos

Módulo `catalogos` del [mapa de módulos](mapa-modulos.md). Depende de `acceso`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/catalogos/plan.md) | [Aprobadas](../tasks/catalogos/todo.md) | 0/10 tareas completadas |

**Historial:**
- 2026-11-02: v1 en revisión, con tres preguntas abiertas.
- 2026-11-02: resueltas las preguntas del periodo vigente y de RN-16 (decisiones 4 y 5). Queda abierta la de la zona horaria.
- 2026-11-02: resuelta la zona horaria (decisión 6) y v1 aprobada, con sus seis supuestos. Plan y lista de 10 tareas en revisión.
- 2026-11-02: plan y lista de tareas aprobados; la implementación empieza por T1.

## Objetivo

Que el administrador mantenga los tres catálogos sobre los que se abren los grupos: cursos, periodos académicos y aulas. Profesores y estudiantes consultan lo que les corresponde según la matriz §2.2. Cubre RF-29 a RF-33, RF-35, RF-36, RF-38, RF-39, RF-40 (sin la eliminación, ver decisión 1) y las reglas RN-13 (código de curso, código de aula y nombre de periodo), RN-16, RN-39 y RN-41.

**Usuarios:**
- Administrador: registra, lista, consulta y edita los tres catálogos; desactiva y reactiva cursos y aulas.
- Profesor: consulta cursos, periodos y aulas.
- Estudiante: consulta cursos y periodos, no aulas.
- Visitante: no accede. La oferta académica pública (RF-50) es de `grupos`.
- El equipo de desarrollo: `grupos` referencia estas entidades, y `matriculas` usa el periodo vigente y las reglas de fechas del periodo.

### Alcance

1. **Entidades** (§5.5 a §5.7 de los requerimientos). Todas heredan de `EntidadBase`. A diferencia de las personas, `Curso` y `Aula` guardan su propio `Activo`: la decisión 2 del mapa solo aplica a quienes tienen cuenta.

   **`Curso`**

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Codigo` | `string(20)` | Índice único (RN-13). Se guarda en mayúsculas (supuesto 1) |
   | `Nombre` | `string(150)` | |
   | `Descripcion` | `string(1000)?` | Vacía o en blanco se guarda como `null` |
   | `DuracionHoras` | `int` | Mayor que 0 |
   | `Costo` | `decimal(10,2)` | Mayor o igual a 0 (supuesto 2) |
   | `Nivel` | `NivelCurso` | Enum nuevo: `Basico`, `Intermedio`, `Avanzado`. Se guarda como string |
   | `Activo` | `bool` | `true` al crear |

   Métodos: `ActualizarDatos(...)`, `Desactivar()`, `Reactivar()`. Migración `AgregaCurso`.

   **`Aula`**

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Codigo` | `string(20)` | Índice único (RN-13). Se guarda en mayúsculas (supuesto 1) |
   | `Ubicacion` | `string(150)?` | Vacía o en blanco se guarda como `null` |
   | `Capacidad` | `int` | Mayor que 0 |
   | `Activo` | `bool` | `true` al crear |

   Métodos: `ActualizarDatos(...)`, `Desactivar()`, `Reactivar()`. Migración `AgregaAula`.

   **`Periodo`**

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Nombre` | `string(50)` | Índice único (RN-13). Se guarda en mayúsculas (supuesto 1) |
   | `InicioClases`, `FinClases` | `DateOnly` | |
   | `InicioMatricula`, `FinMatricula` | `DateOnly` | |
   | `FechaLimiteRetiro` | `DateOnly` | |

   Sin `Activo`: los periodos no se desactivan (RF-37). Métodos: `ActualizarDatos(...)`, y `EnRangoMatricula(fecha)` y `PermiteRetiro(fecha)` de `docs/arquitectura.md` §6, con límites inclusivos; los usa `matriculas`, pero son reglas puras de esta entidad. Migración `AgregaPeriodo`.

2. **Endpoints**

   | Método y ruta | Acceso | RF / RN | Respuesta |
   |---|---|---|---|
   | `POST /api/v1/cursos` | Administrador | RF-29, RN-13, RN-39 | `201` + `Location` · `409 RN-13` · `409 RN-39` |
   | `GET /api/v1/cursos` | Con sesión | RF-30, RN-41 | `200` paginado; `search` (nombre o código), `nivel`, `incluirInactivos` |
   | `GET /api/v1/cursos/{id}` | Con sesión | RF-31 | `200` · `404` |
   | `PUT /api/v1/cursos/{id}` | Administrador | RF-32, RN-13 | `200` · `404` · `409 RN-13` |
   | `POST /api/v1/cursos/{id}/desactivar` | Administrador | RF-33 | `204` · `404` |
   | `POST /api/v1/cursos/{id}/reactivar` | Administrador | RF-33, RN-39 | `204` · `404` |
   | `POST /api/v1/periodos` | Administrador | RF-35, RN-13, RN-16 | `201` + `Location` · `400` · `409 RN-13` |
   | `GET /api/v1/periodos` | Con sesión | RF-36 | `200` paginado; `search` (nombre); los más recientes primero |
   | `GET /api/v1/periodos/{id}` | Con sesión | RF-36 | `200` · `404` |
   | `GET /api/v1/periodos/vigente` | Con sesión | RF-38 | `200` · `404` si ninguno está vigente |
   | `PUT /api/v1/periodos/{id}` | Administrador | RF-36, RN-13, RN-16 | `200` · `400` · `404` · `409 RN-13` |
   | `POST /api/v1/aulas` | Administrador | RF-39, RN-13, RN-39 | `201` + `Location` · `409 RN-13` · `409 RN-39` |
   | `GET /api/v1/aulas` | Administrador o Profesor | RF-40, RN-41 | `200` paginado; `search` (código o ubicación), `incluirInactivos` |
   | `GET /api/v1/aulas/{id}` | Administrador o Profesor | RF-40 | `200` · `404` |
   | `PUT /api/v1/aulas/{id}` | Administrador | RF-40, RN-13 | `200` · `404` · `409 RN-13` |
   | `POST /api/v1/aulas/{id}/desactivar` | Administrador | RF-40 | `204` · `404` |
   | `POST /api/v1/aulas/{id}/reactivar` | Administrador | RF-40, RN-39 | `204` · `404` |

   Detalle de los flujos:
   - **Registro de cursos y aulas:** código de un registro activo → `409 RN-13`; de uno inactivo → `409 RN-39` con un mensaje que ofrece reactivarlo e incluye `cursoId` o `aulaId`.
   - **Edición de cursos y aulas:** código de otro registro, activo o inactivo → `409 RN-13`. Se pueden editar registros inactivos.
   - **Desactivar y reactivar:** idempotentes, como `UsuarioService`: repetir la operación responde `204` sin cambios.
   - **Listados:** cursos ordenados por nombre, aulas por código y periodos por `InicioClases` descendente, siempre con `Id` como desempate.
   - **Periodo vigente:** el que cumple `InicioMatricula ≤ hoy ≤ FinClases`. Si hay varios, el de `InicioMatricula` más reciente, con `Id` como desempate (decisión 4).

3. **Autorización**
   - Nueva política `Politicas.AdministradorOProfesor`, combinada con `conSesionSinCambioPendiente` como las demás.
   - "Con sesión" es el `[Authorize]` por defecto, que ya rechaza a quien tiene un cambio de contraseña pendiente (RN-24).
   - Las políticas se ponen **por endpoint**, no en el controller: un atributo de clase se sumaría al de cada acción y dejaría las lecturas solo para administradores.

4. **Validación**: un validador por DTO de entrada, con los límites de las tablas. RN-16 se valida en el validador (`docs/arquitectura.md` §4) y responde `400` indicando el campo. La condición vive en `Domain/Reglas/ReglasPeriodo.cs`, como `PoliticaContrasena`, para que el validador y la entidad usen la misma regla y se pruebe sin BD; la entidad la vuelve a comprobar como invariante.

5. **Fecha local del instituto** (decisión 6)
   - Nueva opción `Instituto:ZonaHoraria` en `appsettings.json`, con `America/Costa_Rica`. La API no arranca si falta o no es una zona válida.
   - Nueva abstracción `IFechaLocal` con `DateOnly Hoy()`, que convierte la hora de `TimeProvider` a esa zona. Es el único punto que calcula la fecha de hoy.
   - `EstudianteService` (`FechaRegistro`) y `DatosPersonalesRules.FechaNacimiento` dejan de usar la fecha UTC y usan `IFechaLocal`.
   - Las marcas de tiempo (`FechaCreacion`, vencimientos de temporales, tokens) siguen en UTC.

6. **Documentación**: `Matricula.Api.http` con los flujos del módulo, y la sección `Instituto` en el README.

### Fuera de alcance

- **Eliminar cursos, aulas y periodos** (parte de RF-33, RF-37 y parte de RF-40): los añade `grupos` (decisión 1).
- **RN-37** al desactivar cursos y aulas: la añade `grupos` (decisión 4 del mapa).
- **Reducir la capacidad de un aula por debajo del cupo de sus grupos** (RN-09): la guarda la añade `grupos`, porque aquí todavía no hay grupos (decisión 2).
- RF-34 (grupos de un curso) y RF-41 (ocupación de un aula): `grupos`.
- `Politicas.Profesor`: `grupos`.
- Rate limiting: ningún endpoint es anónimo.
- Pruebas de integración: diferidas.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, xUnit 2.9.3. **No se añaden paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaCurso --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/
  Entities/{Curso,Aula,Periodo}.cs           nuevos
  Enums/NivelCurso.cs                        nuevo
  Reglas/ReglasPeriodo.cs                    nuevo (RN-16)
  Reglas/FechaInstituto.cs                   nuevo (UTC → fecha local)
src/Matricula.Application/
  Abstractions/IAppDbContext.cs              + DbSet de Curso, Aula y Periodo
  Abstractions/IFechaLocal.cs                nuevo
  Common/Validacion/DatosPersonalesRules.cs  FechaNacimiento con IFechaLocal
  Estudiantes/                               servicio y validadores con IFechaLocal
  Cursos/                                    CursoService, DTOs, validadores, proyecciones
  Aulas/                                     AulaService, DTOs, validadores, proyecciones
  Periodos/                                  PeriodoService, DTOs, validadores, proyecciones
  DependencyInjection.cs                     + los tres servicios
src/Matricula.Infrastructure/
  Tiempo/{InstitutoOptions,FechaLocal}.cs    nuevos
  DependencyInjection.cs                     + opciones e IFechaLocal
  Persistence/Configurations/{Curso,Aula,Periodo}Configuration.cs
  Persistence/Migrations/                    AgregaCurso, AgregaAula, AgregaPeriodo
  Persistence/AppDbContext.cs                + DbSet de Curso, Aula y Periodo
src/Matricula.Api/
  Controllers/{Cursos,Aulas,Periodos}Controller.cs
  Seguridad/Politicas.cs                     + AdministradorOProfesor
  appsettings.json                           + sección Instituto
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/{CursoTests,AulaTests,PeriodoTests,ReglasPeriodoTests,FechaInstitutoTests}.cs
README.md                                    + sección Instituto
```

## Code Style

Igual que `profesores`: constructores primarios, un archivo por clase, constructor privado para EF, setters privados, recorte de textos en el dominio, proyecciones con `Select` y `AsNoTracking` en las lecturas, pruebas `Metodo_Escenario_Resultado` y `[Authorize(Policy = Politicas.X)]`, nunca `Roles`.

```csharp
public async Task<CursoResponse> RegistrarAsync(RegistrarCursoRequest request, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);

    var curso = new Curso(
        request.Codigo, request.Nombre, request.Descripcion, request.DuracionHoras, request.Costo, request.Nivel);
    await ValidarCodigoDisponibleAsync(curso.Codigo, cancellationToken);

    db.Cursos.Add(curso);
    await db.SaveChangesAsync(cancellationToken);
    return curso.ToResponse();
}
```

```csharp
[Fact]
public void PermiteRetiro_ElMismoDiaDelLimite_DevuelveTrue()
{
    var periodo = CrearPeriodo(fechaLimiteRetiro: new DateOnly(2027, 2, 15));

    Assert.True(periodo.PermiteRetiro(new DateOnly(2027, 2, 15)));
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD):
  - `Curso` y `Aula`: normalizan el código, recortan textos, un opcional en blanco queda `null`, `ActualizarDatos` cambia todos los datos, se crean activos, `Desactivar` y `Reactivar` cambian el estado.
  - `ReglasPeriodo`: cada condición de RN-16, incluidos los límites.
  - `Periodo`: normaliza el nombre, rechaza fechas incoherentes, y `EnRangoMatricula` y `PermiteRetiro` en los límites.
  - `FechaInstituto`: a las 05:59 UTC en Costa Rica sigue siendo el día anterior y a las 06:00 UTC ya es el mismo día.
- **Manual**, con la API y PostgreSQL en marcha, usando `Matricula.Api.http` y Scalar:

| Qué | Esperado |
|---|---|
| Registrar un curso, un aula y un periodo válidos | `201` + `Location` válido |
| Registrar un código o nombre repetido, con otra capitalización | `409 RN-13` |
| Registrar el código de un curso o aula inactivos | `409 RN-39` con `cursoId` o `aulaId` |
| Editar un código o nombre al de otro registro | `409 RN-13` |
| Costo negativo, duración o capacidad 0, código vacío o de 21 caracteres, nivel inexistente | `400` con el campo y el mensaje |
| Periodo que incumple cada condición de RN-16 | `400` con el campo y el mensaje |
| Crear y editar con el mismo nombre o código propio | `PUT` responde `200` |
| Listar cursos con `search`, `nivel` y paginación | Filtra y pagina |
| Listar con y sin `incluirInactivos` | Los inactivos solo aparecen con el filtro |
| Desactivar y reactivar; repetir la operación | `204` siempre; el detalle refleja el estado |
| `GET /periodos/vigente` con y sin periodo vigente | `200` / `404` |
| Dos periodos traslapados y vigentes a la vez | `201` al crearlos; `GET /periodos/vigente` devuelve el de `InicioMatricula` más reciente |
| Periodo con `FechaLimiteRetiro` antes de `InicioClases` y no antes de `InicioMatricula` | `201` |
| Consultar un id inexistente | `404` |
| Arrancar con `Instituto__ZonaHoraria` vacía o inválida | La API no arranca y el error nombra la opción |
| Arrancar con otra zona válida que tenga otro día, por ejemplo `Pacific/Kiritimati` | `GET /periodos/vigente` y `fechaRegistro` usan la fecha de esa zona |
| Registrar un estudiante | `fechaRegistro` es la fecha de Costa Rica, también después de las 18:00 locales |
| Lectura de cursos y periodos con token de profesor y de estudiante | `200` |
| Lectura de aulas con token de profesor / de estudiante | `200` / `403` |
| Escrituras con token de profesor o de estudiante | `403` |
| Cualquier endpoint sin token | `401` |
| Cualquier endpoint con un cambio de contraseña pendiente | `403` (RN-24) |

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. Mensajes en español y comprensibles. Validar los duplicados en el servicio antes de guardar. `IFechaLocal` para la fecha de hoy y `TimeProvider` para las marcas de tiempo.
- **Preguntar antes:** añadir paquetes NuGet. Cambiar el esquema fuera de esta spec. Añadir reglas de negocio que no estén en los requerimientos.
- **Nunca:** eliminar cursos, aulas o periodos en este módulo. `Activo` en `Periodo`. Filtros globales de `Activo`. Eliminación en cascada. `[Authorize(Roles = ...)]`.

## Success Criteria

1. Las migraciones crean `curso`, `aula` y `periodo` con índices únicos en `codigo`, `codigo` y `nombre`, y `costo` como `numeric(10,2)`.
2. El administrador registra, lista, consulta y edita los tres catálogos, y desactiva y reactiva cursos y aulas (RF-29 a RF-33, RF-35, RF-36, RF-39, RF-40, sin eliminación).
3. Registrar el código de un curso o aula inactivos responde `409 RN-39` y ofrece reactivarlo.
4. Ningún periodo incoherente se guarda (RN-16).
5. `GET /api/v1/periodos/vigente` devuelve siempre un solo periodo, o `404` si no hay ninguno (RF-38).
6. El acceso de cada rol coincide con la matriz §2.2.
7. Toda comparación con "hoy" usa la fecha de `Instituto:ZonaHoraria`, y la opción está documentada en el README.
8. Las verificaciones manuales de la tabla dan el resultado esperado.
9. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **Códigos y nombre de periodo en mayúsculas.** `Codigo` y `Periodo.Nombre` se recortan y se guardan en mayúsculas. Así RN-13 no distingue capitalización, sin extensiones ni índices por expresión: `2026-i` y `2026-I` son el mismo periodo. Los requerimientos no piden un formato, así que solo se limita la longitud.
2. **Costo 0 permitido**, para cursos gratuitos. No se acepta negativo. La moneda no se modela.
3. **`incluirInactivos` disponible para todo rol con acceso de lectura.** RN-41 no lo limita, y solo es consulta. Reactivar sigue siendo solo del administrador (RN-39). El detalle de un curso o aula inactivos también es visible, porque el historial los sigue mostrando (RN-40).
4. **Edición sin restricciones por fechas o estado.** RF-32, RF-36 y RF-40 no limitan la edición: se puede editar un periodo pasado o cambiar el código de un curso. Si `grupos` o `matriculas` necesitan una guarda, se añade allí.
5. **Búsqueda de aulas por código o ubicación.** RF-40 no define filtros y §6 pide que todos los listados los permitan. Los periodos buscan por nombre.
6. **Límites de texto:** los de las tablas de entidades. Los requerimientos no los definen.

## Decisiones

Se aplican las decisiones 1, 2, 4 y 5 del mapa, y además:

1. **La eliminación queda para `grupos`** (2026-11-02). La decisión 4 del mapa ya asigna a `grupos` la guarda "solo si nunca tuvo grupos" y RF-37, y `profesores` siguió ese criterio. Un `DELETE` sin guarda dejaría abierta una eliminación que nadie valida. Mientras tanto, un curso o aula creados por error se desactivan y un periodo se corrige con `PUT`.

2. **La guarda de capacidad del aula es de `grupos`** (2026-11-02). Editar `Capacidad` por debajo del cupo de un grupo incumpliría RN-09 y hoy no se puede comprobar. Es un caso nuevo, ya agregado a la decisión 4 del mapa para que no se pierda.

3. **La verificación heredada de `fundacion` T3 sigue pendiente** (2026-11-02). `CursosController`, `PeriodosController` y `AulasController` no tienen nombre compuesto; queda para el primer módulo que tenga uno.

4. **Periodo vigente y traslape** (2026-11-02). Vigente es el periodo con `InicioMatricula ≤ hoy ≤ FinClases`, porque la oferta debe verse desde que abre la matrícula. Los periodos pueden traslaparse, ya que la matrícula del siguiente puede abrir antes de que termine el actual. Si hay dos vigentes, gana el de `InicioMatricula` más reciente y `Id` desempata. Sin periodo vigente, `GET /periodos/vigente` responde `404`.

5. **Interpretación de RN-16** (2026-11-02). Un periodo es coherente si cumple las cuatro condiciones:
   - `InicioMatricula < FinMatricula`
   - `InicioClases < FinClases`
   - `FinMatricula ≤ FinClases`
   - `InicioMatricula ≤ FechaLimiteRetiro ≤ FinClases`

   "Dentro del periodo" incluye la etapa de matrícula, para que un estudiante pueda retirarse antes de que empiecen las clases. No se exige que la matrícula termine antes del inicio de clases, para permitir la matrícula tardía.

6. **"Hoy" es la fecha de Costa Rica** (2026-11-02). Con la fecha UTC, desde las 18:00 locales el sistema ya estaría en el día siguiente, lo que afectaría al periodo vigente y a RN-04 y RN-10 en `matriculas`. La zona se configura en `Instituto:ZonaHoraria` (`America/Costa_Rica`) y solo `IFechaLocal` calcula la fecha de hoy. Se corrigen también los dos usos existentes de `estudiantes`, para que todo el sistema use el mismo "hoy". No requiere paquetes nuevos. Decisión registrada en el mapa (decisión 9), `docs/arquitectura.md` §4, `AGENTS.md` y el README.

## Preguntas abiertas

Ninguna.
