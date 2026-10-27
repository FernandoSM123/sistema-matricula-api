# Spec: estudiantes

Módulo `estudiantes` del [mapa de módulos](mapa-modulos.md). Depende de `acceso`.

| Estado | Plan | Tareas | Avance |
|---|---|---|---|
| **Aprobada** | [Aprobado](../tasks/estudiantes/plan.md) | [Aprobadas](../tasks/estudiantes/todo.md) | 8/9 tareas completadas |

**Historial:**
- 2026-10-27: v1 en revisión.
- 2026-10-27: v1 aprobada. El estudiante edita solo teléfono y dirección; el correo lo cambia el administrador (decisiones 1 y 2).
- 2026-10-27: plan aprobado; lista de 9 tareas en revisión.
- 2026-10-27: lista de tareas aprobada; empieza la implementación (T1).

## Objetivo

Registrar a los estudiantes del instituto, por su cuenta o por un administrador, y darle al administrador su gestión: listar, consultar, editar, desactivar y reactivar. El estudiante consulta su perfil y edita sus datos de contacto. Cubre RF-01, RF-02 y RF-15 a RF-18, la parte de desactivar y reactivar de RF-19, la consulta 1 de la sección 6 y las reglas RN-13 (cédula de estudiantes), RN-19, RN-21, RN-31, RN-38, RN-39 y RN-41.

**Usuarios:**
- Visitante: se registra como estudiante (registro público).
- Estudiante: consulta su perfil y edita sus datos de contacto.
- Administrador: registra estudiantes con una temporal, los lista, consulta, edita, desactiva y reactiva.
- El equipo de desarrollo: `matriculas` usa la entidad `Estudiante` y el claim `estudiante_id` (RN-18), y añade aquí RN-36 y la eliminación (decisión 4 del mapa).

### Alcance

1. **Entidad `Estudiante`** (§5.2 de los requerimientos; decisiones 2 y 7 del mapa).

   | Propiedad | Tipo | Notas |
   |---|---|---|
   | `Id`, `FechaCreacion`, `FechaModificacion` | de `EntidadBase` | |
   | `Cedula` | `string(20)` | Índice único en la tabla `estudiante` (RN-13, única por tabla) |
   | `Nombre` | `string(100)` | |
   | `Apellidos` | `string(100)` | |
   | `Telefono` | `string(20)` | |
   | `FechaNacimiento` | `DateOnly?` | |
   | `Direccion` | `string(250)?` | Vacía o en blanco se guarda como `null` |
   | `FechaRegistro` | `DateOnly` | Fecha de ingreso al instituto. Se fija al crear con `TimeProvider` (supuesto 3) |
   | `UsuarioId` | `int` | FK a `Usuario`, `Restrict`, índice único (1:1, RN-31) |

   Sin `Correo` ni `Activo`: se leen de su `Usuario` (decisiones 2 y 7). El constructor exige una cuenta con rol `Estudiante`, igual que `Administrador`. Métodos: `ActualizarDatos(...)` (administrador) y `ActualizarContacto(telefono, direccion)` (el propio estudiante). Migración `AgregaEstudiante`.

2. **Endpoints**

   | Método y ruta | Acceso | RF / RN | Respuesta |
   |---|---|---|---|
   | `POST /api/v1/estudiantes/registro` | Anónimo, con rate limiting | RF-01, RF-15, RN-13, RN-19, RN-21, RN-26 | `201` con un mensaje de confirmación · `409 RN-21` |
   | `POST /api/v1/estudiantes` | Administrador | RF-02, RF-05, RF-15, RN-13, RN-22, RN-39 | `201` + `Location` con el estudiante y `correoEnviado` · `409 RN-13` · `409 RN-39` |
   | `GET /api/v1/estudiantes` | Administrador | RF-16, RN-41, consulta 1 | `200` paginado; `search` (nombre, apellidos, cédula o correo) e `incluirInactivos` |
   | `GET /api/v1/estudiantes/{id}` | Administrador | RF-17 | `200` · `404` |
   | `PUT /api/v1/estudiantes/{id}` | Administrador | RF-18, RN-13, RN-31 | `200` con el estudiante · `404` · `409 RN-13` |
   | `POST /api/v1/estudiantes/{id}/desactivar` | Administrador | RF-19, RN-38 | `204` · `404` |
   | `POST /api/v1/estudiantes/{id}/reactivar` | Administrador | RF-19, RN-38, RN-39 | `204` · `404` |
   | `GET /api/v1/me` | Autenticado | RF-13 | Para un estudiante, incluye sus datos personales |
   | `PUT /api/v1/me/datos-contacto` | Estudiante | Matriz §2.2 | `200` con el perfil · `403` si no es estudiante |

   La respuesta de un estudiante incluye `id`, `usuarioId`, `cedula`, `nombre`, `apellidos`, `telefono`, `correo`, `fechaNacimiento`, `direccion`, `fechaRegistro`, `activo`, `fechaCreacion` y `ultimoIngreso`. Con `usuarioId` también funcionan `/api/v1/usuarios/{id}/reenviar-temporal`, `desactivar` y `reactivar` de `acceso`.

   Detalle de los flujos:
   - **Registro público:** `{ cedula, nombre, apellidos, telefono, correo, fechaNacimiento?, direccion?, contrasena, confirmacion }`. La contraseña cumple RN-26 y coincide con la confirmación (`400`). No acepta un rol: el DTO no lo tiene y siempre crea rol `Estudiante` (RN-19) con `CuentaService.CrearCuentaPublicaAsync`. Cuenta y estudiante en un solo `SaveChangesAsync`. No inicia sesión ni envía correo (supuesto 4).
   - **RN-21:** si la cédula pertenece a un estudiante (activo o inactivo) o el correo a cualquier cuenta, `409` con `code: "RN-21"` y un mensaje que indica iniciar sesión, solicitar una temporal por olvido o contactar a la administración. No revela si el registro existente está inactivo ni su id.
   - **Registro por administrador:** `{ cedula, nombre, apellidos, telefono, correo, fechaNacimiento?, direccion? }`. Usa `CuentaService.CrearCuentaConTemporalAsync` (rol `Estudiante`, temporal de 72 horas), guarda y después envía la temporal. Correo ya usado → `409 RN-13`. Cédula de un estudiante activo → `409 RN-13`; de uno inactivo → `409 RN-39` con un mensaje que ofrece reactivarlo e incluye su `estudianteId` y `usuarioId`.
   - **Editar (administrador):** los mismos datos que el registro por administrador. Si cambia el correo, se actualiza el `Usuario` con `CuentaService.CambiarCorreoAsync` (RN-31). Cédula de otro estudiante o correo de otra cuenta → `409 RN-13`.
   - **Desactivar y reactivar:** resuelven el `usuarioId` del estudiante y llaman a `UsuarioService.DesactivarAsync` / `ReactivarAsync` de `acceso`. Es la misma operación que `/usuarios/{id}/desactivar` (decisión 2), así que RN-38 se cumple sin sincronizar nada y la guarda RN-36 que añada `matriculas` en `UsuarioService` aplica a las dos rutas.
   - **Datos de contacto propios:** `{ telefono, direccion? }`. El estudiante se identifica por el claim `estudiante_id`; otro rol recibe `403`. No cambia cédula, nombre, apellidos, fecha de nacimiento ni correo (decisiones 1 y 2).

3. **Perfil y token** (extiende lo de `acceso`)
   - `GET /api/v1/me`: `DatosPersonalesResponse` incorpora `fechaNacimiento`, `direccion` y `fechaRegistro`, opcionales (nulos para administradores). `AuthService.ObtenerPerfilAsync` toma los datos personales del administrador o del estudiante asociado a la cuenta.
   - Claim `estudiante_id`: `DatosToken` incorpora `EstudianteId?`; `AuthService` lo busca al emitir cualquier token de una cuenta con rol `Estudiante` y `GeneradorToken` lo agrega. `ICurrentUser.EstudianteId` ya lo lee.
   - `EnvioCorreos.ObtenerNombreAsync` también busca el nombre en `Estudiantes`, para que la temporal y los avisos saluden por su nombre.

4. **Rate limiting** del registro público: política `RegistroEstudiante`, 10 peticiones cada 15 minutos por IP (supuesto 5). Al superarlo, `429`.

5. **Validación**: los validadores reutilizan `DatosPersonalesRules` (cédula, nombre, teléfono, correo) y las reglas de contraseña de `acceso` (RN-26 y confirmación). Nuevas reglas: `Direccion` hasta 250 caracteres; `FechaNacimiento` no futura y no anterior a 1900-01-01 (con `TimeProvider`).

6. **Verificación heredada de `acceso` T10**: un endpoint de administrador con el token de un estudiante responde `403`.

7. **Documentación**: `Matricula.Api.http` con los flujos del módulo y el paso de la migración en el README si cambia algo.

### Fuera de alcance

- **Eliminar un estudiante** (RF-19, "solo si no tiene matrículas") y la guarda **RN-36** al desactivar: los añade `matriculas` (decisión 4 del mapa). Al hacerlo habrá que resolver qué pasa con su `Usuario`, porque RN-35 prohíbe eliminar cuentas.
- RF-20, RF-21 y RF-54 (cursos, horario e historial del estudiante) y `/api/v1/me/matriculas`, `/me/horario`: `matriculas`.
- Cambio de rol: no existe (decisión 8).
- Iniciar sesión automáticamente tras el registro público, verificación del correo y correo de bienvenida.
- Pruebas de integración: diferidas.

## Tech Stack

.NET 10, ASP.NET Core Controllers, EF Core 10 + Npgsql 10.0.3, FluentValidation 12.2, MailKit 4.18.1, xUnit 2.9.3. Rate limiting y JWT ya configurados en `acceso`. **No se añaden paquetes.**

## Commands

```bash
dotnet build                               # warnings = errores
dotnet test tests/Matricula.UnitTests      # pruebas unitarias, sin BD
dotnet ef migrations add AgregaEstudiante --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
smtp4dev --smtpport=2525 --urls=http://localhost:5080
dotnet run --project src/Matricula.Api     # Scalar: http://localhost:5022/scalar
```

## Project Structure

```
src/Matricula.Domain/
  Entities/Estudiante.cs                     nuevo
src/Matricula.Application/
  Abstractions/IAppDbContext.cs              + DbSet<Estudiante>
  Abstractions/DatosToken.cs                 + EstudianteId
  Estudiantes/                               EstudianteService, DTOs, validadores, proyecciones
  Common/Validacion/DatosPersonalesRules.cs  + Direccion, FechaNacimiento
  Auth/AuthService.cs                        + datos del estudiante en el perfil, estudiante_id al emitir el token
  Auth/DatosPersonalesResponse.cs            + campos opcionales del estudiante
  Correos/EnvioCorreos.cs                    + nombre de estudiantes
  DependencyInjection.cs                     + EstudianteService
src/Matricula.Infrastructure/
  Persistence/Configurations/EstudianteConfiguration.cs
  Persistence/Migrations/                    AgregaEstudiante
  Persistence/AppDbContext.cs                + DbSet<Estudiante>
src/Matricula.Api/
  Controllers/EstudiantesController.cs       nuevo
  Controllers/MeController.cs                + PUT datos-contacto
  Seguridad/GeneradorToken.cs                + claim estudiante_id
  Seguridad/LimitesPeticiones.cs             + RegistroEstudiante
  Seguridad/Politicas.cs                     + Estudiante (rol + sin cambio pendiente)
  Matricula.Api.http
tests/Matricula.UnitTests/
  Domain/EstudianteTests.cs
```

## Code Style

Igual que `acceso`: constructores primarios, `ArgumentNullException.ThrowIfNull`, nombres de dominio en español y sufijos técnicos en inglés, un archivo por clase, pruebas `Metodo_Escenario_Resultado`, proyecciones con `Select` y `AsNoTracking` en las lecturas. Las políticas de rol con `[Authorize(Policy = Politicas.X)]`, nunca `Roles` (RN-24).

```csharp
public void ActualizarContacto(string telefono, string? direccion)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(telefono);

    Telefono = telefono.Trim();
    Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
}
```

```csharp
[Fact]
public void Constructor_CuentaDeOtroRol_LanzaArgumentException()
{
    var usuario = new Usuario("profe@instituto.local", Rol.Profesor, OrigenRegistro.Administrador);

    Assert.Throws<ArgumentException>(() =>
        new Estudiante(usuario, "1-1111-1111", "Ana", "Mora", "88888888", null, null, new DateOnly(2026, 10, 27)));
}
```

## Testing Strategy

- **Unitarias** (`tests/Matricula.UnitTests`, sin BD): `Estudiante` — rechaza una cuenta de otro rol, recorta los textos, la dirección en blanco queda `null`, `ActualizarContacto` solo cambia teléfono y dirección, `ActualizarDatos` cambia los datos personales.
- **Manual**, con la API, PostgreSQL y smtp4dev en marcha, usando `Matricula.Api.http` y Scalar:

| Qué | Esperado |
|---|---|
| Registro público válido | `201`; login con esa contraseña da `debeCambiarContrasena: false` y rol `Estudiante`; el token trae `estudiante_id` |
| Registro público con un campo `rol: "Administrador"` | Se ignora; la cuenta es `Estudiante` (RN-19) |
| Registro público con contraseña débil o confirmación distinta | `400` |
| Registro público con cédula o correo existentes (correo con otra capitalización) | `409 RN-21` con el mensaje de iniciar sesión, olvido o administración |
| 11 registros públicos en 15 minutos desde la misma IP | El último responde `429` |
| Registro por administrador | `201` + `Location`; temporal en smtp4dev con el nombre del estudiante; la respuesta no contiene la contraseña |
| Registro por administrador con cédula de un activo / de un inactivo | `409 RN-13` / `409 RN-39` con `estudianteId` y `usuarioId` |
| Primer ingreso del estudiante registrado por el administrador | Cambio obligatorio y luego acceso normal |
| Listar con y sin `incluirInactivos`, y con `search` por nombre, cédula y correo | Los inactivos solo aparecen con el filtro |
| Consultar un id inexistente | `404` |
| Editar cédula a la de otro estudiante / correo al de otra cuenta | `409 RN-13` |
| Editar el correo | El login funciona con el correo nuevo |
| Desactivar un estudiante con sesión abierta | Su token responde `401`; su login `403 RN-29`; `GET /estudiantes/{id}` muestra `activo: false` |
| Reactivar | Vuelve a iniciar sesión |
| `GET /api/v1/me` como estudiante | Cuenta y datos personales del estudiante |
| `PUT /api/v1/me/datos-contacto` como estudiante / como administrador | `200` con el perfil actualizado / `403` |
| `POST /api/v1/administradores` con token de estudiante (heredada de `acceso`) | `403`, no `401` ni `200` |
| `GET /api/v1/estudiantes` con token de estudiante | `403` |
| Logs | Ninguna contraseña ni temporal aparece en la consola |

## Boundaries

- **Siempre:** `dotnet build` sin warnings y pruebas unitarias en verde. Mensajes en español y comprensibles. Crear la cuenta con `CuentaService` y guardar cuenta y estudiante en un solo `SaveChangesAsync`; los correos después. Normalizar el correo. Desactivar y reactivar solo a través de `UsuarioService`.
- **Preguntar antes:** añadir paquetes NuGet. Cambiar el esquema de `estudiante` fuera de esta spec o tocar el de `usuario`/`administrador`. Cambiar los límites del rate limiting una vez aprobados.
- **Nunca:** columnas `correo` o `activo` en `estudiante`. Aceptar un rol en el registro público. Devolver o registrar contraseñas, temporales o hashes. Eliminar estudiantes en este módulo. `[Authorize(Roles = ...)]`.

## Success Criteria

1. La migración `AgregaEstudiante` crea la tabla `estudiante` con índices únicos en `cedula` y `usuario_id` y la FK a `usuario` sin cascada.
2. Un visitante se registra y entra como estudiante con su contraseña; un administrador registra estudiantes que reciben la temporal por correo (RF-01, RF-02).
3. El administrador lista (con búsqueda, filtro de inactivos y paginación), consulta, edita, desactiva y reactiva estudiantes (RF-16 a RF-19 sin eliminación).
4. Desactivar un estudiante desactiva su cuenta y viceversa, porque es el mismo estado (RN-38).
5. El estudiante ve sus datos en `GET /api/v1/me` y edita solo sus datos de contacto; su token lleva `estudiante_id`.
6. Las verificaciones manuales de la tabla, incluida la heredada de `acceso`, dan el resultado esperado.
7. `dotnet build` sin warnings y `dotnet test tests/Matricula.UnitTests` en verde.

## Supuestos

Se aplican salvo que se corrijan en la revisión:

1. **Ruta del registro público:** `POST /api/v1/estudiantes/registro` (anónimo), separada de `POST /api/v1/estudiantes` (administrador), con DTOs distintos.
2. **El registro público pide confirmación de la contraseña**, como el cambio obligatorio, aunque RF-01 solo dice "define su contraseña".
3. **`FechaRegistro` es la fecha de creación** (hoy, según `TimeProvider`) y no se edita.
4. **El registro público no inicia sesión ni envía correo**: responde `201` con un mensaje y la persona inicia sesión con `/auth/login`. Sin `Location`, porque el visitante no puede consultar el recurso.
5. **Rate limiting del registro público:** 10 cada 15 minutos por IP (más holgado que el olvido, por si varios estudiantes se registran desde la red del instituto).
6. **RN-21 vs RN-39 en el registro público:** cualquier cédula o correo existente responde `RN-21`, aunque el estudiante esté inactivo; solo el administrador reactiva (RN-39) y el mensaje lo remite a la administración.
7. **Formato de los datos personales:** los de `acceso` (supuesto 6 de `SPEC-acceso.md`), más dirección de hasta 250 caracteres y fecha de nacimiento no futura ni anterior a 1900. Sin edad mínima.
8. **Búsqueda del listado:** nombre, apellidos, cédula y también correo (RF-16 pide nombre o cédula; el correo ayuda al administrador y es lo mismo que en administradores).
9. **Perfil:** los datos del estudiante se agregan como campos opcionales de `DatosPersonalesResponse`, sin cambiar la forma de `PerfilResponse`.

## Decisiones

Se aplican las decisiones 2, 4, 6, 7 y 8 del mapa, y además:

1. **El estudiante no cambia su correo** (2026-10-27): el correo es su usuario de acceso y el canal de las temporales, y el sistema no verifica correos. Solo lo cambia el administrador (RF-18).

2. **El estudiante edita solo sus datos de contacto** (2026-10-27): teléfono y dirección, como dice la matriz §2.2. La cédula, el nombre, los apellidos y la fecha de nacimiento solo los cambia el administrador.

## Preguntas abiertas

Ninguna.
