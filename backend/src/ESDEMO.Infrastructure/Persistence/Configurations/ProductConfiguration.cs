using ESDEMO.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Price", "\"Price\" >= 0 AND \"Price\" <= 999999999999999999 AND \"Price\" = trunc(\"Price\")");
            table.HasCheckConstraint("CK_Products_Stock", "\"StockQuantity\" >= 0");
            table.HasCheckConstraint("CK_Products_Sku", "\"Sku\" ~ '^[A-Z0-9][A-Z0-9_-]*$'");
            table.HasCheckConstraint("CK_Products_Name", "length(btrim(\"Name\")) > 0");
            table.HasCheckConstraint("CK_Products_Deletion", "\"DeletedAt\" IS NULL OR (NOT \"IsActive\" AND \"DeletedAt\" >= \"CreatedAt\")");
            table.HasCheckConstraint("CK_Products_UpdatedAt", "\"UpdatedAt\" IS NULL OR \"UpdatedAt\" >= \"CreatedAt\"");
        });
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Sku).HasMaxLength(64).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(4000);
        // Unconstrained numeric preserves fractional input so the CHECK rejects it instead of rounding it.
        builder.Property(product => product.Price).HasColumnType("numeric");
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.HasIndex(product => new { product.IsActive, product.CreatedAt });
        builder.Property<uint>("Version").IsRowVersion();
        // No global filter: order history must retain access to discontinued products.
    }
}
