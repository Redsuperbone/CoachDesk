namespace CoachDesk.Api.Domain;

public enum UserRole { Coach, Advisee }
public enum AppointmentStatus { Available, Booked, Cancelled }
public enum MealType { Breakfast, Snack, Lunch, Dinner }

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Advisee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastAssessmentAt { get; set; }
    public List<AnthropometricAssessment> AnthropometricAssessments { get; set; } = [];
    public List<MealPlan> MealPlans { get; set; } = [];
}

public sealed class AnthropometricAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdviseeId { get; set; }
    public Advisee? Advisee { get; set; }
    public int MonthNumber { get; set; }
    public DateOnly? RecordedOn { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? BicipitalSkinfoldMm { get; set; }
    public decimal? TricipitalSkinfoldMm { get; set; }
    public decimal? SubscapularSkinfoldMm { get; set; }
    public decimal? SuprailiacSkinfoldMm { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipCm { get; set; }
    public decimal? ArmCm { get; set; }
    public decimal? ThighCm { get; set; }
}

public sealed class MealPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdviseeId { get; set; }
    public Advisee? Advisee { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public List<Meal> Meals { get; set; } = [];
}

public sealed class Meal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MealPlanId { get; set; }
    public MealPlan? MealPlan { get; set; }
    public MealType Type { get; set; }
    public List<NutritionItem> Items { get; set; } = [];
}

public sealed class NutritionItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MealId { get; set; }
    public Meal? Meal { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string Portion { get; set; } = string.Empty;
    public decimal Calories { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal CarbohydratesGrams { get; set; }
    public decimal FatGrams { get; set; }
}

public sealed class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CoachId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string Detail { get; set; } = string.Empty;
    public Guid? AdviseeId { get; set; }
    public Advisee? Advisee { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Available;
}
