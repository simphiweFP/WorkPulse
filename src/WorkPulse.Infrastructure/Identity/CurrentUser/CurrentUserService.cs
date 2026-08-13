using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WorkPulse.Application.Interfaces;

namespace WorkPulse.Infrastructure.Identity.CurrentUser;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public IReadOnlyCollection<string> Roles => _httpContextAccessor.HttpContext?.User
        .FindAll(ClaimTypes.Role)
        .Select(x => x.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
