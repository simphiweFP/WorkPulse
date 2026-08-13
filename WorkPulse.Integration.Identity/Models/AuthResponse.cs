namespace WorkPulse.Integration.Identity.Models;

public class AuthResponse
{
    public AuthUser User { get; set; } = new();
    public string Token { get; set; } = string.Empty;
}
