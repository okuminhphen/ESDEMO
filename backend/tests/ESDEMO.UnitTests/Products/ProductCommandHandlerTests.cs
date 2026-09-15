using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Products.Abstractions;
using ESDEMO.Application.Products.Commands.CreateProduct;
using ESDEMO.Application.Products.Commands.DeleteProduct;
using ESDEMO.Application.Products.Commands.UpdateProduct;
using ESDEMO.Application.Products.Dtos;
using ESDEMO.Domain.Products;

namespace ESDEMO.UnitTests.Products;

public sealed class ProductCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_normalizes_input_and_sets_creation_time()
    {
        var repository = new RecordingProductRepository();
        var handler = new CreateProductCommandHandler(repository, new FixedTimeProvider(Now));

        await handler.Handle(new CreateProductCommand(new CreateProductRequestDto
        {
            Sku = " demo-1 ", Name = " Demo product ", Description = "  Useful product  ",
            Price = 150_000, StockQuantity = 3, IsActive = true
        }), CancellationToken.None);

        var product = Assert.IsType<Product>(repository.Added);
        Assert.Equal("DEMO-1", product.Sku);
        Assert.Equal("Demo product", product.Name);
        Assert.Equal("Useful product", product.Description);
        Assert.Equal(Now, product.CreatedAt);
    }

    [Fact]
    public async Task Update_rejects_a_product_that_was_soft_deleted()
    {
        var deleted = NewProduct();
        deleted.DeletedAt = Now;
        var repository = new RecordingProductRepository { Found = deleted };
        var handler = new UpdateProductCommandHandler(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateProductCommand(Guid.NewGuid(), ValidUpdate()), CancellationToken.None));
        Assert.Null(repository.Updated);
    }

    [Fact]
    public async Task Update_normalizes_input_and_uses_client_concurrency_version()
    {
        var product = NewProduct();
        var repository = new RecordingProductRepository { Found = product };
        var handler = new UpdateProductCommandHandler(repository, new FixedTimeProvider(Now));

        await handler.Handle(new UpdateProductCommand(product.Id, ValidUpdate()), CancellationToken.None);

        Assert.Same(product, repository.Updated);
        Assert.Equal("UPDATED-1", product.Sku);
        Assert.Equal("Updated product", product.Name);
        Assert.Null(product.Description);
        Assert.Equal(Now, product.UpdatedAt);
        Assert.Equal((uint)7, repository.ExpectedVersion);
    }

    [Fact]
    public async Task Delete_marks_the_product_inactive_and_sets_audit_times()
    {
        var product = NewProduct();
        var repository = new RecordingProductRepository { Found = product };
        var handler = new DeleteProductCommandHandler(repository, new FixedTimeProvider(Now));

        await handler.Handle(new DeleteProductCommand(product.Id, new DeleteProductRequestDto { Version = 9 }), CancellationToken.None);

        Assert.False(product.IsActive);
        Assert.Equal(Now, product.DeletedAt);
        Assert.Equal(Now, product.UpdatedAt);
        Assert.Equal((uint)9, repository.ExpectedVersion);
    }

    private static Product NewProduct() => new()
    {
        Sku = "ORIGINAL-1", Name = "Original product", Price = 100, StockQuantity = 1, IsActive = true
    };

    private static UpdateProductRequestDto ValidUpdate() => new()
    {
        Sku = " updated-1 ", Name = " Updated product ", Description = " ",
        Price = 200, StockQuantity = 2, IsActive = false, Version = 7
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingProductRepository : IProductRepository
    {
        public Product? Found { get; init; }
        public Product? Added { get; private set; }
        public Product? Updated { get; private set; }
        public uint ExpectedVersion { get; private set; }

        public Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Found);
        public Task<ProductResponseDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<ProductResponseDto?>(null);
        public Task<ProductPageResponseDto> GetPageAsync(ProductListRequestDto request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProductResponseDto> AddAsync(Product product, CancellationToken cancellationToken)
        {
            Added = product;
            return Task.FromResult(Response(product));
        }
        public Task<ProductResponseDto> UpdateAsync(Product product, uint expectedVersion, CancellationToken cancellationToken)
        {
            Updated = product;
            ExpectedVersion = expectedVersion;
            return Task.FromResult(Response(product));
        }
        private static ProductResponseDto Response(Product product) => new(product.Id, product.Sku, product.Name, product.Description, product.Price, product.StockQuantity, product.IsActive, product.CreatedAt, product.UpdatedAt, product.DeletedAt, 1);
    }
}
