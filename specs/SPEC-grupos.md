# Spec: grupos

Módulo `grupos` del [mapa de módulos](mapa-modulos.md). Depende de `catalogos` y `profesores`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/grupos/plan.md) | [Aprobadas](../tasks/grupos/todo.md) | 5/13 tareas completadas |

**Historial:**
- 2026-11-05: v1 en revisión, con nueve supuestos y una pregunta abierta (eliminación de profesores y su cuenta).
- 2026-11-05: resuelta la pregunta abierta: al eliminar un profesor se elimina también su cuenta (decisión 4). Se actualizaron RF-14, RF-19, RF-26, RN-35 y la tabla de §4.2 de los requerimientos, y se agregó la decisión 10 al mapa. v1 aprobada, con sus nueve supuestos. Plan y lista de 13 tareas en revisión.
- 2026-11-05: plan y lista de tareas aprobados; la implementación empezará por T1.

## Objetivo

Que el administrador abra la oferta del instituto: crea grupos de un curso en un periodo, les asigna profesor, aula, cupo y horarios, y los lleva por sus estados. El sistema impide los choques de horario de profesores y aulas, y que un grupo supere la capacidad de su aula. Profesores, estudiantes y visitantes consultan lo que les corresponde según la matriz §2.2.

Cubre RF-27, RF-28, RF-34, RF-41 a RF-45, RF-47, RF-48 y RF-50; las reglas RN-07, RN-08, RN-09, RN-13 (NRC), RN-14, RN-15, RN-17 y RN-37; las consultas 4, 5, 7, 8 y 9 de la sección 6 (decisión 3 del mapa), y las guardas que la decisión 4 del mapa asigna a este módulo.

**Usuarios:**
- Administrador: gestiona grupos y horarios, cambia el estado de los grupos y consulta todo.
- Profesor: consulta sus grupos y su horario semanal (RN-18).
- Estudiante: consulta los grupos abiertos.
- Visitante: consulta la oferta académica (RF-50), único endpoint anónimo del módulo.
- El equipo de desarrollo: `matriculas` matricula en estos grupos, mantiene `CuposOcupados` y añade sus guardas (decisión 4 del mapa).

### Alcance

1. **Entidades** (§5.8 y §5.9 de los requerimientos). Heredan de `EntidadBase`.

   **`Grupo`**

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Nrc` | `string(10)` | Índice único (RN-13). Lo escribe el administrador; se recorta y se guarda en mayúsculas (supuesto 1) |
   | `CursoId` | `int` | FK `Restrict` |
   | `ProfesorId` | `int` | FK `Restrict` |
   | `PeriodoId` | `int` | FK `Restrict` |
   | `AulaId` | `int?` | FK `Restrict`. Obligatoria en `Presencial` e `Hibrida`, `null` en `Virtual` (supuesto 2) |
   | `Cupo` | `int` | Mayor que 0. No supera la capacidad del aula (RN-09) |
   | `Modalidad` | `Modalidad` | Enum nuevo: `Presencial`, `Virtual`, `Hibrida`. Se guarda como string |
   | `Estado` | `EstadoGrupo` | Enum nuevo: `Planificado`, `Abierto`, `Cerrado`, `EnCurso`, `Finalizado`, `Cancelado`. Se guarda como string. `Planificado` al crear |
   | `CuposOcupados` | `int` | `0` al crear. Aquí solo se lee; lo mantiene `matriculas` (decisión 2) |
   | `Horarios` | `List<Horario>` | Navegación de los bloques |

   Índices adicionales: `(periodo_id, profesor_id)` y `(periodo_id, aula_id)`, para las búsquedas de traslapes.

   Métodos de dominio:
   - `ActualizarDatos(...)`: aplica las reglas de edición por estado (supuesto 4).
   - `CambiarEstado(nuevo)`: transiciones de RF-47 (supuesto 5). Pasar a `Abierto` exige al menos un bloque de horario (supuesto 6).
   - `AgregarHorario(dia, inicio, fin)`: RN-15 y RN-14 entre los bloques del mismo grupo; no en grupos `Finalizado` o `Cancelado`.
   - `QuitarHorario(horarioId)`: solo en `Planificado` (RF-48).
   - `CuposDisponibles` (calculado): `Cupo - CuposOcupados`.

   **`Horario`**

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `GrupoId` | `int` | FK `Restrict` |
   | `Dia` | `DiaSemana` | Enum nuevo: `Lunes` … `Domingo`. Se guarda como string |
   | `HoraInicio`, `HoraFin` | `TimeOnly` | `HoraFin > HoraInicio` (RN-15) |

   Método `SeTraslapaCon(otro)` (RN-14): mismo día y `a.Inicio < b.Fin && b.Inicio < a.Fin`. Bloques contiguos (08:00–10:00 y 10:00–12:00) no se traslapan.

   Migración `AgregaGrupo` (tablas `grupo` y `horario`).

2. **Endpoints de grupos y horarios**

   | Método y ruta | Acceso | RF / RN | Respuesta |
   |---|---|---|---|
   | `POST /api/v1/grupos` | Administrador | RF-42, RN-09, RN-13, RN-17 | `201` + `Location` · `400` · `404` (curso, profesor, periodo o aula) · `409` |
   | `GET /api/v1/grupos` | Con sesión | RF-43, RN-18 | `200` paginado; filtros `cursoId`, `profesorId`, `periodoId`, `estado`, `modalidad`, `search` (NRC) |
   | `GET /api/v1/grupos/{id}` | Con sesión | RF-44, RN-18 | `200` con horarios, cupo, ocupados y disponibles · `403 RN-18` · `404` |
   | `PUT /api/v1/grupos/{id}` | Administrador | RF-45, RN-07, RN-08, RN-09, RN-13, RN-17 | `200` · `400` · `404` · `409` |
   | `POST /api/v1/grupos/{id}/estado` | Administrador | RF-47 | `204` · `400` · `404` · `409 RF-47` |
   | `POST /api/v1/grupos/{id}/horarios` | Administrador | RF-48, RN-07, RN-08, RN-14, RN-15 | `201` + `Location` (detalle del grupo) · `400` · `404` · `409` |
   | `DELETE /api/v1/grupos/{id}/horarios/{horarioId}` | Administrador | RF-48 | `204` · `404` · `409 RF-48` |

   Detalle de los flujos:
   - **Crear (RF-42):** el grupo nace `Planificado` y sin horarios. Curso, profesor y aula deben estar activos (`409 RN-17`). El cupo no supera la capacidad del aula (`409 RN-09`). NRC repetido → `409 RN-13`. Un id de curso, profesor, periodo o aula que no existe → `404`.
   - **Modalidad y aula:** `Virtual` con `aulaId` o `Presencial`/`Hibrida` sin `aulaId` → `400` desde el validador.
   - **Editar (RF-45):** reglas por estado del supuesto 4. Si cambia el profesor o el aula, se revisan sus traslapes con los horarios del grupo (`409 RN-07` / `409 RN-08`); el nuevo profesor, curso o aula deben estar activos (`409 RN-17`); el cupo no baja de `CuposOcupados` (`409 RF-45`) ni supera la capacidad del aula (`409 RN-09`).
   - **Agregar horario (RF-48):** `400` si `horaFin ≤ horaInicio` (RN-15). `409 RN-14` si se traslapa con otro bloque del mismo grupo. `409 RN-07` si choca con un bloque de otro grupo del mismo profesor en el mismo periodo; `409 RN-08` lo mismo con el aula. Los grupos `Cancelado` no cuentan para los traslapes (supuesto 3). El mensaje nombra el NRC del grupo con el que choca.
   - **Quitar horario (RF-48):** solo si el grupo está `Planificado`; si no, `409 RF-48`. Un `horarioId` de otro grupo → `404`.
   - **Cambiar estado (RF-47):** body `{ "estado": "Abierto" }`. Transición no permitida → `409 RF-47`; abrir sin horarios → `409 RF-47` con un mensaje propio. Repetir el estado actual responde `204` sin cambios. `matriculas` añade RN-12 a la cancelación.
   - **Visibilidad por rol (RN-18):** el administrador ve todos los grupos. El profesor solo los suyos: en el listado se fuerza `profesorId` al suyo y el detalle de otro grupo responde `403 RN-18`. El estudiante solo ve los `Abierto`: en el listado se fuerza `estado=Abierto` y el detalle de otro estado responde `403 RN-18` (supuesto 7).
   - **Listado:** ordenado por `InicioClases` del periodo descendente, nombre del curso, NRC e `Id`. Sin filtro de inactivos: los grupos no se desactivan, tienen estado propio.
   - **Códigos de error sin RN:** cuando no hay una RN que aplique, `code` es el RF (`RF-45`, `RF-47`, `RF-48`), como `CONCURRENCIA` en `fundacion`.

3. **Endpoints de consultas** (sección 6)

   | Método y ruta | Acceso | RF / consulta | Respuesta |
   |---|---|---|---|
   | `GET /api/v1/oferta-academica` | **Anónimo** | RF-50, consulta 9 | `200` paginado; filtros `cursoId`, `modalidad`, `search` (curso o NRC) |
   | `GET /api/v1/cursos/{id}/grupos` | Con sesión | RF-34, consulta 4 | `200` paginado; filtros `estado` y `periodoId` · `404` |
   | `GET /api/v1/profesores/{id}/grupos` | Administrador | RF-27, consulta 5 | `200` paginado; filtro `periodoId` · `404` |
   | `GET /api/v1/me/grupos` | Profesor | RF-27, consulta 5 | Igual que el anterior, con el profesor de la sesión |
   | `GET /api/v1/profesores/{id}/horario` | Administrador | RF-28, consulta 7 | `200` con los bloques de la semana; `periodoId` opcional · `404` |
   | `GET /api/v1/me/horario` | Profesor | RF-28, consulta 7 | Igual que el anterior, con el profesor de la sesión |
   | `GET /api/v1/aulas/{id}/ocupacion` | Administrador | RF-41, consulta 8 | `200` con los bloques ocupados; `periodoId` opcional · `404` |

   Detalle:
   - **Oferta académica:** grupos `Abierto` del periodo vigente (`PeriodoService.ObtenerVigenteAsync`), con curso, profesor, modalidad, aula, horarios y cupos disponibles. Incluye los que tienen 0 disponibles. Sin periodo vigente responde `200` con la lista vacía, no `404` (supuesto 8). `OfertaAcademicaController` es el primer controller de nombre compuesto: cumple la verificación heredada de `fundacion` T3.
   - **Grupos de un curso:** el administrador filtra por cualquier estado; profesor y estudiante solo reciben los `Abierto`, como en `GET /grupos` (consulta 4: "Grupos abiertos por curso").
   - **Horario semanal y ocupación:** una lista sin paginar de bloques (`dia`, `horaInicio`, `horaFin`, `grupoId`, `nrc`, curso y aula o profesor), ordenada por día y hora de inicio. Sin `periodoId` se usa el periodo vigente; si no hay vigente responde `404` pidiendo el `periodoId`. Excluye los grupos `Cancelado`.
   - **Grupos de un profesor:** sin `periodoId` lista todos los periodos.
   - `/me/grupos` y `/me/horario` usan `ICurrentUser.ProfesorId`. `matriculas` ampliará `/me/horario` a los estudiantes (RF-21).

4. **Guardas sobre otros módulos** (decisión 4 del mapa)

   | Operación | Guarda | Respuesta |
   |---|---|---|
   | Desactivar profesor (`/profesores/{id}/desactivar` y `/usuarios/{id}/desactivar` de una cuenta Profesor) | RN-37: sin grupos `Planificado`, `Abierto`, `Cerrado` o `EnCurso` | `409 RN-37` con la cantidad de grupos |
   | Desactivar curso o aula | RN-37 | `409 RN-37` |
   | Editar la capacidad de un aula | RN-09: no por debajo del cupo de sus grupos no `Finalizado` ni `Cancelado` | `409 RN-09` |
   | `DELETE /api/v1/cursos/{id}` y `DELETE /api/v1/aulas/{id}` | Solo si nunca tuvieron grupos (RF-33, RF-40, RN-34) | `204` · `404` · `409 RN-34` |
   | `DELETE /api/v1/periodos/{id}` | Solo si no tiene grupos (RF-37, RN-34) | `204` · `404` · `409 RN-34` |
   | `DELETE /api/v1/profesores/{id}` | Solo si nunca tuvo grupos (RF-26, RN-34). Se elimina también su cuenta, en el mismo guardado (RN-35, decisión 4) | `204` · `404` · `409 RN-34` |

   RN-37 vive en `UsuarioService.DesactivarAsync` para los profesores, así aplica por las dos rutas. El `409 RN-34` indica que se debe desactivar en su lugar.

5. **Autorización**
   - Nueva política `Politicas.Profesor`, combinada con `conSesionSinCambioPendiente` como las demás.
   - `OfertaAcademicaController` con `[AllowAnonymous]`.
   - Políticas por endpoint en `GruposController` (lecturas con `[Authorize]`, escrituras con `Politicas.Administrador`), igual que en `catalogos`. La visibilidad por rol (RN-18) se resuelve en el servicio con `ICurrentUser`, no con atributos.
   - Las nuevas acciones de `ProfesoresController` heredan su `Politicas.Administrador` de clase; las de `CursosController` y `AulasController` llevan su política por acción.

6. **Validación**: un validador por DTO de entrada. RN-15 y la regla modalidad/aula responden `400` desde el validador; la condición de RN-15 vive en `Domain/Reglas/ReglasHorario.cs` para que validador y entidad usen la misma regla, igual que `ReglasPeriodo`. Las reglas que necesitan la BD (RN-07, RN-08, RN-09, RN-13, RN-17, RN-37) las valida el servicio y responden `409`.

7. **Documentación**: `Matricula.Api.http` con los flujos del módulo.

### Fuera de alcance

- **Eliminar grupos** (RF-46) y **RN-12** al cancelar: los añade `matriculas` (decisión 4 del mapa). Mientras tanto, un grupo creado por error se cancela.
- **RF-49** (estudiantes de un grupo), RF-20, RF-21 y las consultas 2, 3, 6 y 10: `matriculas`.
- Mantener `CuposOcupados` y el token de concurrencia `xmin` en `Grupo`: `matriculas` (decisión 2).
- RN-06 (traslapes de un estudiante) al agregar horarios o cambiar de estado un grupo con matrículas: lo añade `matriculas` si aplica.
- Rate limiting de la oferta académica: es una lectura y `docs/arquitectura.md` §4 solo lo pide en `login` y `olvido-contrasena`.
- Pruebas de integración: diferidas.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, xUnit 2.9.3. **No se añaden paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaGrupo --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/
  Entities/{Grupo,Horario}.cs                nuevos
  Enums/{EstadoGrupo,Modalidad,DiaSemana}.cs nuevos
  Reglas/ReglasHorario.cs                    nuevo (RN-14, RN-15)
src/Matricula.Application/
  Abstractions/IAppDbContext.cs              + DbSet de Grupo y Horario
  Grupos/                                    GrupoService, HorarioService (si hace falta separarlo), DTOs, validadores, proyecciones
  Grupos/Consultas/                          OfertaAcademicaService, HorarioSemanalService (consultas de la sección 6)
  Cursos/CursoService.cs                     + RN-37, eliminar
  Aulas/AulaService.cs                       + RN-37, RN-09 en la edición, eliminar
  Periodos/PeriodoService.cs                 + eliminar
  Profesores/ProfesorService.cs              + eliminar
  Cuentas/UsuarioService.cs                  + RN-37 para cuentas Profesor
  DependencyInjection.cs                     + los servicios nuevos
src/Matricula.Infrastructure/
  Persistence/Configurations/{Grupo,Horario}Configuration.cs
  Persistence/Migrations/                    AgregaGrupo
  Persistence/AppDbContext.cs                + DbSet de Grupo y Horario
src/Matricula.Api/
  Controllers/{Grupos,OfertaAcademica}Controller.cs   nuevos
  Controllers/{Cursos,Aulas,Periodos,Profesores,Me}Controller.cs   + endpoints de consulta y eliminación
  Seguridad/Politicas.cs                     + Profesor
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/{GrupoTests,HorarioTests,ReglasHorarioTests}.cs
```

## Code Style

Igual que `catalogos`: constructores primarios, un archivo por clase, constructor privado para EF, setters privados, reglas puras en el dominio, proyecciones con `Select` y `AsNoTracking` en las lecturas, pruebas `Metodo_Escenario_Resultado` y `[Authorize(Policy = Politicas.X)]`, nunca `Roles`.

```csharp
public async Task<GrupoDetalleResponse> AgregarHorarioAsync(
    int grupoId, AgregarHorarioRequest request, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);

    var grupo = await BuscarConHorariosAsync(grupoId, cancellationToken);
    var horario = grupo.AgregarHorario(request.Dia, request.HoraInicio, request.HoraFin); // RN-14, RN-15
    await ValidarTraslapeProfesorAsync(grupo, [horario], cancellationToken);              // RN-07
    await ValidarTraslapeAulaAsync(grupo, [horario], cancellationToken);                  // RN-08

    await db.SaveChangesAsync(cancellationToken);
    return await ObtenerAsync(grupoId, cancellationToken);
}
```

```csharp
[Fact]
public void SeTraslapaCon_BloquesContiguos_DevuelveFalse()
{
    var a = CrearHorario(DiaSemana.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0));
    var b = CrearHorario(DiaSemana.Lunes, new TimeOnly(10, 0), new TimeOnly(12, 0));

    Assert.False(a.SeTraslapaCon(b));
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD):
  - `ReglasHorario` y `Horario.SeTraslapaCon`: mismo día con traslape parcial, contenido e idéntico; bloques contiguos; días distintos; `HoraFin` igual o anterior a `HoraInicio`.
  - `Grupo`: normaliza el NRC, nace `Planificado` con `CuposOcupados = 0`; cada transición permitida y no permitida de RF-47; cancelar desde cada estado salvo `Finalizado`; abrir sin horarios falla; `AgregarHorario` rechaza traslapes internos y grupos finalizados o cancelados; `QuitarHorario` solo en `Planificado`; edición según el estado (supuesto 4); `CuposDisponibles`.
- **Manual**, con la API y PostgreSQL en marcha, usando `Matricula.Api.http` y Scalar:

| Qué | Esperado |
|---|---|
| Crear un grupo presencial y uno virtual válidos | `201` + `Location` válido; estado `Planificado` |
| NRC repetido con otra capitalización | `409 RN-13` |
| Curso, profesor o aula inactivos | `409 RN-17` |
| Cupo mayor que la capacidad del aula | `409 RN-09` |
| Virtual con aula, presencial sin aula, cupo 0, NRC vacío o de 11 caracteres, modalidad inexistente | `400` con el campo y el mensaje |
| Agregar un bloque con `horaFin ≤ horaInicio` | `400` (RN-15) |
| Dos bloques traslapados en el mismo grupo | `409 RN-14` |
| Bloque que choca con otro grupo del mismo profesor / aula en el mismo periodo | `409 RN-07` / `409 RN-08`, con el NRC del otro grupo |
| El mismo choque con un grupo `Cancelado` o de otro periodo | `201` |
| Bloques contiguos (08:00–10:00 y 10:00–12:00) | `201` |
| Cambiar el profesor o el aula a uno con choque de horario | `409 RN-07` / `409 RN-08` |
| Reducir la capacidad de un aula por debajo del cupo de un grupo | `409 RN-09` |
| Recorrer Planificado → Abierto → Cerrado → En curso → Finalizado | `204` en cada paso |
| Saltar un estado, retroceder, cancelar un grupo finalizado | `409 RF-47` |
| Abrir un grupo sin horarios | `409 RF-47` |
| Quitar un bloque en `Planificado` / en `Abierto` | `204` / `409 RF-48` |
| Editar curso o periodo de un grupo `Abierto`; editar un grupo `Finalizado` | `409 RF-45` |
| Listar grupos con cada filtro y paginación | Filtra y pagina |
| Listar y ver el detalle con token de profesor | Solo sus grupos; el detalle de otro → `403 RN-18` |
| Listar y ver el detalle con token de estudiante | Solo los `Abierto`; el detalle de otro estado → `403 RN-18` |
| `GET /oferta-academica` sin token, con y sin periodo vigente | `200` con los abiertos del vigente / `200` vacío |
| `GET /cursos/{id}/grupos` como admin y como estudiante | Todos los estados / solo `Abierto` |
| `GET /profesores/{id}/horario`, `/me/horario`, `/me/grupos` | Bloques ordenados por día y hora, sin cancelados |
| `GET /me/grupos` con token de estudiante | `403` |
| `GET /aulas/{id}/ocupacion` sin `periodoId` y sin periodo vigente | `404` pidiendo el `periodoId` |
| Desactivar un profesor (por las dos rutas), curso o aula con grupos activos | `409 RN-37` |
| Desactivar los mismos con todos sus grupos `Finalizado` o `Cancelado` | `204` |
| Eliminar un curso, aula, periodo o profesor sin grupos / con grupos | `204` / `409 RN-34` |
| Eliminar un profesor sin grupos | Desaparecen el profesor y su cuenta; su token deja de valer (`401`); su correo y su cédula se pueden registrar de nuevo |
| La ruta del controller de oferta académica | `/api/v1/oferta-academica` (verificación heredada de `fundacion` T3) |
| Escrituras con token de profesor o de estudiante | `403` |
| Cualquier endpoint, salvo la oferta, sin token | `401` |
| Cualquier endpoint con un cambio de contraseña pendiente | `403` (RN-24) |

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. Mensajes en español y comprensibles, y los de traslape nombran el grupo con el que choca. Validar en el servicio antes de guardar. `IFechaLocal` para la fecha de hoy. Excluir los grupos `Cancelado` de los traslapes.
- **Preguntar antes:** añadir paquetes NuGet. Cambiar el esquema fuera de esta spec. Añadir reglas de negocio que no estén en los requerimientos o en los supuestos aprobados.
- **Nunca:** eliminar grupos o cancelar con efecto sobre matrículas en este módulo. Eliminar una cuenta de usuario por sí sola o la de un administrador (RN-35); solo se elimina junto con su profesor. Eliminación en cascada. `[Authorize(Roles = ...)]`. Exponer la oferta con datos del profesor más allá de su nombre.

## Success Criteria

1. La migración crea `grupo` y `horario` con FK `Restrict`, índice único en `nrc` y los enums como texto.
2. El administrador crea, lista, consulta y edita grupos, gestiona sus horarios y los lleva por sus estados (RF-42 a RF-45, RF-47, RF-48).
3. Ningún profesor ni aula queda con dos bloques traslapados en el mismo periodo, salvo con grupos cancelados (RN-07, RN-08, RN-14), y ningún grupo supera la capacidad de su aula, tampoco al reducirla (RN-09).
4. No se usan cursos, profesores ni aulas inactivos en grupos nuevos (RN-17), y no se desactivan mientras tengan grupos activos (RN-37).
5. Cursos, aulas, periodos y profesores sin grupos se eliminan, el profesor junto con su cuenta; con grupos responden `409 RN-34`.
6. La oferta académica es pública y muestra los grupos abiertos del periodo vigente con sus cupos disponibles (RF-50).
7. Las consultas 4, 5, 7 y 8 responden con el acceso de la matriz §2.2 y RN-18.
8. Las verificaciones manuales de la tabla dan el resultado esperado, incluida la heredada de `fundacion` T3.
9. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **NRC escrito por el administrador.** RF-42 lo lista como dato del grupo. Se recorta, se guarda en mayúsculas y es único en todo el sistema, no por periodo (RN-13). Solo se limita la longitud (10).
2. **Aula según modalidad.** `Presencial` e `Hibrida` llevan aula; `Virtual` no. RN-09 y RN-08 solo aplican con aula.
3. **Traslapes solo dentro del mismo periodo y sin cancelados.** RN-07 y RN-08 dicen "en el mismo periodo"; como los periodos pueden traslaparse (decisión 4 de `catalogos`), un profesor podría tener dos grupos a la misma hora en periodos distintos traslapados. Se acepta por apegarse a la regla. Los grupos `Finalizado` sí cuentan.
4. **Edición por estado (RF-45).**
   - `Planificado`: todo se edita.
   - `Abierto`, `Cerrado`, `EnCurso`: profesor, aula, modalidad y cupo (para reasignar, como piden RF-26 y RN-37); curso, periodo y NRC no.
   - `Finalizado`, `Cancelado`: nada.
   - El cupo nunca baja de `CuposOcupados`.
5. **Transiciones de RF-47 solo hacia adelante.** Planificado → Abierto → Cerrado → En curso → Finalizado, y Cancelado desde cualquiera salvo Finalizado. No se retrocede (por ejemplo, reabrir un grupo cerrado) ni se salta un estado. Los cambios de estado son manuales; ninguno ocurre por fechas.
6. **Abrir exige horario.** §5.11 dice que un grupo tiene uno o varios bloques: no se abre un grupo sin bloques. Se pueden agregar bloques en cualquier estado salvo `Finalizado` y `Cancelado`, y quitarlos solo en `Planificado` (RF-48).
7. **Visibilidad.** El estudiante ve solo grupos `Abierto` en `GET /grupos`, el detalle y los grupos de un curso; `matriculas` ampliará su acceso a los grupos donde está matriculado. El profesor ve en `GET /grupos` solo los suyos, pero en `/cursos/{id}/grupos` ve los abiertos del curso, como cualquier usuario (consulta 4).
8. **Oferta sin periodo vigente** responde `200` con la lista vacía: para un visitante, "no hay oferta" no es un error. Muestra el nombre del profesor, no su correo ni su teléfono.
9. **`CuposOcupados` se crea aquí.** RF-44 y RF-50 necesitan mostrar cupos ocupados y disponibles, así que la columna existe desde este módulo con valor `0`. `matriculas` la mantiene y añade el token `xmin`.

## Decisiones

Se aplican las decisiones 1 a 5, 9 y 10 del mapa, y además:

1. **La eliminación de grupos queda para `matriculas`** (2026-11-05). La decisión 4 del mapa le asigna RF-46, cuya guarda ("solo si nunca tuvo matrículas") depende de la tabla `matricula`. Un `DELETE` sin guarda dejaría abierta una eliminación que nadie valida, el mismo criterio que la decisión 1 de `catalogos`.

2. **`CuposOcupados` como contador** (2026-11-05). Sigue `docs/arquitectura.md` §5: el contador se lee aquí y lo mantienen `matriculas` junto con el `xmin`, para la concurrencia del último cupo.

3. **Códigos de error de RF** (2026-11-05). Las restricciones que no son una RN (transiciones de estado, quitar bloques fuera de `Planificado`, edición por estado) usan el RF como `code`, para que el cliente pueda distinguirlas.

4. **Al eliminar un profesor se elimina también su cuenta** (2026-11-05). Resuelve lo que la decisión 1 de `profesores` dejó pendiente. Un profesor sin grupos se registró por error y su cuenta no tiene historial: conservarla inactiva dejaría una cuenta sin persona (contra RN-31) y bloquearía su correo para siempre (RN-13). Ambos se borran en un solo `SaveChangesAsync`, sin cascada: solo `profesor.usuario_id` apunta a la cuenta, y una cuenta de profesor nunca es `creado_por_id` de otra, porque solo los administradores crean cuentas. Las sesiones abiertas se cortan solas, porque `ValidacionSecurityStamp` ya no encuentra la cuenta. Se corrigieron RF-14, RF-19, RF-26, RN-35 y la tabla de §4.2; la regla vale también para los estudiantes, que eliminará `matriculas` (decisión 10 del mapa).

## Preguntas abiertas

Ninguna.
