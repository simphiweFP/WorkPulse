namespace WorkPulse.Application.DTOs.Auth;

public class AuthResponseDto
{
    public UserDto User { get; set; } = new();
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
