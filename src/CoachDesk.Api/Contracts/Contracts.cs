using CoachDesk.Api.Domain;

namespace CoachDesk.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);
public sealed record CoachResponse(Guid Id, string FullName, string Email, string Role);
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, CoachResponse Coach);
public sealed record AdviseeSummaryResponse(Guid Id, string FullName, string? Email, string? PhoneNumber, DateOnly? BirthDate, bool IsActive, DateTimeOffset? LastAssessmentAt);
public sealed record AnthropometricAssessmentRequest(int MonthNumber, DateOnly? RecordedOn, decimal? WeightKg, decimal? HeightCm, decimal? BicipitalSkinfoldMm, decimal? TricipitalSkinfoldMm, decimal? SubscapularSkinfoldMm, decimal? SuprailiacSkinfoldMm, decimal? WaistCm, decimal? HipCm, decimal? ArmCm, decimal? ThighCm);
public sealed record AnthropometricProfileRequest(IReadOnlyList<AnthropometricAssessmentRequest> Assessments);
public sealed record AnthropometricProfileResponse(Guid AdviseeId, IReadOnlyList<AnthropometricAssessmentRequest> Assessments);
public sealed record NutritionItemRequest(string FoodName, string Portion, decimal Calories, decimal ProteinGrams, decimal CarbohydratesGrams, decimal FatGrams);
public sealed record MealRequest(MealType Type, IReadOnlyList<NutritionItemRequest> Items);
public sealed record MealPlanRequest(string Name, DateOnly EffectiveDate, string? Notes, IReadOnlyList<MealRequest> Meals);
public sealed record MealPlanResponse(Guid Id, Guid AdviseeId, string Name, DateOnly EffectiveDate, string? Notes, IReadOnlyList<MealRequest> Meals);
public sealed record CreateAvailabilityRequest(DateTimeOffset StartsAt, int DurationMinutes, string Detail);
public sealed record AppointmentResponse(Guid Id, DateTimeOffset StartsAt, int DurationMinutes, string Detail, string? AdviseeName, AppointmentStatus Status);
