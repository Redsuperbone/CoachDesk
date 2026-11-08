using Microsoft.EntityFrameworkCore;

namespace CoachDesk.Api.Data;

/// <summary>
/// Verifica que la API pueda alcanzar la base de datos configurada.
/// El esquema de Supabase se administra fuera de esta aplicación; por ello,
/// este componente no aplica migraciones ni inserta datos al iniciar.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task VerifyConnectionAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<CoachDeskDbContext>();

        try
        {
            await database.Database.OpenConnectionAsync();
            // Verifica que el script de ciclos ya se aplicó sin modificar datos.
            await database.Measurements.AsNoTracking().OrderBy(item => item.MeasurementId).Select(item => item.Period).Take(1).ToListAsync();
            await database.Database.CloseConnectionAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No fue posible verificar la conexión a PostgreSQL/Supabase.");
            throw new InvalidOperationException("No fue posible conectarse a la base de datos configurada. Revisa el detalle registrado por la API.", exception);
        }

        logger.LogInformation("Conexión y esquema de ciclos de PostgreSQL/Supabase verificados. Las migraciones y datos semilla están deshabilitados.");
    }
}
