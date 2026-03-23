using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestAI.Infrastructure.Persistence.Configurations.Commerce;

public sealed class FiscalDocumentSubmissionConfiguration : IEntityTypeConfiguration<FiscalDocumentSubmission>
{
    public void Configure(EntityTypeBuilder<FiscalDocumentSubmission> b)
    {
        b.ToTable("FiscalDocumentSubmissions");
        b.Property(x => x.RequestPayload).HasColumnType("nvarchar(max)");
        b.Property(x => x.ResponsePayload).HasColumnType("nvarchar(max)");
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);
        b.Property(x => x.ExternalReference).HasMaxLength(128);
        b.HasIndex(x => new { x.AccountId, x.CommercialInvoiceId, x.AttemptNumber }).IsUnique();
        b.HasOne(x => x.CommercialInvoice).WithMany(x => x.FiscalSubmissions).HasForeignKey(x => x.CommercialInvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
