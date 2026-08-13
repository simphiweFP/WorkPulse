using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WorkPulse.Integration.Identity.Models;

namespace WorkPulse.Integration.Identity.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _settings;

    public JwtTokenService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> settings)
    {
        _userManager = userManager;
        _settings = settings.Value;
    }

    public async Task<string> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
        };

        if (roles.Count > 0)
        {
            claims.Add(new Claim(ClaimTypes.Role, roles[0]));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
