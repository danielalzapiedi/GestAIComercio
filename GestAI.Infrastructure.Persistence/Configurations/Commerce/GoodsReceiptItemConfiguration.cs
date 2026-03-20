using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptItem> b)
    {
        b.ToTable("GoodsReceiptItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).HasMaxLength(300).IsRequired();
        b.Property(x => x.InternalCode).HasMaxLength(80).IsRequired();
        b.Property(x => x.QuantityReceived).HasPrecision(18, 2);
        b.Property(x => x.UnitCost).HasPrecision(18, 2);
        b.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.GoodsReceiptId, x.SortOrder });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.GoodsReceipt).WithMany(x => x.Items).HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.PurchaseDocumentItem).WithMany(x => x.ReceiptItems).HasForeignKey(x => x.PurchaseDocumentItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
