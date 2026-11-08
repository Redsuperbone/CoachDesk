using CoachDesk.Api.Domain;

namespace CoachDesk.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);
public sealed record CoachResponse(int Id, string FullName, string Email, string Role);
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, CoachResponse Coach);
public sealed record AdviseeSummaryResponse(int Id, string FullName, string? Email, string? PhoneNumber, DateOnly? BirthDate, bool IsActive, DateTimeOffset? LastAssessmentAt);
public sealed record CreateAdviseeRequest(string FirstName, string LastName, string? Sex, DateOnly? BirthDate, string? PhoneNumber, string? Occupation, string? Goal, DateOnly? CoachingStartDate);
public sealed record UpdateAdviseeServiceStatusRequest(bool IsActive);
public sealed record AnthropometricAssessmentRequest(int MonthNumber, DateOnly? RecordedOn, decimal? WeightKg, decimal? HeightCm, decimal? BicipitalSkinfoldMm, decimal? TricipitalSkinfoldMm, decimal? SubscapularSkinfoldMm, decimal? SuprailiacSkinfoldMm, decimal? WaistCm, decimal? HipCm, decimal? ArmCm, decimal? ThighCm);
public sealed record AnthropometricProfileRequest(IReadOnlyList<AnthropometricAssessmentRequest> Assessments);
public sealed record AnthropometricProfileResponse(int AdviseeId, IReadOnlyList<AnthropometricAssessmentRequest> Assessments);
public sealed record PhotoEvidenceResponse(int Id, int Month, string Type, string FileName, long SizeBytes, DateTimeOffset UploadedAt);
public sealed record NutritionItemRequest(int? FoodId, string FoodName, string Portion, decimal Calories, decimal ProteinGrams, decimal CarbohydratesGrams, decimal FatGrams);
public sealed record MealRequest(MealType Type, IReadOnlyList<NutritionItemRequest> Items);
public sealed record MealPlanRequest(string Name, DateOnly EffectiveDate, string? Notes, IReadOnlyList<MealRequest> Meals);
public sealed record MealPlanResponse(int Id, int AdviseeId, string Name, DateOnly EffectiveDate, string? Notes, IReadOnlyList<MealRequest> Meals);
public sealed record CreateAvailabilityRequest(DateTimeOffset StartsAt, int DurationMinutes, string Detail);
public sealed record AppointmentResponse(int Id, DateTimeOffset StartsAt, int DurationMinutes, string Detail, string? AdviseeName, AppointmentStatus Status);
