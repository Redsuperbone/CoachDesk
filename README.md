# Coach Desk

Aplicación de escritorio WPF para el módulo del coach de Manríquez Gym.

## Estructura

```
src/CoachDesk.Desktop/
├── Commands/       # Implementaciones ICommand reutilizables
├── Configuration/  # Opciones configurables, por ejemplo la URL de la API
├── Converters/     # Conversores para enlaces XAML
├── Helpers/        # Utilidades transversales
├── Models/         # Entidades de dominio y DTOs de la API
├── Services/       # Contratos e implementaciones de servicios HTTP
├── ViewModels/     # Estado y lógica de presentación (MVVM)
├── Views/          # Vistas XAML y controles de usuario
└── Resources/      # Estilos, plantillas y recursos visuales
```

## Backend REST y MySQL

La solución contiene `src/CoachDesk.Api`, una API ASP.NET Core 8 que centraliza autenticación JWT, asesorados, expediente antropométrico, planes nutricionales y horarios publicados. La app WPF se conecta a `https://localhost:7001/api/` de forma predeterminada.

La base de datos se describe en la migración versionada de Entity Framework Core ubicada en `src/CoachDesk.Api/Data/Migrations`. Nunca subas contraseñas ni claves JWT reales al repositorio. Para desarrollo local, crea `src/CoachDesk.Api/appsettings.Development.json` (está ignorado por Git):

```json
{
  "ConnectionStrings": {
    "CoachDesk": "Server=localhost;Port=3306;Database=coachdesk;User=TU_USUARIO;Password=TU_CONTRASENA;SslMode=Disabled;"
  },
  "Jwt": {
    "Key": "una-clave-secreta-larga-de-al-menos-32-caracteres"
  },
  "Seed": {
    "CoachFullName": "Tu nombre",
    "CoachEmail": "coach@manriquezgym.com",
    "CoachPassword": "Una-contrasena-inicial-segura"
  }
}
```

Al iniciar la API se aplican las migraciones pendientes y, solo si no existe ya ese correo, se crea el primer Coach con la configuración `Seed`. En la nube utiliza las variables de entorno equivalentes (`ConnectionStrings__CoachDesk`, `Jwt__Key`, `Seed__CoachEmail`, `Seed__CoachPassword`) en vez del archivo local.

Para iniciar el backend:

```powershell
dotnet run --project src/CoachDesk.Api/CoachDesk.Api.csproj
```

Swagger estará disponible en `https://localhost:7001/swagger` durante el desarrollo.

### Primera ejecución local con MySQL

El proyecto incluye [create-local-development-database.sql](database/create-local-development-database.sql) para crear una base aislada llamada `coachdesk_dev` y el usuario limitado `coachdesk_app`. Antes de ejecutarlo, reemplaza la contraseña de ejemplo por una contraseña local segura y usa exactamente la misma en la configuración siguiente.

1. Copia `src/CoachDesk.Api/appsettings.Development.example.json` como `src/CoachDesk.Api/appsettings.Development.json`. Para MySQL local, conserva `SslMode=Disabled` en la cadena de conexión.
2. Sustituye los tres valores de ejemplo: la contraseña de MySQL, `Jwt:Key` y `Seed:CoachPassword`.
3. Ejecuta el SQL con una cuenta administradora de tu MySQL local. En tu instalación, el cliente está normalmente en `C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe`.
4. Inicia la API. Esta aplicará la migración inicial y generará, una sola vez, el Coach y los tres asesorados de demostración porque `Seed:DemoData` está en `true`.
5. Ejecuta la aplicación WPF e inicia sesión con el correo y la contraseña configurados en `Seed`.

El archivo `appsettings.Development.json` está ignorado por Git; úsalo solamente para desarrollo local.

La interfaz no accede directamente a MySQL. Toda operación de datos pasa por servicios que consumen la API REST con `HttpClient`.

La capa REST incluye `ApiService`, manejo tipado de errores, una sesión de autenticación en memoria, y servicios para login y consulta de asesorados activos. Los endpoints iniciales son `auth/login` y `coach/advisees?active=true`; ajústalos si el contrato final del backend usa rutas distintas.

## Guía visual

Las vistas mantienen la identidad visual de Coach Desk: fondo `#080D16`, navegación superior y lateral `#1D2B3E`, tarjetas `#1C2A3D`, campos `#0E1729` y acciones principales en cian `#3AB8F2`. Reutiliza los recursos de `Resources/Colors.xaml` y `Resources/Styles.xaml` para cualquier módulo nuevo.

## Requisitos de desarrollo

- Visual Studio 2022 con la carga de trabajo **Desarrollo de escritorio con .NET**.
- .NET SDK 8 LTS o una versión SDK posterior compatible.

## Ejecutar

```powershell
dotnet restore CoachDesk.sln
dotnet build CoachDesk.sln
dotnet run --project src/CoachDesk.Desktop/CoachDesk.Desktop.csproj
```
