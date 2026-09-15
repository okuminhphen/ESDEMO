using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Orders.Dtos;
using ESDEMO.Application.Products.Dtos;
using ESDEMO.Domain.Products;
using ESDEMO.Infrastructure.Persistence;
using ESDEMO.Tests.Auth;
using ESDEMO.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ESDEMO.Tests.Orders;

public sealed class CustomerOrderApiTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "Valid_Test_Password!2026";

    [PostgresFact]
    public async Task Customer_can_create_pay_and_read_only_own_order_with_outbox_message()
    {
        await using var factory = await CreateFactoryAsync();
        using var customer = factory.CreateClient();
        using var other = factory.CreateClient();
        var customerTokens = await RegisterAndLoginAsync(customer, "customer");
        var otherTokens = await RegisterAndLoginAsync(other, "other");
        Authorize(customer, customerTokens);
        Authorize(other, otherTokens);
        var product = await AddProductAsync(factory);

        var catalog = await customer.GetFromJsonAsync<PublicProductPageResponseDto>("/api/products");
        Assert.Contains(catalog!.Items, item => item.Id == product.Id && item.IsInStock);

        var createKey = Guid.NewGuid().ToString("N");
        var createdResponse = await customer.PostAsJsonAsync("/api/orders", new { productId = product.Id, idempotencyKey = createKey });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = (await createdResponse.Content.ReadFromJsonAsync<OrderResponseDto>())!;
        Assert.Equal(product.Price, created.TotalAmount);
        Assert.Equal("PendingPayment", created.Status);
        Assert.Single(created.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/orders/{created.Id}")).StatusCode);

        var wrong = await customer.PostAsJsonAsync($"/api/orders/{created.Id}/pay", new { amount = product.Price - 1, idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.Conflict, wrong.StatusCode);
        var paidResponse = await customer.PostAsJsonAsync($"/api/orders/{created.Id}/pay", new { amount = product.Price, idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.OK, paidResponse.StatusCode);
        var paid = (await paidResponse.Content.ReadFromJsonAsync<OrderResponseDto>())!;
        Assert.Equal("Paid", paid.Status);
        Assert.NotNull(paid.PaidAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await db.Products.Where(item => item.Id == product.Id).Select(item => item.StockQuantity).SingleAsync());
        Assert.Single(await db.OutboxMessages.Where(message => message.EventType == "OrderPaid").ToListAsync());
    }

    [PostgresFact]
    public async Task Create_order_idempotency_returns_same_order_and_rejects_changed_request()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var tokens = await RegisterAndLoginAsync(client, "idempotent");
        Authorize(client, tokens);
        var first = await AddProductAsync(factory, "ORD-ONE");
        var second = await AddProductAsync(factory, "ORD-TWO");
        var key = Guid.NewGuid().ToString("N");
        var created = await client.PostAsJsonAsync("/api/orders", new { productId = first.Id, idempotencyKey = key });
        var replay = await client.PostAsJsonAsync("/api/orders", new { productId = first.Id, idempotencyKey = key });
        var changed = await client.PostAsJsonAsync("/api/orders", new { productId = second.Id, idempotencyKey = key });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        var one = (await created.Content.ReadFromJsonAsync<OrderResponseDto>())!;
        var two = (await replay.Content.ReadFromJsonAsync<OrderResponseDto>())!;
        Assert.Equal(one.Id, two.Id);
    }

    private async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory(database.ConnectionString, 1000);
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        return factory;
    }

    private static async Task<Product> AddProductAsync(AuthApiFactory factory, string sku = "ORDER-PRODUCT")
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var product = new Product { Sku = $"{sku}-{Guid.NewGuid():N}"[..Math.Min(30, sku.Length + 33)], Name = "Order product", Price = 25_000, StockQuantity = 1, IsActive = true };
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private static async Task<TokenResponseDto> RegisterAndLoginAsync(HttpClient client, string label)
    {
        var email = $"{label}-{Guid.NewGuid():N}@example.test";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new { email, displayName = "Customer", password = Password, confirmPassword = Password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<TokenResponseDto>())!;
    }

    private static void Authorize(HttpClient client, TokenResponseDto tokens) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
}
