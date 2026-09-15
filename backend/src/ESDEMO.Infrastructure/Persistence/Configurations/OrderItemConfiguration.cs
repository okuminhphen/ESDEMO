using ESDEMO.Domain.Orders;
using ESDEMO.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ESDEMO.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_UnitPrice", "\"UnitPrice\" >= 0 AND \"UnitPrice\" <= 999999999999999999 AND \"UnitPrice\" = trunc(\"UnitPrice\")");
            table.HasCheckConstraint("CK_OrderItems_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_OrderItems_Name", "length(btrim(\"ProductNameSnapshot\")) > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(item => item.UnitPrice).HasColumnType("numeric");
        builder.HasIndex(item => new { item.OrderId, item.ProductId }).IsUnique();
        builder.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
