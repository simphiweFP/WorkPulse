namespace WorkPulse.Application.DTOs.Users;

public sealed class DeveloperDto
{
    public string Id { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int ActiveTaskCount { get; init; }
    public int InProgressTaskCount { get; init; }
    public int CompletedTaskCount { get; init; }
}
