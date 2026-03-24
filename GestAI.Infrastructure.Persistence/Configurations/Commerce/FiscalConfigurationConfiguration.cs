using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class FiscalConfigurationConfiguration : IEntityTypeConfiguration<FiscalConfiguration>
{
    public void Configure(EntityTypeBuilder<FiscalConfiguration> b)
    {
        b.ToTable("FiscalConfigurations");
        b.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
        b.Property(x => x.TaxIdentifier).HasMaxLength(32).IsRequired();
        b.Property(x => x.GrossIncomeTaxId).HasMaxLength(64);
        b.Property(x => x.CertificateReference).HasMaxLength(300);
        b.Property(x => x.PrivateKeyReference).HasMaxLength(300);
        b.Property(x => x.ApiBaseUrl).HasMaxLength(300);
        b.Property(x => x.Observations).HasMaxLength(1000);
        b.HasIndex(x => new { x.AccountId, x.IsActive });
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
