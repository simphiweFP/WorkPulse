namespace WorkPulse.Infrastructure.Identity.Authorization;

public static class PolicyNames
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string AdminOrManager = nameof(AdminOrManager);
    public const string AdminManagerEmployee = nameof(AdminManagerEmployee);
}
