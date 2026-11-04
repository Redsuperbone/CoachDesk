namespace CoachDesk.Desktop.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public string BaseUrl { get; init; } = "https://localhost:7001/api/";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

}
