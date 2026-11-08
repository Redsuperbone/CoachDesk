namespace CoachDesk.Api.Domain;

// Entidades de lectura/escritura alineadas al esquema SQL importado en Supabase.
// La API usa PostgREST con JWT para las operaciones de negocio; estos mapeos no
// se emplean para crear ni migrar el esquema.
public sealed class Profile
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Coach
{
    public int CoachId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Client
{
    public int ClientNumber { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Sex { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Occupation { get; set; }
    public string? Goal { get; set; }
    public DateOnly? CoachingStartDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class Measurement
{
    public int MeasurementId { get; set; }
    public int ClientNumber { get; set; }
    public short Period { get; set; }
    public short Month { get; set; }
    public DateOnly MeasurementDate { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Neck { get; set; }
    public decimal? Chest { get; set; }
    public decimal? Waist { get; set; }
    public decimal? Hip { get; set; }
    public decimal? Leg { get; set; }
    public decimal? Biceps { get; set; }
    public decimal? Calf { get; set; }
    public decimal? TricipitalSkinfold { get; set; }
    public decimal? BicipitalSkinfold { get; set; }
    public decimal? IliacCrestSkinfold { get; set; }
    public decimal? ScapularSkinfold { get; set; }
    public decimal? BodyFatPercentage { get; set; }
    public bool Confirmed { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

public sealed class Food
{
    public int FoodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal KcalPer100g { get; set; }
    public decimal ProteinPer100g { get; set; }
    public decimal CarbsPer100g { get; set; }
    public decimal FatPer100g { get; set; }
    public bool IsActive { get; set; }
}

public sealed class NutritionPlan
{
    public int NutritionPlanId { get; set; }
    public int ClientNumber { get; set; }
    public int? CoachId { get; set; }
    public string? Goal { get; set; }
    public short KcalTarget { get; set; }
    public short ProteinTargetGrams { get; set; }
    public short CarbsTargetGrams { get; set; }
    public short FatTargetGrams { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
}

public sealed class MealEntry
{
    public int MealId { get; set; }
    public int NutritionPlanId { get; set; }
    public short Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly? Time { get; set; }
    public string? Description { get; set; }
}

public sealed class AppointmentSlot
{
    public int AppointmentId { get; set; }
    public int? CoachId { get; set; }
    public int? ClientNumber { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Modality { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset? ReservedAt { get; set; }
    public int Version { get; set; }
}
