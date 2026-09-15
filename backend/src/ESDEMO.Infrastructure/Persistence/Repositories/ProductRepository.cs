using System.Linq.Expressions;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Products.Abstractions;
using ESDEMO.Application.Products.Dtos;
using ESDEMO.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ESDEMO.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext db) : IProductRepository
{
    private const string VersionProperty = "Version";
    private static readonly Expression<Func<Product, ProductResponseDto>> Projection = product => new(
        product.Id, product.Sku, product.Name, product.Description, product.Price, product.StockQuantity,
        product.IsActive, product.CreatedAt, product.UpdatedAt, product.DeletedAt,
        EF.Property<uint>(product, VersionProperty));

    public Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<ProductResponseDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking().Where(product => product.Id == id).Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProductPageResponseDto> GetPageAsync(ProductListRequestDto request, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();
        if (!request.IncludeDeleted)
        {
            query = query.Where(product => product.DeletedAt == null);
        }
        if (request.IsActive is { } active)
        {
            query = query.Where(product => product.IsActive == active);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Search treats %, _ and backslash literally, not as SQL LIKE wildcards.
            var search = request.Search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
            var pattern = $"%{search}%";
            query = query.Where(product => EF.Functions.ILike(product.Sku, pattern, "\\")
                || EF.Functions.ILike(product.Name, pattern, "\\"));
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(product => product.CreatedAt).ThenByDescending(product => product.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(Projection).ToListAsync(cancellationToken);
        return new ProductPageResponseDto(items, count, request.Page, request.PageSize);
    }

    public async Task<ProductResponseDto> AddAsync(Product product, CancellationToken cancellationToken)
    {
        db.Products.Add(product);
        await SaveAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponseDto> UpdateAsync(Product product, uint expectedVersion, CancellationToken cancellationToken)
    {
        var entry = db.Entry(product);
        var version = entry.Property<uint>(VersionProperty);
        if (version.CurrentValue != expectedVersion)
        {
            throw ChangedProduct();
        }
        version.OriginalValue = expectedVersion;
        // Even an otherwise identical PUT must check the version at the database.
        entry.Property(item => item.UpdatedAt).IsModified = true;
        await SaveAsync(cancellationToken);
        return ToResponse(product);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            // One SaveChanges is atomic; the database unique index and xmin check
            // remain authoritative if requests race after the initial read.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ChangedProduct();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Products_Sku" })
        {
            throw new ConflictException("A product with this SKU already exists.");
        }
    }

    private ProductResponseDto ToResponse(Product product) => new(
        product.Id, product.Sku, product.Name, product.Description, product.Price, product.StockQuantity,
        product.IsActive, product.CreatedAt, product.UpdatedAt, product.DeletedAt,
        db.Entry(product).Property<uint>(VersionProperty).CurrentValue);

    private static ConflictException ChangedProduct() =>
        new("The product has changed. Reload it and retry with the current version.");
}
