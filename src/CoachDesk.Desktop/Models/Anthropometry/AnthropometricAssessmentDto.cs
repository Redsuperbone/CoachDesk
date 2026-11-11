namespace CoachDesk.Desktop.Models.Anthropometry;

public sealed class AnthropometricAssessmentDto
{
    public int MonthNumber { get; init; }
    public DateOnly? RecordedOn { get; init; }
    public decimal? WeightKg { get; init; }
    public decimal? HeightCm { get; init; }
    public decimal? BicipitalSkinfoldMm { get; init; }
    public decimal? TricipitalSkinfoldMm { get; init; }
    public decimal? SubscapularSkinfoldMm { get; init; }
    public decimal? SuprailiacSkinfoldMm { get; init; }
    public decimal? WaistCm { get; init; }
    public decimal? HipCm { get; init; }
    public decimal? ArmCm { get; init; }
    public decimal? ThighCm { get; init; }
    public bool IsConfirmed { get; init; }
}
