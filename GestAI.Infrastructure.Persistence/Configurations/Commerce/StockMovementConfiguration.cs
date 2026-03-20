using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("StockMovements");
        b.HasKey(x => x.Id);
        b.Property(x => x.MovementType).HasConversion<int>();
        b.Property(x => x.QuantityDelta).HasPrecision(18, 2);
        b.Property(x => x.Reason).HasMaxLength(200).IsRequired();
        b.Property(x => x.Note).HasMaxLength(1000);
        b.Property(x => x.ReferenceGroup).HasMaxLength(80);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.OccurredAtUtc });
        b.HasIndex(x => new { x.AccountId, x.ProductId, x.ProductVariantId, x.WarehouseId, x.OccurredAtUtc });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CounterpartWarehouse).WithMany().HasForeignKey(x => x.CounterpartWarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
