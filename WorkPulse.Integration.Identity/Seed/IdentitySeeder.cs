using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using WorkPulse.Integration.Identity.Models;
using WorkPulse.Integration.Identity.Roles;

namespace WorkPulse.Integration.Identity.Seed;

public static class IdentitySeeder
{
    public static async Task<IdentitySeedResult> SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureRolesAsync(roleManager, cancellationToken);

        const string adminEmail = "admin@workpulse.local";
        const string adminPassword = "WorkPulseAdmin123!";
        const string developerEmail = "developer@workpulse.local";
        const string developerPassword = "WorkPulseDev123!";
        const string pendingEmail = "pending@workpulse.local";
        const string pendingPassword = "WorkPulsePending123!";

        var adminId = await EnsureUserAsync(userManager, WorkPulseRoles.Admin, adminEmail, adminPassword, "System", "Admin", logger);
        var developerId = await EnsureUserAsync(userManager, WorkPulseRoles.Developer, developerEmail, developerPassword, "Default", "Developer", logger);
        var pendingId = await EnsurePendingUserAsync(userManager, pendingEmail, pendingPassword, "Awaiting", "User", logger);

        return new IdentitySeedResult
        {
            AdminUserId = adminId,
            DeveloperUserId = developerId,
            PendingUserId = pendingId
        };
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager, CancellationToken cancellationToken)
    {
        foreach (var role in new[] { WorkPulseRoles.Pending, WorkPulseRoles.Admin, WorkPulseRoles.Developer })
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<string> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string role,
        string email,
        string password,
        string firstName,
        string lastName,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = role == WorkPulseRoles.Admin ? "11111111-1111-1111-1111-111111111111" : "22222222-2222-2222-2222-222222222222",
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            logger.LogInformation("Created seed user {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user.Id;
    }

    private static async Task<string> EnsurePendingUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName,
        ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = "33333333-3333-3333-3333-333333333333",
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            logger.LogInformation("Created seed user {Email}", email);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        return user.Id;
    }
}
