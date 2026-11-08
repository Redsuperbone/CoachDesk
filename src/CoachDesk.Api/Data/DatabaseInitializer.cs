using CoachDesk.Api.Domain;
using CoachDesk.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CoachDesk.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        using var scope = services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<CoachDeskDbContext>();
        await database.Database.MigrateAsync();

        var email = configuration["Seed:CoachEmail"]?.Trim().ToLowerInvariant();
        var password = configuration["Seed:CoachPassword"];
        var fullName = configuration["Seed:CoachFullName"]?.Trim();

        User? coach = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            coach = await database.Users.SingleOrDefaultAsync(item => item.Email == email);
        }

        if (coach is null && (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password)))
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No se creó el coach inicial: Seed__CoachEmail y Seed__CoachPassword deben configurarse juntos.");
            }
            else
            {
                var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
                coach = new User
                {
                    FullName = string.IsNullOrWhiteSpace(fullName) ? "Coach administrador" : fullName,
                    Email = email,
                    PasswordHash = passwords.Hash(password),
                    Role = UserRole.Coach,
                    IsActive = true
                };
                database.Users.Add(coach);
                await database.SaveChangesAsync();
                logger.LogInformation("Se creó el coach inicial con el correo configurado en Seed__CoachEmail.");
            }
        }

        if (coach is null) coach = await database.Users.FirstOrDefaultAsync(item => item.Role == UserRole.Coach && item.IsActive);
        if (coach is null)
        {
            logger.LogInformation("No se crearon datos demo: configura un Coach inicial primero.");
            return;
        }

        if (!configuration.GetValue<bool>("Seed:DemoData") || await database.Advisees.AnyAsync()) return;

        var carlos = new Advisee
        {
            FullName = "Carlos Ruiz Méndez", Email = "carlos.ruiz@example.test", PhoneNumber = "55 1000 1001",
            BirthDate = new DateOnly(2000, 5, 14), LastAssessmentAt = DateTimeOffset.UtcNow,
            AnthropometricAssessments =
            [
                new AnthropometricAssessment { MonthNumber = 1, RecordedOn = DateOnly.FromDateTime(DateTime.UtcNow), WeightKg = 78.2m, HeightCm = 178m, BicipitalSkinfoldMm = 8m, TricipitalSkinfoldMm = 12m, SubscapularSkinfoldMm = 15m, SuprailiacSkinfoldMm = 14m, WaistCm = 82m, HipCm = 98m, ArmCm = 36m, ThighCm = 59m }
            ]
        };
        var mariana = new Advisee { FullName = "Mariana Ortiz Soto", Email = "mariana.ortiz@example.test", PhoneNumber = "55 1000 1002", BirthDate = new DateOnly(1998, 9, 3), LastAssessmentAt = DateTimeOffset.UtcNow };
        var jorge = new Advisee { FullName = "Jorge Hernández León", Email = "jorge.hernandez@example.test", PhoneNumber = "55 1000 1003", BirthDate = new DateOnly(1995, 1, 22), LastAssessmentAt = DateTimeOffset.UtcNow };
        database.Advisees.AddRange(carlos, mariana, jorge);
        database.MealPlans.Add(new MealPlan
        {
            Advisee = carlos,
            Name = "Déficit calórico - Mes 1",
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Notes = "Hidratación mínima: 2.5 litros de agua al día.",
            Meals =
            [
                new Meal { Type = MealType.Breakfast, Items = [new NutritionItem { FoodName = "Avena con huevo y manzana", Portion = "1 porción", Calories = 520m, ProteinGrams = 31m, CarbohydratesGrams = 56m, FatGrams = 18m }] },
                new Meal { Type = MealType.Lunch, Items = [new NutritionItem { FoodName = "Pechuga de pollo con arroz", Portion = "1 plato", Calories = 640m, ProteinGrams = 52m, CarbohydratesGrams = 68m, FatGrams = 14m }] },
                new Meal { Type = MealType.Dinner, Items = [new NutritionItem { FoodName = "Pescado blanco con verduras", Portion = "1 plato", Calories = 430m, ProteinGrams = 42m, CarbohydratesGrams = 24m, FatGrams = 16m }] }
            ]
        });
        var todayUtc = DateTime.UtcNow.Date;
        database.Appointments.AddRange(
            new Appointment { CoachId = coach.Id, StartsAt = new DateTimeOffset(todayUtc.AddDays(1).AddHours(16), TimeSpan.Zero), DurationMinutes = 60, Detail = "Toma de pliegues y perímetros" },
            new Appointment { CoachId = coach.Id, StartsAt = new DateTimeOffset(todayUtc.AddDays(2).AddHours(18), TimeSpan.Zero), DurationMinutes = 45, Detail = "Videollamada de rutina" });
        await database.SaveChangesAsync();
        logger.LogInformation("Se crearon los datos demo locales de Coach Desk.");
    }
}
