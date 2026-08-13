namespace WorkPulse.Application.DTOs.Auth;

public sealed class TokenResultDto
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
