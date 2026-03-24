using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class DeliveryNoteConfiguration : IEntityTypeConfiguration<DeliveryNote>
{
    public void Configure(EntityTypeBuilder<DeliveryNote> b)
    {
        b.ToTable("DeliveryNotes");
        b.Property(x => x.Number).HasMaxLength(32).IsRequired();
        b.Property(x => x.Observations).HasMaxLength(1000);
        b.Property(x => x.TotalQuantity).HasPrecision(18, 2);
        b.Property(x => x.PendingQuantity).HasPrecision(18, 2);
        b.HasIndex(x => new { x.AccountId, x.Number }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.SaleId, x.DeliveredAtUtc });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Sale).WithMany(x => x.DeliveryNotes).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CommercialInvoice).WithMany(x => x.DeliveryNotes).HasForeignKey(x => x.CommercialInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
