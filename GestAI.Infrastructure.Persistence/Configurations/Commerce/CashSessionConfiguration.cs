using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> b)
    {
        b.ToTable("CashSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.OpenedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ClosedByUserId).HasMaxLength(450);
        b.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        b.Property(x => x.ClosingBalanceExpected).HasPrecision(18, 2);
        b.Property(x => x.ClosingBalanceDeclared).HasPrecision(18, 2);
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ModifiedByUserId).HasMaxLength(450);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.AccountId, x.CashRegisterId, x.Status });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.CashRegister).WithMany(x => x.Sessions).HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Cascade);
    }
}
