namespace WorkPulse.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
}
