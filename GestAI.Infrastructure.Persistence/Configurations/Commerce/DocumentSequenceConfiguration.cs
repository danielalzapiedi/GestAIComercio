using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> b)
    {
        b.ToTable("DocumentSequences");
        b.Property(x => x.DocumentType).HasMaxLength(64).IsRequired();
        b.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
        b.HasIndex(x => new { x.AccountId, x.DocumentType, x.PointOfSale }).IsUnique();
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
