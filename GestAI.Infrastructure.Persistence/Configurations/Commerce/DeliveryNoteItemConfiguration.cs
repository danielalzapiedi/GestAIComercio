using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class DeliveryNoteItemConfiguration : IEntityTypeConfiguration<DeliveryNoteItem>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteItem> b)
    {
        b.ToTable("DeliveryNoteItems");
        b.Property(x => x.Description).HasMaxLength(300).IsRequired();
        b.Property(x => x.InternalCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.QuantityOrdered).HasPrecision(18, 2);
        b.Property(x => x.QuantityDelivered).HasPrecision(18, 2);
        b.HasIndex(x => new { x.DeliveryNoteId, x.SortOrder });
        b.HasOne(x => x.DeliveryNote).WithMany(x => x.Items).HasForeignKey(x => x.DeliveryNoteId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.SaleItem).WithMany(x => x.DeliveryNoteItems).HasForeignKey(x => x.SaleItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
