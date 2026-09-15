using System.ComponentModel.DataAnnotations;
using ESDEMO.Application.Common.Security;
using ESDEMO.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ESDEMO.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    ApplicationDbContext dbContext,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminSeedOptions> adminSeedOptions,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        foreach (var roleName in ApplicationRoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                EnsureSucceeded(
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName)),
                    $"create the {roleName} role");
            }
        }

        var options = adminSeedOptions.Value;

        if (!options.Enabled)
        {
            logger.LogInformation("Admin seed is disabled; roles were initialized without an admin account.");
            return;
        }

        Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);

        var admin = await userManager.FindByEmailAsync(options.Email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                DisplayName = options.DisplayName.Trim(),
                Email = options.Email.Trim(),
                UserName = options.Email.Trim(),
                EmailConfirmed = true
            };

            EnsureSucceeded(
                await userManager.CreateAsync(admin, options.Password),
                "create the configured admin account");
        }

        if (!await userManager.IsInRoleAsync(admin, ApplicationRoleNames.Admin))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(admin, ApplicationRoleNames.Admin),
                "assign the Admin role to the configured account");
        }

        logger.LogInformation("Identity roles and the configured admin account are initialized.");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(", ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Failed to {operation}. {errors}");
    }
}
