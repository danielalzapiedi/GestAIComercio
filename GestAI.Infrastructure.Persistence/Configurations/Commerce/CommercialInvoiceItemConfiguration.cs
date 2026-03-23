using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CommercialInvoiceItemConfiguration : IEntityTypeConfiguration<CommercialInvoiceItem>
{
    public void Configure(EntityTypeBuilder<CommercialInvoiceItem> b)
    {
        b.ToTable("CommercialInvoiceItems");
        b.Property(x => x.Description).HasMaxLength(300).IsRequired();
        b.Property(x => x.InternalCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Quantity).HasPrecision(18, 2);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.LineSubtotal).HasPrecision(18, 2);
        b.Property(x => x.TaxRate).HasPrecision(9, 4);
        b.Property(x => x.TaxAmount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.CommercialInvoiceId, x.SortOrder });
        b.HasOne(x => x.CommercialInvoice).WithMany(x => x.Items).HasForeignKey(x => x.CommercialInvoiceId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.SaleItem).WithMany(x => x.CommercialInvoiceItems).HasForeignKey(x => x.SaleItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
