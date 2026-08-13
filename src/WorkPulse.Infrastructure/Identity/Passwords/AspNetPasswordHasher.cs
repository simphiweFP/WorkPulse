using Microsoft.AspNetCore.Identity;
using WorkPulse.Application.Interfaces;

namespace WorkPulse.Infrastructure.Identity.Passwords;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<string> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(string.Empty, password);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(string.Empty, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
