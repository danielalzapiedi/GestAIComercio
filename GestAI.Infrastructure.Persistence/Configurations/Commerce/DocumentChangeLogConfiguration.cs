using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class DocumentChangeLogConfiguration : IEntityTypeConfiguration<DocumentChangeLog>
{
    public void Configure(EntityTypeBuilder<DocumentChangeLog> b)
    {
        b.ToTable("DocumentChangeLogs");
        b.Property(x => x.EntityName).HasMaxLength(64).IsRequired();
        b.Property(x => x.DocumentNumber).HasMaxLength(32).IsRequired();
        b.Property(x => x.Action).HasMaxLength(64).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        b.Property(x => x.ChangedFields).HasColumnType("nvarchar(max)");
        b.Property(x => x.RelatedDocumentNumber).HasMaxLength(32);
        b.Property(x => x.UserId).HasMaxLength(64);
        b.Property(x => x.UserName).HasMaxLength(200);
        b.HasIndex(x => new { x.AccountId, x.EntityName, x.EntityId, x.ChangedAtUtc });
    }
}
