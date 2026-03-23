using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CommercialInvoiceConfiguration : IEntityTypeConfiguration<CommercialInvoice>
{
    public void Configure(EntityTypeBuilder<CommercialInvoice> b)
    {
        b.ToTable("CommercialInvoices");
        b.Property(x => x.Number).HasMaxLength(32).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        b.Property(x => x.Subtotal).HasPrecision(18, 2);
        b.Property(x => x.TaxAmount).HasPrecision(18, 2);
        b.Property(x => x.OtherTaxesAmount).HasPrecision(18, 2);
        b.Property(x => x.Total).HasPrecision(18, 2);
        b.Property(x => x.FiscalStatusDetail).HasMaxLength(500);
        b.Property(x => x.Cae).HasMaxLength(32);
        b.HasIndex(x => new { x.AccountId, x.Number }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.SaleId });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Sale).WithMany(x => x.Invoices).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FiscalConfiguration).WithMany(x => x.Invoices).HasForeignKey(x => x.FiscalConfigurationId).OnDelete(DeleteBehavior.Restrict);
    }
}
