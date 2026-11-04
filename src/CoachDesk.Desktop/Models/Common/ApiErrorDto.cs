namespace CoachDesk.Desktop.Models.Common;

public sealed class ApiErrorDto
{
    public string? Message { get; init; }
    public string? Code { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
