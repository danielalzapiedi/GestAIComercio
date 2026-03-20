using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class SupplierAccountAllocationConfiguration : IEntityTypeConfiguration<SupplierAccountAllocation>
{
    public void Configure(EntityTypeBuilder<SupplierAccountAllocation> b)
    {
        b.ToTable("SupplierAccountAllocations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.SourceMovementId, x.TargetMovementId }).IsUnique();
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.SourceMovement).WithMany(x => x.AllocationsAsSource).HasForeignKey(x => x.SourceMovementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TargetMovement).WithMany(x => x.AllocationsAsTarget).HasForeignKey(x => x.TargetMovementId).OnDelete(DeleteBehavior.Restrict);
    }
}
