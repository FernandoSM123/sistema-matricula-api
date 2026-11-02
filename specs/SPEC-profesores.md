# Spec: profesores

Módulo `profesores` del [mapa de módulos](mapa-modulos.md). Depende de `acceso`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/profesores/plan.md) | [Aprobadas](../tasks/profesores/todo.md) | 5/7 |

**Historial:**
- 2026-10-28: v1 en revisión.
- 2026-10-28: resueltas las preguntas abiertas: la eliminación queda para `grupos` y la verificación de kebab-case sigue pendiente (decisiones 1 y 2).
- 2026-10-28: v1 aprobada. Plan y lista de 7 tareas en revisión.
- 2026-10-28: plan y lista de tareas aprobados; la implementación empieza por T1.

## Objetivo

Que el administrador registre a los profesores del instituto y los gestione: listar, consultar, editar, desactivar y reactivar. El profesor recibe su temporal por correo, entra al sistema y consulta su perfil. Cubre RF-03, RF-22 a RF-25, la parte de desactivar y reactivar de RF-26 y las reglas RN-13 (cédula de profesores), RN-20, RN-22, RN-31, RN-38, RN-39 y RN-41.

**Usuarios:**
- Administrador: registra profesores con una temporal, los lista, consulta, edita, desactiva y reactiva.
- Profesor: ingresa con la temporal, hace el cambio obligatorio y consulta su perfil. No edita sus datos (matriz §2.2: "Gestionar profesores" solo para el administrador).
- El equipo de desarrollo: `grupos` usa la entidad `Profesor` y el claim `profesor_id` (RN-18), y añade aquí RN-37 y la eliminación (decisión 4 del mapa).

### Alcance

1. **Entidad `Profesor`** (§5.3 de los requerimientos; decisiones 2 y 7 del mapa).

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Id`, `FechaCreacion`, `FechaModificacion` | de `EntidadBase` | |
   | `Cedula` | `string(20)` | Índice único en la tabla `profesor` (RN-13, única por tabla) |
   | `Nombre` | `string(100)` | |
   | `Apellidos` | `string(100)` | |
   | `Telefono` | `string(20)` | |
   | `Especialidad` | `string(100)?` | Texto libre (supuesto 2). Vacía o en blanco se guarda como `null` |
   | `FechaContratacion` | `DateOnly?` | La indica el administrador (supuesto 3) |
   | `UsuarioId` | `int` | FK a `Usuario`, `Restrict`, índice único (1:1, RN-31) |

   Sin `Correo` ni `Activo`: se leen de su `Usuario` (decisiones 2 y 7). El constructor exige una cuenta con rol `Profesor`, igual que `Estudiante` y `Administrador`. Método: `ActualizarDatos(...)`. Migración `AgregaProfesor`.

2. **Endpoints**

   | Método y ruta | Acceso | RF / RN | Respuesta |
   |---|---|---|---|
   | `POST /api/v1/profesores` | Administrador | RF-03, RF-05, RF-22, RN-13, RN-20, RN-22, RN-39 | `201` + `Location` con el profesor y `correoEnviado` · `409 RN-13` · `409 RN-39` |
   | `GET /api/v1/profesores` | Administrador | RF-23, RN-41 | `200` paginado; `search` (nombre, apellidos, cédula, especialidad o correo) e `incluirInactivos` |
   | `GET /api/v1/profesores/{id}` | Administrador | RF-24 | `200` · `404` |
   | `PUT /api/v1/profesores/{id}` | Administrador | RF-25, RN-13, RN-31 | `200` con el profesor · `404` · `409 RN-13` |
   | `POST /api/v1/profesores/{id}/desactivar` | Administrador | RF-26, RN-38 | `204` · `404` |
   | `POST /api/v1/profesores/{id}/reactivar` | Administrador | RF-26, RN-38, RN-39 | `204` · `404` |
   | `GET /api/v1/me` | Autenticado | RF-13 | Para un profesor, incluye sus datos personales |

   La respuesta de un profesor incluye `id`, `usuarioId`, `cedula`, `nombre`, `apellidos`, `telefono`, `correo`, `especialidad`, `fechaContratacion`, `activo`, `fechaCreacion` y `ultimoIngreso`. Con `usuarioId` también funcionan `/api/v1/usuarios/{id}/reenviar-temporal`, `desactivar` y `reactivar` de `acceso`.

   Detalle de los flujos:
   - **Registro:** `{ cedula, nombre, apellidos, telefono, correo, especialidad?, fechaContratacion? }`. Usa `CuentaService.CrearCuentaConTemporalAsync` (rol `Profesor`, temporal de 72 horas), guarda cuenta y profesor en un solo `SaveChangesAsync` y después envía la temporal. Correo ya usado → `409 RN-13`. Cédula de un profesor activo → `409 RN-13`; de uno inactivo → `409 RN-39` con un mensaje que ofrece reactivarlo e incluye su `profesorId` y `usuarioId`.
   - **Editar:** los mismos datos que el registro. Si cambia el correo, se actualiza el `Usuario` con `CuentaService.CambiarCorreoAsync` (RN-31). Cédula de otro profesor o correo de otra cuenta → `409 RN-13`.
   - **Desactivar y reactivar:** resuelven el `usuarioId` del profesor y llaman a `UsuarioService.DesactivarAsync` / `ReactivarAsync` de `acceso`. Es la misma operación que `/usuarios/{id}/desactivar` (decisión 2), así que RN-38 se cumple sin sincronizar nada y la guarda RN-37 que añada `grupos` en `UsuarioService` aplica a las dos rutas.

3. **Perfil y token** (extiende lo de `acceso` y `estudiantes`)
   - `GET /api/v1/me`: `DatosPersonalesResponse` incorpora `Especialidad` y `FechaContratacion`, opcionales (nulos para administradores y estudiantes). `AuthService.ObtenerPerfilAsync` toma los datos personales del administrador, del estudiante o del profesor asociado a la cuenta.
   - Claim `profesor_id`: `DatosToken` incorpora `ProfesorId?`; `AuthService` lo busca al emitir cualquier token de una cuenta con rol `Profesor` y `GeneradorToken` lo agrega. `ICurrentUser.ProfesorId` y `ClaimsToken.ProfesorId` ya existen.
   - `EnvioCorreos.ObtenerNombreAsync` también busca el nombre en `Profesores`, para que la temporal y los avisos saluden por su nombre.

4. **Validación**: el validador reutiliza `DatosPersonalesRules` (cédula, nombre, apellidos, teléfono, correo). Nuevas reglas: `Especialidad` hasta 100 caracteres; `FechaContratacion` no anterior a 1900-01-01 (supuesto 3).

5. **Documentación**: `Matricula.Api.http` con los flujos del módulo y el paso de la migración en el README si cambia algo.

### Fuera de alcance

- **Eliminar un profesor** (RF-26, "solo si nunca tuvo grupos") y la guarda **RN-37** al desactivar: los añade `grupos` (decisión 4 del mapa). Al hacerlo habrá que resolver qué pasa con su `Usuario`, porque RN-35 prohíbe eliminar cuentas (decisión 1).
- RF-27 y RF-28 (cursos, grupos y horario del profesor), consultas 5 y 7, y `/api/v1/me/grupos`, `/me/horario`: `grupos`.
- La política `Politicas.Profesor`: ningún endpoint de este módulo la usa; la añade `grupos`, el primero que la necesita.
- Edición de datos propios por el profesor: la matriz §2.2 no la incluye.
- Cambio de rol: no existe (decisión 8).
- Rate limiting: todos los endpoints son de administrador.
- Pruebas de integración: diferidas.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, MailKit 4.18.1, xUnit 2.9.3. JWT y correo ya configurados en `acceso`. **No se añaden paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaProfesor --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
smtp4dev --smtpport=2525 --urls=http://localhost:5080
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/
  Entities/Profesor.cs                       nuevo
src/Matricula.Application/
  Abstractions/IAppDbContext.cs              + DbSet<Profesor>
  Abstractions/DatosToken.cs                 + ProfesorId
  Profesores/                                ProfesorService, DTOs, validadores, proyecciones
  Common/Validacion/DatosPersonalesRules.cs  + Especialidad, FechaContratacion
  Auth/AuthService.cs                        + datos del profesor en el perfil, profesor_id al emitir el token
  Auth/DatosPersonalesResponse.cs            + campos opcionales del profesor
  Correos/EnvioCorreos.cs                    + nombre de profesores
  DependencyInjection.cs                     + ProfesorService
src/Matricula.Infrastructure/
  Persistence/Configurations/ProfesorConfiguration.cs
  Persistence/Migrations/                    AgregaProfesor
  Persistence/AppDbContext.cs                + DbSet<Profesor>
src/Matricula.Api/
  Controllers/ProfesoresController.cs        nuevo
  Seguridad/GeneradorToken.cs                + claim profesor_id
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/ProfesorTests.cs
```

## Code Style

Igual que `estudiantes`: constructores primarios, `ArgumentNullException.ThrowIfNull`, nombres de dominio en español y sufijos técnicos en inglés, un archivo por clase, pruebas `Metodo_Escenario_Resultado`, proyecciones con `Select` y `AsNoTracking` en las lecturas. Las políticas de rol con `[Authorize(Policy = Politicas.X)]`, nunca `Roles` (RN-24).

```csharp
public void ActualizarDatos(
    string cedula,
    string nombre,
    string apellidos,
    string telefono,
    string? especialidad,
    DateOnly? fechaContratacion)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(cedula);
    ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
    ArgumentException.ThrowIfNullOrWhiteSpace(apellidos);
    ArgumentException.ThrowIfNullOrWhiteSpace(telefono);

    Cedula = cedula.Trim();
    Nombre = nombre.Trim();
    Apellidos = apellidos.Trim();
    Telefono = telefono.Trim();
    Especialidad = string.IsNullOrWhiteSpace(especialidad) ? null : especialidad.Trim();
    FechaContratacion = fechaContratacion;
}
```

```csharp
[Fact]
public void Constructor_CuentaDeOtroRol_LanzaArgumentException()
{
    var usuario = new Usuario("ana@instituto.local", Rol.Estudiante, OrigenRegistro.Administrador);

    Assert.Throws<ArgumentException>(() =>
        new Profesor(usuario, "1-1111-1111", "Ana", "Mora", "88888888", "Redes", null));
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD): `Profesor` — rechaza una cuenta de otro rol, recorta los textos, la especialidad en blanco queda `null`, `ActualizarDatos` cambia todos los datos personales.
- **Manual**, con la API, PostgreSQL y smtp4dev en marcha, usando `Matricula.Api.http` y Scalar:

| Qué | Esperado |
|---|---|
| Registro de un profesor | `201` + `Location`; temporal en smtp4dev con el nombre del profesor; la respuesta no contiene la contraseña |
| Registro con un correo ya usado (con otra capitalización) | `409 RN-13` |
| Registro con cédula de un profesor activo / de uno inactivo | `409 RN-13` / `409 RN-39` con `profesorId` y `usuarioId` |
| Registro con la cédula de un estudiante o administrador existente y otro correo | `201` (RN-13 es por tabla) |
| Registro con datos inválidos (cédula vacía, especialidad de 101 caracteres, fecha de contratación de 1899) | `400` con el campo y el mensaje |
| Primer ingreso del profesor | Login con la temporal da `debeCambiarContrasena: true`; tras el cambio obligatorio, el token trae `profesor_id` y rol `Profesor` |
| Listar con y sin `incluirInactivos`, y con `search` por nombre, cédula, especialidad y correo | Los inactivos solo aparecen con el filtro |
| Consultar un id inexistente | `404` |
| Editar cédula a la de otro profesor / correo al de otra cuenta | `409 RN-13` |
| Editar el correo | El login funciona con el correo nuevo |
| Desactivar un profesor con sesión abierta | Su token responde `401`; su login `403 RN-29`; `GET /profesores/{id}` muestra `activo: false` |
| Reactivar | Vuelve a iniciar sesión |
| Desactivar por `/usuarios/{usuarioId}/desactivar` | `GET /profesores/{id}` muestra `activo: false` (RN-38) |
| `GET /api/v1/me` como profesor | Cuenta y datos personales del profesor, con especialidad y fecha de contratación |
| `GET /api/v1/me` como administrador y como estudiante | Igual que antes; los campos del profesor van en `null` |
| `GET /api/v1/profesores` y `POST /api/v1/profesores` con token de profesor o de estudiante | `403` |
| Logs | Ninguna contraseña ni temporal aparece en la consola |

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. Mensajes en español y comprensibles. Crear la cuenta con `CuentaService` y guardar cuenta y profesor en un solo `SaveChangesAsync`; los correos después. Normalizar el correo. Desactivar y reactivar solo a través de `UsuarioService`.
- **Preguntar antes:** añadir paquetes NuGet. Cambiar el esquema de `profesor` fuera de esta spec o tocar el de `usuario`, `administrador` o `estudiante`.
- **Nunca:** columnas `correo` o `activo` en `profesor`. Crear cuentas de profesor fuera de un endpoint de administrador (RN-20). Devolver o registrar contraseñas, temporales o hashes. Eliminar profesores en este módulo. `[Authorize(Roles = ...)]`.

## Success Criteria

1. La migración `AgregaProfesor` crea la tabla `profesor` con índices únicos en `cedula` y `usuario_id` y la FK a `usuario` sin cascada.
2. Un administrador registra profesores que reciben la temporal por correo, hacen el cambio obligatorio y entran con rol `Profesor` (RF-03, RF-22).
3. El administrador lista (con búsqueda, filtro de inactivos y paginación), consulta, edita, desactiva y reactiva profesores (RF-23 a RF-26 sin eliminación).
4. Desactivar un profesor desactiva su cuenta y viceversa, porque es el mismo estado (RN-38).
5. El profesor ve sus datos en `GET /api/v1/me` y su token lleva `profesor_id`.
6. Las verificaciones manuales de la tabla dan el resultado esperado.
7. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **Formato de los datos personales:** los de `acceso` (supuesto 6 de `SPEC-acceso.md`) para cédula, nombre, apellidos, teléfono y correo.
2. **Especialidad como texto libre** de hasta 100 caracteres (por ejemplo, "Programación", "Redes"), no un catálogo. Una sola especialidad por profesor.
3. **Fecha de contratación:** opcional, la indica el administrador (no se fija con la fecha de creación, a diferencia de `FechaRegistro` del estudiante) y se puede editar. Se acepta una fecha futura (contratación ya acordada que empieza después); solo se rechaza antes de 1900-01-01.
4. **Búsqueda del listado:** nombre, apellidos, cédula, especialidad (RF-23) y también correo, igual que en estudiantes y administradores.
5. **Perfil:** los datos del profesor se agregan como campos opcionales de `DatosPersonalesResponse`, sin cambiar la forma de `PerfilResponse`.
6. **Claim `profesor_id` en este módulo**, igual que `estudiante_id` en `estudiantes`, aunque el primero que lo usa es `grupos`. Así `grupos` solo agrega la política y la guarda de propiedad.

## Decisiones

Se aplican las decisiones 2, 4, 6, 7 y 8 del mapa, y además:

1. **La eliminación de profesores queda para `grupos`** (2026-10-28): `DELETE /profesores/{id}` no se implementa aquí, igual que la de estudiantes queda para `matriculas`. La guarda "solo si nunca tuvo grupos" solo se puede comprobar cuando existan los grupos, y entonces se decidirá qué pasa con el `Usuario` del profesor (RN-35).

2. **La verificación heredada de `fundacion` T3 sigue pendiente** (2026-10-28): pide un controller de nombre compuesto para comprobar la ruta en kebab-case, y `ProfesoresController` no lo es. La cumplirá el primer módulo que tenga uno (por ejemplo, `catalogos` o `grupos`).

## Preguntas abiertas

Ninguna.
