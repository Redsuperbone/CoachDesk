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
