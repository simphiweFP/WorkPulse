using Microsoft.AspNetCore.Identity;
using PasswordHasherContract = WorkPulse.Application.Interfaces.IPasswordHasher;

namespace WorkPulse.Integration.Identity.Authentication;

public sealed class AspNetPasswordHasher : PasswordHasherContract
{
    private readonly PasswordHasher<string> _passwordHasher = new();

    public string Hash(string password)
        => _passwordHasher.HashPassword(string.Empty, password);

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(string.Empty, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}