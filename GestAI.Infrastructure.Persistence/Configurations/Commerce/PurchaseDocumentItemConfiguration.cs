using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class PurchaseDocumentItemConfiguration : IEntityTypeConfiguration<PurchaseDocumentItem>
{
    public void Configure(EntityTypeBuilder<PurchaseDocumentItem> b)
    {
        b.ToTable("PurchaseDocumentItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).HasMaxLength(300).IsRequired();
        b.Property(x => x.InternalCode).HasMaxLength(80).IsRequired();
        b.Property(x => x.QuantityOrdered).HasPrecision(18, 2);
        b.Property(x => x.QuantityReceived).HasPrecision(18, 2);
        b.Property(x => x.UnitCost).HasPrecision(18, 2);
        b.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.PurchaseDocumentId, x.SortOrder });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.PurchaseDocument).WithMany(x => x.Items).HasForeignKey(x => x.PurchaseDocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
