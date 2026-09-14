using Microsoft.EntityFrameworkCore;

namespace ESDEMO.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add ApplyConfigurationsFromAssembly when the first entity configuration exists.
    }
}
