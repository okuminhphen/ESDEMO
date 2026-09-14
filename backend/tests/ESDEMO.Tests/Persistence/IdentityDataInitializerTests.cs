using ESDEMO.Application.Common.Security;
using ESDEMO.Infrastructure.Identity;
using ESDEMO.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ESDEMO.Tests.Persistence;

public sealed class IdentityDataInitializerTests(PostgresDatabaseFixture database)
    : IClassFixture<PostgresDatabaseFixture>
{
    [PostgresFact]
    public async Task Initializer_applies_migrations_and_seeds_roles_and_admin_idempotently()
    {
        const string email = "seed-admin@example.test";
        const string password = "Strong_Test_Password!2026";
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(database.ConnectionString));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.Configure<AdminSeedOptions>(options =>
        {
            options.Enabled = true;
            options.Email = email;
            options.DisplayName = "Seed Administrator";
            options.Password = password;
        });
        services.AddScoped<DatabaseInitializer>();

        await using var serviceProvider = services.BuildServiceProvider();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        }

        await using var assertionScope = serviceProvider.CreateAsyncScope();
        var context = assertionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = assertionScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(email);

        Assert.NotNull(admin);
        Assert.Equal(2, await context.Roles.CountAsync());
        Assert.Single(await context.Users.ToListAsync());
        Assert.True(await userManager.IsInRoleAsync(admin, ApplicationRoleNames.Admin));
        Assert.True(await userManager.CheckPasswordAsync(admin, password));
    }
}
