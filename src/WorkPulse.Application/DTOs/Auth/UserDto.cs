namespace WorkPulse.Application.DTOs.Auth;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
