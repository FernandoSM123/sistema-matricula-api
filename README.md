# API — Sistema de Matrícula

API REST en .NET 10 + EF Core para el sistema de matrícula. Los requerimientos están en `docs/requerimientos.md` y las pautas de arquitectura en `docs/arquitectura.md`.

## Estructura

```
src/
  Matricula.Api             Controllers, middleware, Program.cs, configuración
  Matricula.Application     Servicios (casos de uso), DTOs, validadores, interfaces
  Matricula.Domain          Entidades, enums y reglas de negocio puras
  Matricula.Infrastructure  EF Core (AppDbContext, configuraciones, migraciones), correo
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

3. Copia la estructura de `appsettings.Development.json` con tus valores reales:

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

## Migraciones

```bash
dotnet ef migrations add <Nombre> --project src/Matricula.Infrastructure --startup-project src/Matricula.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Matricula.Infrastructure --startup-project src/Matricula.Api
```

## Pruebas

```bash
dotnet test
```

Las pruebas de integración que usan base de datos trabajan sobre una BD aparte, `matricula_tests`, en el mismo PostgreSQL local. Se recrea con las migraciones al iniciar la ejecución, así que nunca apuntes a la BD de desarrollo. La cadena de conexión se toma de la variable de entorno `ConnectionStrings__MatriculaTests`.

## Docker (opcional)

`docker-compose.yml` levanta PostgreSQL (:5432) y smtp4dev (:2525, web :5080) con `docker compose up -d`, como alternativa a instalarlos localmente. No es necesario para desarrollar.
