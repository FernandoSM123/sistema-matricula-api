# Spec: matriculas

Módulo `matriculas` del [mapa de módulos](mapa-modulos.md). Depende de `grupos` y `estudiantes`. Es el último módulo del mapa.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/matriculas/plan.md) | [Aprobadas](../tasks/matriculas/todo.md) | 12/13 |

**Historial:**
- 2026-11-10: v1 en revisión, con once supuestos y tres preguntas abiertas (excepciones del administrador en RN-11, alcance de RN-36 y RN-06 al agregar horarios).
- 2026-11-10: resuelta la pregunta 1: al administrador no se le aplican las restricciones comunes de matrícula (estado del grupo y fechas), sin marca en la petición. Se reescribió el supuesto 1, y el usuario confirmó que RN-01, RN-03, RN-05 y RN-06 se mantienen para el administrador.
- 2026-11-10: resuelta la pregunta 2 con la opción (a): RN-36 bloquea si hay matrículas activas en cualquier periodo en curso (supuesto 12).
- 2026-11-10: resuelta la pregunta 3 con la opción (a): agregar un bloque que haría chocar a un estudiante matriculado responde `409 RN-06` (supuesto 13). Sin preguntas abiertas; v1 queda en revisión hasta que el usuario la lea y la apruebe.
- 2026-11-10: v1 aprobada, con sus trece supuestos. Plan y lista de 13 tareas en revisión.
- 2026-11-10: plan y lista de tareas aprobados; la implementación empezará por T1.

## Objetivo

Que un estudiante se matricule, se retire o cambie de grupo por su cuenta, y que el administrador lo haga por cualquier estudiante, aplicando las reglas del instituto: estudiante activo, grupo abierto con cupo, fechas del periodo, sin repetir curso y sin choques de horario. Si dos personas compiten por el último cupo, solo una lo obtiene. Además, cerrar las consultas que dependen de las matrículas y las guardas que el mapa dejó pendientes en otros módulos.

Cubre RF-20, RF-21, RF-46, RF-49 y RF-51 a RF-54; las reglas RN-01 a RN-06, RN-10 a RN-12, RN-18 y RN-36; las consultas 2, 3, 6 y 10 de la sección 6 (decisión 3 del mapa); las guardas que la decisión 4 del mapa asigna a este módulo (RN-36 y eliminación de estudiantes, eliminación de grupos y RN-12), y la decisión 10 del mapa para los estudiantes.

**Usuarios:**
- Estudiante: se matricula, se retira, cambia de grupo y consulta sus matrículas, su historial y su horario (RN-18).
- Administrador: hace lo mismo por cualquier estudiante, sin las restricciones de estado del grupo ni de fechas (RN-11, supuesto 1); consulta los estudiantes de cualquier grupo; elimina grupos y estudiantes sin datos relacionados.
- Profesor: consulta los estudiantes matriculados en sus grupos (RF-49, RN-18).
- Visitante: nada nuevo; la oferta (RF-50) ya muestra los cupos que este módulo mantiene.

### Alcance

1. **Entidad `Matricula`** (§5.10 de los requerimientos). Hereda de `EntidadBase`.

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `EstudianteId` | `int` | FK `Restrict` |
   | `GrupoId` | `int` | FK `Restrict` |
   | `FechaMatricula` | `DateOnly` | `IFechaLocal.Hoy()` al matricular (decisión 9 del mapa). `FechaCreacion` guarda la marca de tiempo UTC |
   | `Estado` | `EstadoMatricula` | Enum nuevo: `Activa`, `Retirada`. Se guarda como string. `Activa` al crear |
   | `FechaRetiro` | `DateOnly?` | Se asigna al retirar |
   | `MotivoRetiro` | `MotivoRetiro?` | Enum nuevo: `RetiroVoluntario`, `CambioGrupo`, `CancelacionGrupo`. Se guarda como string |

   Índices:
   - Único filtrado `(estudiante_id, grupo_id) WHERE estado = 'Activa'` (`docs/arquitectura.md` §5): no hay dos matrículas activas del mismo estudiante en el mismo grupo, pero sí se puede volver a matricular después de retirarse.
   - `(grupo_id, estado)` para RF-49 y RN-12.

   Métodos de dominio:
   - Constructor: nace `Activa` con su fecha.
   - `Retirar(fecha, motivo)`: pasa a `Retirada` con fecha y motivo; retirar una ya retirada lanza `409 RF-52`.

   **Cambios en `Grupo`:**
   - `uint Version` mapeada a `xmin` con `.IsRowVersion()` (token de concurrencia, `docs/arquitectura.md` §5).
   - `OcuparCupo()`: `409 RN-03` si `CuposDisponibles == 0`; si no, `CuposOcupados++`.
   - `LiberarCupo()`: `CuposOcupados--`, nunca por debajo de 0.
   - `AceptaMovimientos(esAdministrador)` o equivalente: RN-02 y RN-11 (supuesto 1).

   **Reglas puras nuevas** en `Domain/Reglas/ReglasMatricula.cs` (o métodos de `Periodo`): RN-04 y RN-10 ya existen como `Periodo.EnRangoMatricula` y `Periodo.PermiteRetiro`; se añade el traslape de periodos del supuesto 4.

   Migración `AgregaMatricula` (tabla `matricula`; `xmin` no crea columna).

2. **Endpoints de matrícula**

   | Método y ruta | Acceso | RF / RN | Respuesta |
   |---|---|---|---|
   | `POST /api/v1/matriculas` | Administrador | RF-51, RN-01…06, RN-11 | `201` + `Location` · `400` · `404` (estudiante o grupo) · `409` |
   | `POST /api/v1/me/matriculas` | Estudiante | RF-51, RN-01…06, RN-11, RN-18 | `201` + `Location` · `400` · `404` (grupo) · `409` |
   | `GET /api/v1/matriculas/{id}` | Administrador o el estudiante dueño | RF-54, RN-18 | `200` · `403 RN-18` · `404` |
   | `POST /api/v1/matriculas/{id}/retiro` | Administrador o el estudiante dueño | RF-52, RN-10, RN-11, RN-18 | `204` · `403 RN-18` · `404` · `409` |
   | `POST /api/v1/matriculas/{id}/cambio-grupo` | Administrador o el estudiante dueño | RF-53, RN-02…06, RN-10, RN-11, RN-18 | `201` + `Location` (la matrícula nueva) · `400` · `403 RN-18` · `404` · `409` |

   Body:
   - Administrador: `{ "estudianteId": 1, "grupoId": 2 }`.
   - Estudiante: `{ "grupoId": 2 }`. El estudiante sale del claim `estudiante_id`, nunca del body (RN-18).
   - Cambio de grupo: `{ "grupoDestinoId": 3 }`.

   Detalle de los flujos:
   - **Matricular (RF-51)**, en este orden, cada una con su `409` y su `code`:
     1. RN-01: el estudiante existe (`404`) y su cuenta está activa.
     2. RN-02 / RN-11: el grupo está `Abierto` (no aplica al administrador: supuesto 1).
     3. RN-04: hoy (`IFechaLocal.Hoy()`) está en el rango de matrícula del periodo del grupo (no aplica al administrador: supuesto 1).
     4. RN-05: el estudiante no tiene otra matrícula `Activa` en un grupo del mismo curso y periodo. Incluye el mismo grupo: se responde `409 RN-05` y no la violación del índice.
     5. RN-06: ningún bloque del grupo se traslapa con los bloques de los grupos donde el estudiante tiene matrículas `Activa` (supuesto 4). El mensaje nombra el NRC y el bloque con que choca.
     6. RN-03: `Grupo.OcuparCupo()`.
     7. Un solo `SaveChangesAsync`. Si otro proceso cambió el grupo entre la lectura y el guardado, `xmin` lo detecta (supuesto 6).
   - **Retirar (RF-52):** la matrícula debe estar `Activa` (`409 RF-52`); RN-11 sobre el estado del grupo y RN-10 sobre la fecha límite de retiro (no aplican al administrador: supuesto 1). Queda `Retirada` con `FechaRetiro = hoy` y `MotivoRetiro = RetiroVoluntario`; el grupo libera el cupo. Un solo guardado.
   - **Cambiar de grupo (RF-53):** el destino debe ser otro grupo del mismo curso y periodo (`409 RF-53`). Sobre el origen se aplican las reglas del retiro (RN-10, RN-11); sobre el destino las de matricular salvo RN-04 (supuesto 3), y RN-05 y RN-06 ignoran la matrícula de origen. La matrícula de origen queda `Retirada` con motivo `CambioGrupo`, se crea una nueva `Activa` en el destino y se mueven los cupos, todo en un solo `SaveChangesAsync`: si cualquier regla o la concurrencia falla, no cambia nada.
   - **Visibilidad (RN-18):** el estudiante solo ve, retira o cambia sus propias matrículas; las de otro responden `403 RN-18`. El administrador, todas. El profesor no usa estos endpoints (`403`).

3. **Endpoints de consultas** (sección 6)

   | Método y ruta | Acceso | RF / consulta | Respuesta |
   |---|---|---|---|
   | `GET /api/v1/estudiantes/{id}/matriculas` | Administrador | RF-20, RF-54, consultas 2 y 10 | `200` paginado; filtros `periodoId`, `estado`, `search` (curso o NRC) · `404` |
   | `GET /api/v1/me/matriculas` | Estudiante | RF-20, RF-54, consultas 2 y 10 | Igual que el anterior, con el estudiante de la sesión |
   | `GET /api/v1/estudiantes/{id}/horario` | Administrador | RF-21, consulta 6 | `200` con los bloques de la semana; `periodoId` opcional · `404` |
   | `GET /api/v1/me/horario` | Profesor **o Estudiante** | RF-21, RF-28, consultas 6 y 7 | Se amplía a estudiantes; el profesor sigue igual |
   | `GET /api/v1/grupos/{id}/estudiantes` | Administrador o el profesor del grupo | RF-49, consulta 3 | `200` paginado; filtros `estado` (por defecto `Activa`), `search` (nombre, apellidos o cédula) · `403 RN-18` · `404` |

   Detalle:
   - **Matrículas de un estudiante:** RF-20 (cursos y grupos matriculados) y RF-54 (historial de matrículas y retiros) son la misma consulta con distinto filtro (supuesto 7). Cada fila: id, grupo (id, NRC, modalidad, estado), curso (código, nombre), periodo (id, nombre), profesor (nombre), fechas de matrícula y retiro, estado y motivo. Ordenado por inicio de clases del periodo descendente, nombre del curso e `Id`. Muestra matrículas de grupos y estudiantes inactivos (RN-40).
   - **Horario de un estudiante:** reutiliza `HorarioSemanalService` y `BloqueHorarioResponse` con el filtro "grupos donde el estudiante tiene una matrícula `Activa`". Sin `periodoId` usa el vigente; sin vigente `404` pidiendo el `periodoId`, igual que el del profesor.
   - **Estudiantes de un grupo:** cada fila: id de la matrícula, estudiante (id, cédula, nombre, apellidos, correo), fecha de matrícula, estado, fecha y motivo de retiro. Ordenado por apellidos, nombre e `Id`. El profesor de otro grupo recibe `403 RN-18`; el estudiante, `403`.
   - **Grupos visibles para el estudiante:** amplía el supuesto 7 de `grupos`: el detalle `GET /grupos/{id}` también responde `200` a un estudiante con una matrícula (activa o retirada) en ese grupo, aunque no esté `Abierto`. El listado `GET /grupos` sigue mostrando solo los `Abierto`; sus grupos los ve en `/me/matriculas`.

4. **Guardas sobre otros módulos** (decisión 4 del mapa)

   | Operación | Guarda | Respuesta |
   |---|---|---|
   | Desactivar estudiante (`/estudiantes/{id}/desactivar` y `/usuarios/{id}/desactivar` de una cuenta Estudiante) | RN-36: sin matrículas `Activa` en ningún periodo en curso, es decir, con `InicioMatricula ≤ hoy ≤ FinClases` (supuesto 12) | `409 RN-36` con la cantidad de matrículas |
   | `DELETE /api/v1/estudiantes/{id}` | Solo si nunca tuvo matrículas, activas o retiradas (RF-19, RN-34). Se elimina también su cuenta, en el mismo guardado (RN-35, decisión 10 del mapa) | `204` · `404` · `409 RN-34` |
   | `DELETE /api/v1/grupos/{id}` | Solo si nunca tuvo matrículas (RF-46, RN-34). Se eliminan también sus bloques de horario, en el mismo guardado y sin cascada | `204` · `404` · `409 RN-34` |
   | Cancelar un grupo (`POST /grupos/{id}/estado` con `Cancelado`) | RN-12: todas sus matrículas `Activa` pasan a `Retirada` con motivo `CancelacionGrupo` y fecha de hoy, y `CuposOcupados` queda en 0. Un solo guardado con el cambio de estado. RN-10 no aplica | `204` |
   | Agregar un bloque a un grupo con matrículas activas | RN-06 para sus estudiantes: el bloque nuevo no puede chocar con sus otros grupos (supuesto 13) | `409 RN-06` con la cantidad de estudiantes afectados |

   RN-36 vive en `UsuarioService.DesactivarAsync` para las cuentas Estudiante, como RN-37 para los profesores, así aplica por las dos rutas. Al eliminar un estudiante, sus sesiones se cortan solas porque `ValidacionSecurityStamp` ya no encuentra la cuenta (decisión 4 de `grupos`).

5. **Autorización**
   - `MatriculasController` nuevo: `POST` con `Politicas.Administrador`; `GET {id}`, `retiro` y `cambio-grupo` con una política nueva `Politicas.AdministradorOEstudiante`. La propiedad (RN-18) se resuelve en el servicio con `ICurrentUser`.
   - `MeController`: `POST /me/matriculas` y `GET /me/matriculas` con `Politicas.Estudiante`; `GET /me/horario` pasa a una política nueva `Politicas.ProfesorOEstudiante` y el servicio decide por rol.
   - `GruposController`: `GET {id}/estudiantes` con `Politicas.AdministradorOProfesor` (ya existe); `DELETE {id}` con `Politicas.Administrador`.
   - `EstudiantesController`: `DELETE {id}`, `GET {id}/matriculas` y `GET {id}/horario` heredan su `Politicas.Administrador` de clase.

6. **Validación:** un validador por DTO de entrada (ids obligatorios y mayores que 0, `estado` válido). Todas las reglas de negocio necesitan la BD y las valida el servicio con `409`.

7. **Concurrencia (RNF de concurrencia):** token `xmin` en `Grupo`. Matricular, retirar, cambiar de grupo y cancelar modifican `CuposOcupados`, así que dos operaciones simultáneas sobre el mismo grupo no pueden guardar las dos (supuesto 6).

8. **Documentación:** `Matricula.Api.http` con los flujos del módulo.

### Fuera de alcance

- Lista de espera, pagos, calificaciones y certificados (§1.3 de los requerimientos).
- Correos al estudiante al matricularse, retirarse o al cancelarse un grupo: los requerimientos solo piden correos de contraseñas (§7).
- Cambiar automáticamente el estado de los grupos por fechas o al llenarse el cupo (supuesto 5 de `grupos`).
- Eliminar matrículas: nunca (RN-35).
- Reportes de ocupación o demanda (§1.3).
- Pruebas de integración y de concurrencia automatizadas: diferidas. La concurrencia se verifica a mano.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, xUnit 2.9.3. **No se añaden paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaMatricula --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/
  Entities/Matricula.cs                      nuevo
  Entities/Grupo.cs                          + Version, OcuparCupo, LiberarCupo, regla de RN-02/RN-11
  Enums/{EstadoMatricula,MotivoRetiro}.cs    nuevos
  Reglas/ReglasMatricula.cs                  nuevo si hace falta (traslape de periodos, supuesto 4)
src/Matricula.Application/
  Abstractions/IAppDbContext.cs              + DbSet<Matricula>
  Matriculas/                                MatriculaService, DTOs, validadores, proyecciones
  Matriculas/Consultas/                      consultas 2, 3, 6 y 10 (si conviene separarlas)
  Grupos/GrupoService.cs                     + RN-12 al cancelar, eliminar (RF-46), visibilidad del estudiante
  Grupos/Consultas/HorarioSemanalService.cs  + horario del estudiante (RF-21)
  Estudiantes/EstudianteService.cs           + eliminar junto con su cuenta
  Cuentas/UsuarioService.cs                  + RN-36 para cuentas Estudiante
  DependencyInjection.cs                     + MatriculaService
src/Matricula.Infrastructure/
  Persistence/Configurations/MatriculaConfiguration.cs   nuevo
  Persistence/Configurations/GrupoConfiguration.cs       + xmin
  Persistence/Migrations/                    AgregaMatricula
  Persistence/AppDbContext.cs                + DbSet<Matricula>
src/Matricula.Api/
  Controllers/MatriculasController.cs        nuevo
  Controllers/{Me,Estudiantes,Grupos}Controller.cs       + endpoints
  Seguridad/Politicas.cs                     + AdministradorOEstudiante, ProfesorOEstudiante
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/{MatriculaTests,GrupoCuposTests}.cs (o ampliar GrupoTests)
```

## Code Style

Igual que `grupos`: constructores primarios, un archivo por clase, constructor privado para EF, setters privados, reglas puras en el dominio, proyecciones con `Select` y `AsNoTracking` en las lecturas, `Include` solo para modificar, pruebas `Metodo_Escenario_Resultado` y `[Authorize(Policy = Politicas.X)]`, nunca `Roles`.

```csharp
public async Task<MatriculaResponse> CambiarGrupoAsync(
    int matriculaId, CambioGrupoRequest request, CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);

    var origen = await BuscarPropiaAsync(matriculaId, cancellationToken);           // RN-18
    var destino = await BuscarGrupoConHorariosAsync(request.GrupoDestinoId!.Value, cancellationToken);
    var hoy = fechaLocal.Hoy();

    ValidarRetiro(origen, hoy);                                     // RN-10, RN-11 (no al administrador)
    ValidarMismoCursoYPeriodo(origen.Grupo, destino);               // RF-53
    ValidarIngreso(destino, hoy, validarRangoMatricula: false);     // RN-02, RN-11 (no al administrador)
    await ValidarCursoRepetidoAsync(origen.EstudianteId, destino, origen.Id, cancellationToken); // RN-05
    await ValidarTraslapesAsync(origen.EstudianteId, destino, origen.Id, cancellationToken);    // RN-06

    origen.Retirar(hoy, MotivoRetiro.CambioGrupo);
    origen.Grupo.LiberarCupo();
    destino.OcuparCupo();                                                           // RN-03
    var nueva = new Matricula(origen.EstudianteId, destino.Id, hoy);
    db.Matriculas.Add(nueva);

    await db.SaveChangesAsync(cancellationToken);                                   // todo o nada; xmin en ambos grupos
    return await ObtenerAsync(nueva.Id, cancellationToken);
}
```

```csharp
[Fact]
public void OcuparCupo_GrupoLleno_LanzaRN03()
{
    var grupo = CrearGrupoAbierto(cupo: 1);
    grupo.OcuparCupo();

    var ex = Assert.Throws<BusinessRuleException>(grupo.OcuparCupo);
    Assert.Equal("RN-03", ex.Code);
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD):
  - `Matricula`: nace `Activa` con su fecha; `Retirar` asigna estado, fecha y motivo; retirar dos veces lanza `RF-52`.
  - `Grupo`: `OcuparCupo` hasta llenar y `RN-03` al pasarse; `LiberarCupo` no baja de 0; la regla de RN-02/RN-11 por cada estado, para estudiante y para administrador.
  - Verificación heredada de `grupos` T1: `Grupo.ActualizarDatos` rechaza un cupo menor que `CuposOcupados` (`RF-45`), ahora que existe `OcuparCupo`.
  - Traslape de periodos (supuesto 4): periodos traslapados, contiguos y separados.
- **Manual**, con la API y PostgreSQL en marcha, usando `Matricula.Api.http` y Scalar:

| Qué | Esperado |
|---|---|
| Estudiante se matricula en un grupo `Abierto`, dentro del rango, sin choques | `201` + `Location`; `cuposOcupados` del grupo +1 |
| Admin matricula a un estudiante inactivo | `409 RN-01` |
| Estudiante se matricula en un grupo `Planificado` / `Cerrado` | `409 RN-02` / `409 RN-11` |
| Admin matricula, retira o cambia en un grupo `Cerrado` o `EnCurso`, o fuera de las fechas del periodo | `201` / `204` |
| Admin matricula en un grupo `Finalizado` o `Cancelado` | `409 RN-11` |
| Admin matricula en un grupo lleno, con choque de horario o con el curso repetido | `409 RN-03` / `RN-06` / `RN-05` |
| Grupo sin cupos disponibles | `409 RN-03` |
| Matricular fuera del rango de matrícula del periodo | `409 RN-04` |
| Segundo grupo del mismo curso y periodo, o el mismo grupo dos veces | `409 RN-05` |
| Grupo con un bloque que choca con otro grupo matriculado | `409 RN-06` con el NRC del otro grupo |
| Bloques contiguos con otro grupo matriculado | `201` |
| Dos peticiones simultáneas por el último cupo | Una `201`, la otra `409` (`RN-03` o `CONCURRENCIA`) |
| Retirarse antes / después de la fecha límite | `204` / `409 RN-10` |
| Retirar una matrícula ya retirada | `409 RF-52` |
| Volver a matricularse en el mismo grupo después de retirarse | `201` |
| Cambio de grupo válido | `201`; origen `Retirada` con motivo `CambioGrupo`, destino `Activa`; cupos movidos |
| Cambio a un grupo de otro curso o periodo | `409 RF-53` |
| Cambio a un grupo lleno, con choque o no `Abierto` | `409` con la regla; la matrícula original sigue `Activa` y los cupos no cambian |
| Cambio después de la fecha límite de retiro | `409 RN-10` |
| Estudiante consulta, retira o cambia la matrícula de otro | `403 RN-18` |
| `GET /me/matriculas` y `/estudiantes/{id}/matriculas` con filtros `periodoId` y `estado` | Filtra y pagina; incluye retiradas con fecha y motivo |
| `GET /me/horario` con token de estudiante y de profesor | Los bloques de sus grupos matriculados / los de sus grupos |
| `GET /estudiantes/{id}/horario` sin `periodoId` y sin periodo vigente | `404` pidiendo el `periodoId` |
| `GET /grupos/{id}/estudiantes` como admin, como su profesor y como otro profesor | `200` / `200` / `403 RN-18` |
| Estudiante ve el detalle de un grupo `Cerrado` donde está matriculado / donde no | `200` / `403 RN-18` |
| Agregar a un grupo con matriculados un bloque que choca con otro grupo de uno de ellos / que no choca | `409 RN-06` con la cantidad de estudiantes / `201` |
| Cancelar un grupo con matrículas activas | `204`; todas `Retirada` con motivo `CancelacionGrupo`; `cuposOcupados = 0` |
| Eliminar un grupo sin matrículas / con matrículas (aunque estén retiradas) | `204` y desaparecen sus bloques / `409 RN-34` |
| Eliminar un estudiante sin matrículas / con matrículas | `204`, desaparecen el estudiante y su cuenta, su token deja de valer / `409 RN-34` |
| Desactivar un estudiante con matrículas activas en un periodo en curso, aunque no sea el vigente (por las dos rutas) | `409 RN-36` |
| La oferta académica con un grupo `Abierto` lleno | Aparece con `cuposDisponibles = 0` (verificación heredada de `grupos` T8) |
| Escrituras de matrícula con token de profesor | `403` |
| Cualquier endpoint sin token | `401` |
| Cualquier endpoint con un cambio de contraseña pendiente | `403` (RN-24) |

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. Un solo `SaveChangesAsync` por operación compuesta (cambio de grupo, cancelación con RN-12, eliminación con su cuenta o sus bloques). `IFechaLocal.Hoy()` para RN-04, RN-10 y las fechas de matrícula y retiro. Mantener `CuposOcupados` en cada operación que cambia una matrícula. Mensajes en español que nombren la regla y el grupo con que choca.
- **Preguntar antes:** añadir paquetes NuGet. Cambiar el esquema fuera de esta spec. Añadir reglas que no estén en los requerimientos o en los supuestos aprobados. Enviar correos por matrícula o retiro.
- **Nunca:** eliminar matrículas (RN-35). Eliminación en cascada. Tomar el id del estudiante del body cuando la petición la hace un estudiante. Recalcular `CuposOcupados` con un `COUNT` en lugar de usar el contador con `xmin`. `[Authorize(Roles = ...)]`.

## Success Criteria

1. La migración crea `matricula` con FK `Restrict`, el índice único filtrado de matrícula activa y los enums como texto; `Grupo` usa `xmin` como token de concurrencia.
2. Estudiante y administrador matriculan, retiran y cambian de grupo (RF-51 a RF-53), cada operación con las reglas RN-01 a RN-06, RN-10 y RN-11.
3. El cambio de grupo es atómico: si falla, la matrícula original y los cupos quedan como estaban.
4. Dos matrículas simultáneas por el último cupo nunca dejan `CuposOcupados > Cupo`.
5. Al cancelar un grupo, sus matrículas activas quedan retiradas (RN-12).
6. Grupos y estudiantes sin matrículas se eliminan (el estudiante junto con su cuenta); con matrículas responden `409 RN-34`. Un estudiante con matrículas activas en un periodo en curso no se desactiva (RN-36).
7. Las consultas 2, 3, 6 y 10 responden con el acceso de la matriz §2.2 y RN-18.
8. Las verificaciones manuales de la tabla dan el resultado esperado, incluidas las dos heredadas de `grupos`.
9. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **Al administrador no se le aplican las restricciones comunes de matrícula (RN-11)** (resuelto 2026-11-10). Sin ninguna marca en la petición, cuando matricula, retira o cambia de grupo a un estudiante se omiten:
   - el estado del grupo (RN-02, RN-11): puede operar en grupos `Planificado`, `Abierto`, `Cerrado` y `EnCurso`;
   - las fechas del periodo (RN-04, RN-10).

   Se mantienen, porque protegen la integridad de los datos y no son restricciones de calendario: RN-01 (estudiante activo), RN-03 (cupo), RN-05 (curso repetido) y RN-06 (choque de horario). Tampoco puede operar en grupos `Finalizado` ni `Cancelado`: matricular en un grupo cancelado contradiría RN-12. El estudiante sigue todas las reglas.
2. **Una matrícula retirada no se reactiva.** Para volver al grupo se crea otra matrícula, con todas las reglas. El historial (RF-54) muestra las dos.
3. **El cambio de grupo se rige por RN-10, no por RN-04.** RN-10 nombra explícitamente los cambios de grupo; así un cambio es posible después del fin de matrícula y hasta la fecha límite de retiro.
4. **RN-06 compara con los periodos que se traslapan.** RN-06 no dice "en el mismo periodo". Se compara con los bloques de los grupos donde el estudiante tiene matrículas `Activa` y cuyo periodo se traslapa en fechas de clases con el del grupo nuevo (incluido el mismo periodo). Los grupos `Cancelado` no cuentan (sus matrículas ya están retiradas) ni los `Finalizado`.
5. **RN-05 solo con matrículas activas.** Un estudiante que se retiró de un grupo puede matricular otro grupo del mismo curso y periodo.
6. **Concurrencia.** El conflicto de `xmin` sigue respondiendo `409 CONCURRENCIA` desde `GlobalExceptionHandler`, pero en las operaciones de matrícula el servicio lo convierte en el mensaje de `docs/arquitectura.md` §5: "El grupo cambió mientras se procesaba la solicitud (por ejemplo, se ocupó el último cupo). Intente de nuevo." No se reintenta automáticamente.
7. **RF-20 y RF-54 son un mismo endpoint.** `GET .../matriculas` con `estado` opcional: sin él devuelve activas y retiradas (historial, RF-54); con `estado=Activa`, los cursos y grupos matriculados (RF-20). Ambos con filtro por periodo.
8. **El profesor ve de sus estudiantes** cédula, nombre, apellidos y correo; no teléfono ni dirección. Por defecto solo las matrículas activas; con `estado=Retirada` ve los retirados.
9. **Eliminar un grupo borra sus bloques.** §4.2 permite quitar bloques solo en `Planificado`, pero un grupo sin matrículas que se elimina por error no tiene historial; sus bloques se quitan en el mismo guardado, de forma explícita (no por cascada), en cualquier estado.
10. **RN-12 no aplica fechas.** Cancelar un grupo retira sus matrículas aunque haya pasado la fecha límite de retiro: la cancelación es una decisión del instituto, no un retiro del estudiante.
11. **La fecha de matrícula es `DateOnly`** local del instituto, como pide §5.10 ("Fecha de matrícula"); la hora exacta queda en `FechaCreacion` (UTC).
12. **RN-36 con periodos traslapados** (resuelto 2026-11-10). "El periodo vigente" se lee como cualquier periodo en curso (`InicioMatricula ≤ hoy ≤ FinClases`), no solo el que devuelve `ObtenerIdVigenteAsync`: si dos periodos coinciden, las matrículas activas en cualquiera de ellos impiden desactivar al estudiante.
13. **RN-06 al agregar un bloque** (resuelto 2026-11-10). Al agregar un bloque a un grupo con matrículas `Activa`, `GrupoService.AgregarHorarioAsync` revisa, para cada estudiante matriculado, que el bloque nuevo no se traslape con los bloques de sus otros grupos con matrícula `Activa` (mismo criterio de periodos del supuesto 4). Si alguno chocaría, el bloque se rechaza con `409 RN-06`, indicando cuántos estudiantes quedarían con choque y el NRC de uno de los grupos con que chocan; no se nombra a los estudiantes. Se revisa después de RN-14, RN-07 y RN-08, y aplica igual al administrador. Así ningún estudiante queda con choque de horario.

## Decisiones

Se aplican las decisiones 1 a 5, 9 y 10 del mapa, y las decisiones 2 y 3 de `grupos` (contador `CuposOcupados` y códigos de error de RF).

## Preguntas abiertas

1. ~~¿Qué permite la excepción del administrador en RN-11?~~ Resuelta el 2026-11-10: ver el supuesto 1.
2. ~~Alcance de RN-36 con periodos traslapados.~~ Resuelta el 2026-11-10: ver el supuesto 12.
3. ~~RN-06 al agregar un bloque a un grupo con matrículas activas.~~ Resuelta el 2026-11-10: ver el supuesto 13.

No quedan preguntas abiertas.
