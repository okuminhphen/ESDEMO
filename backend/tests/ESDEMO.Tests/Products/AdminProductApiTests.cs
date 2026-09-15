using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Common.Security;
using ESDEMO.Application.Products.Commands.CreateProduct;
using ESDEMO.Application.Products.Dtos;
using ESDEMO.Infrastructure.Identity;
using ESDEMO.Infrastructure.Persistence;
using ESDEMO.Tests.Auth;
using ESDEMO.Tests.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ESDEMO.Tests.Products;

public sealed class AdminProductApiTests(PostgresDatabaseFixture database)
    : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "Strong_Test_Password!2026";

    [PostgresFact]
    public async Task Controller_requires_current_admin_role()
    {
        await using var factory = await CreateFactoryAsync();
        using var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/admin/products")).StatusCode);

        using var customer = factory.CreateClient();
        var customerEmail = NewEmail("customer");
        var registration = new RegisterRequestDto
        {
            Email = customerEmail,
            DisplayName = "Customer",
            Password = Password,
            ConfirmPassword = Password
        };
        Assert.Equal(HttpStatusCode.Created,
            (await customer.PostAsJsonAsync("/api/auth/register", registration)).StatusCode);
        await LoginAsync(customer, customerEmail);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await customer.GetAsync("/api/admin/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await customer.PostAsJsonAsync("/api/admin/products", ValidCreate())).StatusCode);
    }

    [PostgresFact]
    public async Task Admin_can_create_read_update_and_soft_delete_a_normalized_product()
    {
        await using var factory = await CreateFactoryAsync();
        using var admin = await CreateAdminClientAsync(factory);
        var sku = UniqueSku("crud").ToLowerInvariant();

        var createResponse = await admin.PostAsJsonAsync("/api/admin/products", ValidCreate(sku));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = (await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!;
        Assert.Equal(sku.ToUpperInvariant(), created.Sku);
        Assert.Equal("Demo Product", created.Name);
        Assert.NotEqual(0U, created.Version);
        Assert.EndsWith($"/api/admin/products/{created.Id}", createResponse.Headers.Location?.OriginalString);

        var detail = await admin.GetFromJsonAsync<ProductResponseDto>($"/api/admin/products/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
        Assert.Equal(created.Sku, detail.Sku);
        Assert.Equal(created.Name, detail.Name);
        Assert.Equal(created.Description, detail.Description);
        Assert.Equal(created.Price, detail.Price);
        Assert.Equal(created.StockQuantity, detail.StockQuantity);
        Assert.Equal(created.IsActive, detail.IsActive);
        Assert.Equal(created.Version, detail.Version);

        var update = new UpdateProductRequestDto
        {
            Sku = $" {sku} ",
            Name = " Updated Product ",
            Description = "  ",
            Price = 450_000,
            StockQuantity = 7,
            IsActive = false,
            Version = created.Version
        };
        var updateResponse = await admin.PutAsJsonAsync($"/api/admin/products/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!;
        Assert.Equal("Updated Product", updated.Name);
        Assert.Null(updated.Description);
        Assert.Equal(450_000, updated.Price);
        Assert.False(updated.IsActive);
        Assert.NotEqual(created.Version, updated.Version);
        Assert.NotNull(updated.UpdatedAt);

        var deleteResponse = await admin.DeleteAsync(
            $"/api/admin/products/{created.Id}?version={updated.Version}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.DeleteAsync($"/api/admin/products/{created.Id}?version={updated.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.PutAsJsonAsync($"/api/admin/products/{created.Id}", update)).StatusCode);

        var deleted = await admin.GetFromJsonAsync<ProductResponseDto>($"/api/admin/products/{created.Id}");
        Assert.False(deleted!.IsActive);
        Assert.NotNull(deleted.DeletedAt);
        var normalPage = await admin.GetFromJsonAsync<ProductPageResponseDto>(
            $"/api/admin/products?search={Uri.EscapeDataString(sku)}");
        Assert.Empty(normalPage!.Items);
        var deletedPage = await admin.GetFromJsonAsync<ProductPageResponseDto>(
            $"/api/admin/products?search={Uri.EscapeDataString(sku)}&includeDeleted=true");
        Assert.Contains(deletedPage!.Items, item => item.Id == created.Id);

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("/api/admin/products", ValidCreate(sku))).StatusCode);
    }

    [PostgresFact]
    public async Task Invalid_payload_query_and_missing_product_return_problem_details()
    {
        await using var factory = await CreateFactoryAsync();
        using var admin = await CreateAdminClientAsync(factory);
        var invalidPayloads = new object[]
        {
            new { sku = "bad sku", name = "A", price = -1, stockQuantity = -1, isActive = true },
            new { sku = UniqueSku("fraction"), name = "Product", price = 1.5m, stockQuantity = 1, isActive = true },
            new { sku = UniqueSku("missing"), name = "Product" }
        };

        foreach (var payload in invalidPayloads)
        {
            var response = await admin.PostAsJsonAsync("/api/admin/products", payload);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out _));
        }

        foreach (var query in new[] { "page=0", "page=1000001", "pageSize=0", "pageSize=101", $"search={new string('a', 201)}" })
        {
            Assert.Equal(HttpStatusCode.BadRequest,
                (await admin.GetAsync($"/api/admin/products?{query}")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.DeleteAsync($"/api/admin/products/{Guid.NewGuid()}?version=0")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.GetAsync($"/api/admin/products/{Guid.NewGuid()}")).StatusCode);
    }

    [PostgresFact]
    public async Task MediatR_validation_rejects_invalid_product_without_http()
    {
        await using var factory = await CreateFactoryAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<RequestValidationException>(() => sender.Send(
            new CreateProductCommand(new CreateProductRequestDto
            {
                Sku = "bad sku",
                Name = " ",
                Price = 1.5m,
                StockQuantity = -1,
                IsActive = true
            })));
    }

    [PostgresFact]
    public async Task List_has_stable_pagination_filters_and_literal_case_insensitive_search()
    {
        await using var factory = await CreateFactoryAsync();
        using var admin = await CreateAdminClientAsync(factory);
        var marker = Guid.NewGuid().ToString("N")[..10];
        var first = await CreateAsync(admin, ValidCreate($"LIST-{marker}-A", $"Percent % {marker}", true));
        await Task.Delay(2);
        var second = await CreateAsync(admin, ValidCreate($"LIST-{marker}_B", $"Underscore {marker}", false));
        await Task.Delay(2);
        var third = await CreateAsync(admin, ValidCreate($"LIST-{marker}-C", $"Third {marker}", true));

        var page = await admin.GetFromJsonAsync<ProductPageResponseDto>(
            $"/api/admin/products?search={marker}&page=1&pageSize=2");
        Assert.Equal(3, page!.TotalCount);
        Assert.Equal([third.Id, second.Id], page.Items.Select(item => item.Id));
        var pageTwo = await admin.GetFromJsonAsync<ProductPageResponseDto>(
            $"/api/admin/products?search={marker}&page=2&pageSize=2");
        Assert.Equal([first.Id], pageTwo!.Items.Select(item => item.Id));

        var active = await admin.GetFromJsonAsync<ProductPageResponseDto>(
            $"/api/admin/products?search={marker}&isActive=true");
        Assert.Equal(2, active!.TotalCount);
        Assert.All(active.Items, item => Assert.True(item.IsActive));
        var percent = await admin.GetFromJsonAsync<ProductPageResponseDto>(
            "/api/admin/products?search=%25");
        Assert.Contains(percent!.Items, item => item.Id == first.Id);
        Assert.DoesNotContain(percent.Items, item => item.Id == second.Id);
        var underscore = await admin.GetFromJsonAsync<ProductPageResponseDto>(
            "/api/admin/products?search=_");
        Assert.Contains(underscore!.Items, item => item.Id == second.Id);
        Assert.DoesNotContain(underscore.Items, item => item.Id == first.Id);
    }

    [PostgresFact]
    public async Task Stale_and_concurrent_changes_return_conflict_without_overwriting_the_winner()
    {
        await using var factory = await CreateFactoryAsync();
        using var admin = await CreateAdminClientAsync(factory);
        var created = await CreateAsync(admin, ValidCreate(UniqueSku("version")));
        var firstEdit = ValidUpdate(created, "First Edit");
        var firstResponse = await admin.PutAsJsonAsync($"/api/admin/products/{created.Id}", firstEdit);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var current = (await firstResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!;
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PutAsJsonAsync($"/api/admin/products/{created.Id}", ValidUpdate(created, "Stale Edit"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.DeleteAsync($"/api/admin/products/{created.Id}?version={created.Version}")).StatusCode);

        var concurrent = await Task.WhenAll(
            admin.PutAsJsonAsync($"/api/admin/products/{created.Id}", ValidUpdate(current, "Concurrent A")),
            admin.PutAsJsonAsync($"/api/admin/products/{created.Id}", ValidUpdate(current, "Concurrent B")));
        Assert.Single(concurrent, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(concurrent, response => response.StatusCode == HttpStatusCode.Conflict);
        var stored = await admin.GetFromJsonAsync<ProductResponseDto>($"/api/admin/products/{created.Id}");
        Assert.Contains(stored!.Name, new[] { "Concurrent A", "Concurrent B" });
    }

    private async Task<AuthApiFactory> CreateFactoryAsync()
    {
        var factory = new AuthApiFactory(database.ConnectionString, 1000);
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        return factory;
    }

    private static async Task<HttpClient> CreateAdminClientAsync(AuthApiFactory factory)
    {
        var email = NewEmail("admin");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                DisplayName = "Product Administrator",
                EmailConfirmed = true
            };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, ApplicationRoleNames.Admin)).Succeeded);
        }

        var client = factory.CreateClient();
        await LoginAsync(client, email);
        return client;
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto { Email = email, Password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = (await response.Content.ReadFromJsonAsync<TokenResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    private static async Task<ProductResponseDto> CreateAsync(HttpClient client, CreateProductRequestDto request)
    {
        var response = await client.PostAsJsonAsync("/api/admin/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponseDto>())!;
    }

    private static CreateProductRequestDto ValidCreate(
        string? sku = null, string name = " Demo Product ", bool active = true) => new()
        {
            Sku = sku ?? UniqueSku("product"),
            Name = name,
            Description = "Demo description",
            Price = 350_000,
            StockQuantity = 10,
            IsActive = active
        };

    private static UpdateProductRequestDto ValidUpdate(ProductResponseDto product, string name) => new()
    {
        Sku = product.Sku,
        Name = name,
        Description = product.Description,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        IsActive = product.IsActive,
        Version = product.Version
    };

    private static string UniqueSku(string prefix) => $"{prefix}-{Guid.NewGuid():N}".ToUpperInvariant();
    private static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.test";
}
