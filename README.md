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

## Backend REST, PostgreSQL y Supabase

La solución contiene `src/CoachDesk.Api`, una API ASP.NET Core 8 que media entre la aplicación WPF y Supabase. El inicio de sesión se valida con Supabase Auth; las consultas y operaciones se envían a PostgREST/RPC con el Bearer token recibido, por lo que las políticas RLS de Supabase continúan aplicándose. La app WPF se conecta a `https://localhost:7001/api/` de forma predeterminada.

El esquema de la base de datos es el archivo SQL administrado en Supabase. La API no ejecuta migraciones de Entity Framework, no crea tablas y no inserta datos al iniciar; solamente verifica la conectividad. Nunca subas contraseñas ni claves JWT reales al repositorio. Para desarrollo local, crea `src/CoachDesk.Api/appsettings.Development.json` (está ignorado por Git):

```json
{
  "ConnectionStrings": {
    "CoachDesk": "Host=db.TU_PROJECT_REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=TU_CONTRASENA;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Supabase": {
    "Url": "https://TU_PROJECT_REF.supabase.co",
    "PublishableKey": "TU_PUBLISHABLE_KEY"
  }
}
```

El acceso de usuarios se administra con Supabase Auth y los perfiles/roles existentes en el esquema importado. En la nube utiliza las variables de entorno equivalentes (`ConnectionStrings__CoachDesk`, `Supabase__Url`, `Supabase__PublishableKey`) en vez del archivo local. La publishable key puede estar en la configuración del backend, pero nunca expongas la contraseña de PostgreSQL ni una clave `service_role` al cliente WPF.

Para iniciar el backend:

```powershell
dotnet run --project src/CoachDesk.Api/CoachDesk.Api.csproj
```

Swagger estará disponible en `https://localhost:7001/swagger` durante el desarrollo.

### Primera ejecución con Supabase

1. Copia `src/CoachDesk.Api/appsettings.Development.example.json` como `src/CoachDesk.Api/appsettings.Development.json`.
2. En el panel de Supabase abre **Connect**, copia la cadena de conexión PostgreSQL y colócala en `ConnectionStrings:CoachDesk`. Para una API persistente usa la conexión directa; si tu red requiere IPv4, usa el pooler en modo sesión.
3. Agrega `Supabase:Url` y `Supabase:PublishableKey` desde **Project Settings > API**. No subas este archivo ni la contraseña de Supabase a Git.
4. Inicia la API. Validará la conexión sin modificar el esquema ni insertar datos. Después, `POST /api/auth/login` valida el correo y contraseña mediante Supabase Auth.
5. Ejecuta la aplicación WPF e inicia sesión con un usuario creado mediante Supabase Auth que tenga perfil de coach.

El archivo `appsettings.Development.json` está ignorado por Git; úsalo solamente para desarrollo local.

La interfaz no accede directamente a PostgreSQL ni a Supabase. Toda operación de datos pasa por servicios que consumen la API REST con `HttpClient`.

### Seguridad de la integración

- `DatabaseInitializer` solo ejecuta una comprobación de conexión; las migraciones automáticas, `EnsureCreated` y la inserción de datos demo están deshabilitados.
- La conexión Npgsql se limita a la comprobación de conectividad. Los endpoints de coach usan el Data API de Supabase con el token del usuario, evitando el bypass involuntario de RLS de una conexión PostgreSQL privilegiada.
- La baja de un cliente es lógica: cambia `clientes.estado` a `Baja`; no elimina el expediente.
- La publicación, cancelación y eliminación de horarios usa las funciones RPC `publicar_horario`, `cancelar_cita` y `eliminar_horario` del esquema oficial.

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
