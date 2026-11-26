# API — Sistema de Matrícula

API REST en .NET 10 + EF Core para el sistema de matrícula. Los requerimientos están en `docs/requerimientos.md` y las pautas de arquitectura en `docs/arquitectura.md`.

## Estructura

```
src/
  Matricula.Api             Controllers, middleware, Program.cs, configuración
  Matricula.Application     Servicios (casos de uso), DTOs, validadores, interfaces
  Matricula.Domain          Entidades, enums y reglas de negocio puras
  Matricula.Infrastructure  EF Core (AppDbContext, configuraciones, migraciones), correo, seeds (admin y datos de prueba)
tests/
  Matricula.UnitTests
  Matricula.IntegrationTests
```

Dependencias: `Api → Application → Domain`, `Infrastructure → Application`. `Api` referencia `Infrastructure` solo para registrar dependencias.

## Requisitos

- .NET SDK 10
- PostgreSQL 17 instalado localmente (puerto 5432)
- Herramienta de EF: `dotnet tool install --global dotnet-ef`
- smtp4dev para ver los correos en desarrollo: `dotnet tool install --global Rnwood.Smtp4dev`

## Levantar el entorno

En Windows, `dev.cmd` (en la raíz de `API`) abre dos ventanas: una con smtp4dev y otra con la API en `dotnet watch`, que muestra las URLs disponibles. Puedes ejecutarlo con doble clic o con `.\dev.cmd` desde la terminal. Con `dotnet watch`, los cambios se aplican al guardar. Si un cambio necesita reiniciar la API, se reinicia sola, y con `Ctrl+R` puedes forzar el reinicio.

Antes de la primera ejecución, y cada vez que haya migraciones nuevas, actualiza la base de datos con el comando `dotnet ef database update` de abajo.

Paso a paso, sin el script:

```bash
smtp4dev --smtpport=2525 --urls=http://localhost:5080   # en otra terminal; bandeja web en http://localhost:5080
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api
```

- Documentación interactiva (Scalar): http://localhost:5022/scalar
- Health check: http://localhost:5022/health

## Configuración y credenciales

Los archivos `appsettings.json` y `appsettings.Development.json` solo guardan la **estructura** de la configuración, con valores por defecto para desarrollo (`Username=postgres;Password=postgres`, los mismos del `docker-compose.yml`). Si tu PostgreSQL usa esas credenciales, no necesitas configurar nada más. Las credenciales reales nunca se suben al repositorio:

- **Desarrollo:** [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) de .NET, guardados fuera del proyecto, en tu perfil de usuario.
- **Otros entornos:** variables de entorno (`ConnectionStrings__Matricula`, `Smtp__Host`, …).

En Development, los user-secrets se cargan después de los `appsettings*.json`, así que sobrescriben los valores por defecto sin cambiar el código.

### Configurar user-secrets (una sola vez por equipo)

1. Desde la carpeta `API`, inicializa los secrets del proyecto (si el `.csproj` ya tiene un `<UserSecretsId>`, omite este paso):

   ```powershell
   dotnet user-secrets init --project src/Matricula.Api
   ```

2. Crea el archivo `secrets.json` en:

   ```
   %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
   ```

   `<UserSecretsId>` es el GUID que aparece en `src/Matricula.Api/Matricula.Api.csproj`. Si la carpeta no existe, créala. En Visual Studio también puedes abrirlo con clic derecho en el proyecto → *Manage User Secrets*.

3. Copia `secrets.example.json` (en la raíz) como tu `secrets.json` y reemplaza los valores de ejemplo. La plantilla tiene todos los secretos que usa la API, con comentarios. Si solo necesitas la conexión, basta con esto:

   ```json
   {
     "ConnectionStrings": {
       "Matricula": "Host=localhost;Port=5432;Database=matricula;Username=postgres;Password=\"TU_CONTRASEÑA\""
     }
   }
   ```

   Si la contraseña contiene `;`, `"` o `'`, enciérrala entre comillas dobles escapadas (`\"...\"`) como en el ejemplo.

   Con valores sin caracteres especiales también puedes usar el comando `dotnet user-secrets set "ConnectionStrings:Matricula" "<cadena>" --project src/Matricula.Api`. En PowerShell, las comillas dentro del valor se pierden, así que en ese caso edita el archivo a mano.

4. Comprueba lo que quedó guardado:

   ```powershell
   dotnet user-secrets list --project src/Matricula.Api
   ```

Para quitar un valor, usa `dotnet user-secrets remove "<clave>" --project src/Matricula.Api`. Para borrarlos todos, usa `dotnet user-secrets clear --project src/Matricula.Api`.

> El `secrets.json` se guarda en texto plano, sin cifrar. Solo sirve para que las credenciales no terminen en el repositorio. No usar en producción.

### Todas las claves

Como variable de entorno, cada clave usa `__` en lugar de `:` (por ejemplo `Jwt__Key`). Las marcadas como **secreto** van solo en user-secrets o en variables de entorno, nunca en `appsettings*.json`.

> **Si ya tenías user-secrets de antes de la corrección `nombres`**, renombra las claves en tu `secrets.json` (`%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`, o *Manage User Secrets* en Visual Studio): `Jwt:Clave` → `Jwt:Key`, `AdminSeed:ContrasenaTemporal` → `AdminSeed:TemporaryPassword`, `Smtp:Usuario`/`Contrasena` → `Smtp:User`/`Password` y `DatosPrueba:Habilitado`/`Contrasena`/`Semilla` → `TestData:Enabled`/`Password`/`Seed`. Sin `Jwt:Key` la API no arranca y el error lo dice; en cambio, las claves viejas de `DatosPrueba` se ignoran **sin aviso** y la siembra no se ejecuta.

| Clave | Secreto | Por defecto | Para qué |
|---|---|---|---|
| `ConnectionStrings:Matricula` | **Sí** | En Development, `postgres`/`postgres` en `localhost:5432` | Conexión a PostgreSQL. Sin ella, la API no arranca |
| `Jwt:Key` | **Sí** | — | Firma de los tokens. Obligatoria, de 32 caracteres o más |
| `Jwt:Issuer`, `Jwt:Audience` | No | `matricula-api`, `matricula-app` | Emisor y audiencia de los tokens |
| `Jwt:AccessTokenMinutes` | No | `15` | Vigencia del token de acceso (RN-50) |
| `Jwt:RefreshTokenMinutes` | No | `480` | Duración máxima de la sesión desde el ingreso; renovarla no la extiende (RF-60, RN-50). Debe ser mayor que `AccessTokenMinutes` |
| `AdminSeed:TemporaryPassword` | **Sí** | — | Temporal del administrador inicial. Obligatoria mientras no haya administradores |
| `AdminSeed:Correo`, `Cedula`, `Nombre`, `Apellidos`, `Telefono` | No | Datos genéricos (`admin@instituto.local`, John Doe) | Datos del administrador inicial |
| `Smtp:Host`, `Smtp:Port`, `Smtp:UseSsl` | No | En Development, `localhost:2525` (smtp4dev), sin SSL | Servidor de correo |
| `Smtp:User`, `Smtp:Password` | **Sí** | Vacíos | Solo si el servidor SMTP pide autenticación |
| `Smtp:SenderEmail`, `Smtp:SenderName` | No | `no-responder@instituto.local`, `Sistema de Matrícula` | Remitente de los correos |
| `Cors:AllowedOrigins` | No | En Development, `http://localhost:5173` | Orígenes de los frontends |
| `Institute:TimeZone` | No | `America/Costa_Rica` | Zona de la fecha de hoy |
| `ErrorLog:RetentionDays` | No | `90` | Días que se guarda el registro de errores |
| `TestData:Enabled` | No | `false` | Activa la siembra de datos de prueba (solo Development) |
| `TestData:Password` | **Sí** | — | Contraseña de las cuentas de prueba y del administrador inicial |
| `TestData:Seed`, `Cursos`, `Aulas`, `Profesores`, `Estudiantes`, `GruposPerPeriodo`, `MaxMatriculasPerEstudiante` | No | Ver "Datos de prueba" | Cantidades de la siembra |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` con `dotnet run` | Entorno. User-secrets y datos de prueba solo funcionan en `Development` |

### CORS

La sección `Cors:AllowedOrigins` indica desde qué orígenes (URL del frontend) el navegador puede llamar a la API:

| Archivo | Valor |
|---|---|
| `appsettings.json` | `[]`: no se permite ningún origen (valor seguro por defecto) |
| `appsettings.Development.json` | `["http://localhost:5173"]` (provisional, hasta definir el puerto del frontend) |

Para cambiarlo en desarrollo, edita `appsettings.Development.json`. En otros entornos, usa variables de entorno con un índice por origen:

```powershell
$env:Cors__AllowedOrigins__0 = "https://matricula.ejemplo.com"
$env:Cors__AllowedOrigins__1 = "https://admin.ejemplo.com"
```

La lista se lee al arrancar, así que hay que reiniciar la API después de cambiarla. CORS solo afecta a los navegadores; herramientas como `curl` o Scalar no lo necesitan.

La política también expone dos encabezados de respuesta que el navegador no deja leer desde otro origen si no se declaran (`Access-Control-Expose-Headers`):
- `Retry-After`: los segundos de espera de un `429`, para que el frontend muestre cuánto falta y deshabilite el envío ese tiempo.
- `Location`: la ruta del recurso creado en un `201`.

Ver [COR-02](correcciones/COR-02-encabezados-cors.md).

### Institute (zona horaria)

La opción `Institute:TimeZone` define qué día es "hoy" para el sistema. Está en `appsettings.json` con el valor `America/Costa_Rica`. Es un identificador [IANA](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones) y no es un secreto.

```json
"Institute": {
  "TimeZone": "America/Costa_Rica"
}
```

**Por qué existe:** el periodo vigente, las fechas de matrícula y retiro (RN-04, RN-10), la fecha de registro del estudiante y la validación de la fecha de nacimiento comparan fechas sin hora. Con la fecha UTC, desde las 18:00 en Costa Rica el sistema ya estaría en el día siguiente y, por ejemplo, cerraría la matrícula seis horas antes de tiempo.

**Cómo se usa en el código:** `ILocalClock.Today()` es el único punto que calcula la fecha de hoy. Las marcas de tiempo (`FechaCreacion`, vencimientos de temporales, tokens) siguen en UTC con `TimeProvider`.

Para cambiarla, edita `appsettings.json` o, en otros entornos, usa una variable de entorno y reinicia la API:

```powershell
$env:Institute__TimeZone = "America/Costa_Rica"
```

Si la opción falta o no es una zona válida, **la API no arranca** y lo indica en el error. Esto también ocurre si el entorno no tiene datos de zonas horarias, como algunas imágenes mínimas de Docker: hay que instalarlos (`tzdata`) y no activar la globalización invariante.

### Jwt

La API emite tokens JWT firmados con una clave simétrica (HS256), y junto a ellos un refresh token para renovar la sesión (`POST /api/v1/auth/refresh`). `Issuer`, `Audience`, `AccessTokenMinutes` (15) y `RefreshTokenMinutes` (480) están en `appsettings.json`; para probar los vencimientos se pueden bajar en user-secrets (por ejemplo, 1 y 3). la **clave** va solo en user-secrets o en la variable de entorno `Jwt__Key`. Sin una clave de al menos 32 caracteres, **la API no arranca** y lo indica en el error.

Para generar una clave aleatoria en PowerShell:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

y agrégala a tu `secrets.json`:

```json
"Jwt": { "Key": "<la clave generada>" }
```

Cambiar la clave invalida todos los tokens emitidos (hay que volver a iniciar sesión).

### AdminSeed (primer administrador)

Al arrancar, si no existe ningún administrador, la API crea uno con los datos genéricos de `appsettings.json` (`admin@instituto.local`, John Doe, cédula `000000000`) y la contraseña temporal de `AdminSeed:TemporaryPassword`, que va solo en user-secrets o en `AdminSeed__TemporaryPassword`:

```json
"AdminSeed": { "TemporaryPassword": "TuTemporal123" }
```

- La temporal debe cumplir la política de contraseñas: al menos 8 caracteres, una mayúscula y un número.
- No se envía por correo: es la que configuras.
- Si no hay administradores y falta la temporal, la API no arranca y lo indica.
- Mientras el administrador inicial no defina su contraseña, cada arranque renueva su temporal desde la configuración (vence a las 72 horas). Después de definirla, `TemporaryPassword` deja de usarse.

**Primer ingreso:**

1. Aplica las migraciones y arranca la API (ver arriba).
2. `POST /api/v1/auth/login` con `admin@instituto.local` y la temporal → la respuesta trae `debeCambiarContrasena: true`. Ese token solo sirve para el cambio obligatorio y el logout.
3. `POST /api/v1/auth/cambio-obligatorio` con la nueva contraseña y su confirmación → devuelve un token normal.
4. Con ese token, `PUT /api/v1/administradores/1` reemplaza los datos genéricos por los reales.

En Scalar, pega el `accessToken` en la autenticación Bearer. `src/Matricula.Api/Matricula.Api.http` tiene el recorrido completo.

### Datos de prueba (solo desarrollo)

Para no crear datos a mano, la API puede sembrar al arrancar cursos, aulas, tres periodos (pasado, vigente y futuro), profesores, estudiantes, grupos con horarios y matrículas. Los datos se generan con [Bogus](https://github.com/bchavez/Bogus) y cumplen las reglas del sistema: sin traslapes, con cupos consistentes y con grupos y matrículas en todos sus estados. El detalle está en `specs/SPEC-datos-prueba.md`.

**Activarlo** en user-secrets. La contraseña de las cuentas de prueba debe cumplir la política (8 caracteres, una mayúscula y un número):

```json
"TestData": { "Enabled": true, "Password": "TuPrueba123" }
```

La siembra solo se ejecuta si se cumplen las tres condiciones:

- `TestData:Enabled` es `true`;
- el entorno es `Development` (en otro entorno escribe una advertencia y no hace nada);
- la BD no tiene datos de negocio (con datos, escribe "La BD ya tiene datos" y no hace nada).

Si la configuración no es válida, por ejemplo sin `Password` o con una cantidad fuera de rango, la API no arranca y el error nombra la clave. La siembra no envía correos y nunca escribe la contraseña en el log.

**Empezar de cero** (borra todos los datos de la BD de desarrollo):

```bash
dotnet ef database drop --force --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
dotnet run --project src/Matricula.Api
```

Al arrancar, el log muestra un resumen con la semilla usada, los totales y los grupos y matrículas por estado.

**Cuentas** (todas con `TestData:Password`):

| Correo | Caso |
|---|---|
| `admin@instituto.local` | El administrador inicial. La siembra le asigna la contraseña de prueba como definitiva, así que no pide el cambio obligatorio |
| `profesor1@prueba.test` | Profesor con grupos en los periodos pasado y vigente |
| `estudiante1@prueba.test` | Estudiante con matrículas en los periodos pasado y vigente |
| `profesor2@prueba.test`, `estudiante2@prueba.test` | Con cambio obligatorio pendiente: la contraseña es una temporal que vence a las 72 horas. Para renovarla, se vuelve a sembrar |
| `nombre.apellido.N@prueba.test` | El resto. Alrededor de un 10 % de los estudiantes y un profesor están desactivados |

**Cantidades** (en `appsettings.json`, se cambian en user-secrets o en `appsettings.Development.json`):

| Clave | Por defecto | Rango |
|---|---|---|
| `Seed` | `null` (datos distintos en cada siembra) | Un número reproduce siempre los mismos datos |
| `Cursos` | 8 | 1–200 |
| `Aulas` | 5 | 1–100 |
| `Profesores` | 6 | 1–200 |
| `Estudiantes` | 30 | 0–5000 |
| `GruposPerPeriodo` | 5 | 0–200 |
| `MaxMatriculasPerEstudiante` | 2 | 0–6 |

Si los profesores o las aulas no alcanzan para todos los grupos sin traslapes, se crean menos grupos y el log lo advierte. Con los valores por defecto, la siembra tarda alrededor de 1 segundo.

## Endpoints sin sesión y límites de peticiones

Todo endpoint exige un token, salvo estos, que tienen un límite de peticiones por IP. Al superarlo responden `429` con `Retry-After`. Los contadores viven en memoria y se reinician al reiniciar la API.

| Endpoint | Límite |
|---|---|
| `POST /api/v1/auth/login` | 10 por minuto |
| `POST /api/v1/auth/olvido-contrasena` | 5 cada 15 minutos |
| `POST /api/v1/estudiantes/registro` (registro público de estudiantes) | 10 cada 15 minutos |

También están abiertos, sin límite, las consultas públicas de solo lectura `GET /api/v1/oferta-academica` y `GET /api/v1/periodos/vigente` (COR-01), además de `/health`, OpenAPI y Scalar.

## Migraciones

```bash
dotnet ef migrations add <Nombre> --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
```

## Pruebas

```bash
dotnet test tests/Matricula.UnitTests
```

Por ahora solo hay pruebas unitarias de las reglas del dominio, que no usan base de datos ni necesitan configuración. Los endpoints se prueban manualmente con Scalar (http://localhost:5022/scalar) o con `src/Matricula.Api/Matricula.Api.http`, y los correos se revisan en smtp4dev (http://localhost:5080).

Las pruebas de integración están diferidas. Cuando se retomen, usarán una BD aparte, `matricula_tests`, en el mismo PostgreSQL local, recreada con las migraciones en cada ejecución, y nunca la BD de desarrollo.

## Docker (opcional)

`docker-compose.yml` levanta PostgreSQL (:5432) y smtp4dev (:2525, web :5080) con `docker compose up -d`, como alternativa a instalarlos localmente. No es necesario para desarrollar.
