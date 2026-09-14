using ESDEMO.Domain.Orders;
using ESDEMO.Domain.Products;
using ESDEMO.Infrastructure.Identity;
using ESDEMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ESDEMO.Tests.Persistence;

public sealed class ApplicationModelTests
{
    [Fact]
    public void Model_builds_and_generates_PostgreSQL_schema_without_a_database()
    {
        using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE \"AspNetUsers\"", script);
        Assert.Contains("CREATE TABLE \"Products\"", script);
        Assert.Contains("CREATE TABLE \"Orders\"", script);
        Assert.Contains("CREATE TABLE \"RefreshSessions\"", script);
        Assert.Contains("CREATE TABLE \"OutboxMessages\"", script);
        Assert.Contains("UX_PaymentAttempts_OneSuccessPerOrder", script);
        Assert.Contains("WHERE \"Status\" = 'Succeeded'", script);
    }

    [Fact]
    public void Business_entities_have_no_Identity_dependency_or_accidental_shadow_foreign_keys()
    {
        Assert.DoesNotContain(typeof(Product).Assembly.GetReferencedAssemblies(),
            assembly => assembly.Name!.Contains("Identity") || assembly.Name.Contains("EntityFrameworkCore"));

        using var context = CreateContext();
        var foreignKeyProperties = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .SelectMany(foreignKey => foreignKey.Properties);
        Assert.DoesNotContain(foreignKeyProperties, property => property.IsShadowProperty());
    }

    [Fact]
    public void Concurrency_and_history_relationships_are_configured()
    {
        using var context = CreateContext();
        var product = context.Model.FindEntityType(typeof(Product))!;
        Assert.True(product.FindProperty("Version")!.IsConcurrencyToken);

        var order = context.Model.FindEntityType(typeof(Order))!;
        Assert.All(order.GetForeignKeys(), foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));

        var user = context.Model.FindEntityType(typeof(ApplicationUser))!;
        Assert.Contains(user.GetIndexes(), index =>
            index.IsUnique && index.Properties.SingleOrDefault()?.Name == nameof(ApplicationUser.NormalizedEmail));
    }

    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_generation_only")
            .Options);
}
