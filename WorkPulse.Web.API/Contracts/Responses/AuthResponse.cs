namespace WorkPulse.Web.API.Contracts.Responses;

public class AuthResponse
{
    public UserResponse User { get; set; } = new();
    public string Token { get; set; } = string.Empty;
}
