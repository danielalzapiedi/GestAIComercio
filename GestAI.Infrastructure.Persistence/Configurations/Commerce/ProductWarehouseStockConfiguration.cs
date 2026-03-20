using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class ProductWarehouseStockConfiguration : IEntityTypeConfiguration<ProductWarehouseStock>
{
    public void Configure(EntityTypeBuilder<ProductWarehouseStock> b)
    {
        b.ToTable("ProductWarehouseStocks");
        b.HasKey(x => x.Id);
        b.Property(x => x.QuantityOnHand).HasPrecision(18, 2);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.WarehouseId, x.ProductId }).HasFilter("[ProductVariantId] IS NULL").IsUnique();
        b.HasIndex(x => new { x.AccountId, x.WarehouseId, x.ProductVariantId }).HasFilter("[ProductVariantId] IS NOT NULL").IsUnique();
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
