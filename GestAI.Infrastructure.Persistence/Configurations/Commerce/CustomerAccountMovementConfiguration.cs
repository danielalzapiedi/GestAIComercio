using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CustomerAccountMovementConfiguration : IEntityTypeConfiguration<CustomerAccountMovement>
{
    public void Configure(EntityTypeBuilder<CustomerAccountMovement> b)
    {
        b.ToTable("CustomerAccountMovements");
        b.HasKey(x => x.Id);
        b.Property(x => x.MovementType).HasConversion<int>();
        b.Property(x => x.PaymentMethod).HasConversion<int>();
        b.Property(x => x.ReferenceNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.DebitAmount).HasPrecision(18, 2);
        b.Property(x => x.CreditAmount).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(250).IsRequired();
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.CustomerId, x.IssuedAtUtc });
        b.HasIndex(x => new { x.AccountId, x.SaleId }).IsUnique().HasFilter("[SaleId] IS NOT NULL");
        b.HasIndex(x => new { x.AccountId, x.CashMovementId }).IsUnique().HasFilter("[CashMovementId] IS NOT NULL");
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Sale).WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashMovement).WithOne(x => x.CustomerAccountMovement).HasForeignKey<CustomerAccountMovement>(x => x.CashMovementId).OnDelete(DeleteBehavior.Restrict);
    }
}
