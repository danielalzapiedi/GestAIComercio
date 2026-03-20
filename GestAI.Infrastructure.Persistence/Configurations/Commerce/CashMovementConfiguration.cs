using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> b)
    {
        b.ToTable("CashMovements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Direction).HasConversion<int>();
        b.Property(x => x.OriginType).HasConversion<int>();
        b.Property(x => x.PaymentMethod).HasConversion<int>();
        b.Property(x => x.ReferenceNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Concept).HasMaxLength(250).IsRequired();
        b.Property(x => x.Observations).HasMaxLength(2000);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.CashRegisterId, x.OccurredAtUtc });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CashRegister).WithMany().HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CashSession).WithMany(x => x.Movements).HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
