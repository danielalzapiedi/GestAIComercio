using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class SupplierAccountMovementConfiguration : IEntityTypeConfiguration<SupplierAccountMovement>
{
    public void Configure(EntityTypeBuilder<SupplierAccountMovement> b)
    {
        b.ToTable("SupplierAccountMovements");
        b.HasKey(x => x.Id);
        b.Property(x => x.MovementType).HasConversion<int>();
        b.Property(x => x.ReferenceNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.DebitAmount).HasPrecision(18, 2);
        b.Property(x => x.CreditAmount).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(200).IsRequired();
        b.Property(x => x.Note).HasMaxLength(1000);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.SupplierId, x.IssuedAtUtc });
        b.HasIndex(x => new { x.AccountId, x.PurchaseDocumentId }).IsUnique().HasFilter("[PurchaseDocumentId] IS NOT NULL");
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PurchaseDocument).WithMany(x => x.SupplierAccountMovements).HasForeignKey(x => x.PurchaseDocumentId).OnDelete(DeleteBehavior.Restrict);
    }
}
