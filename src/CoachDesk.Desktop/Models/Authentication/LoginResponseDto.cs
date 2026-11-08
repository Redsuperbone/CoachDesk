namespace CoachDesk.Desktop.Models.Authentication;

public sealed class LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset? ExpiresAt { get; init; }
    public CoachDto Coach { get; init; } = new();
}

public sealed class CoachDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;

    public bool IsCoach => string.Equals(Role, "Coach", StringComparison.OrdinalIgnoreCase);
}
