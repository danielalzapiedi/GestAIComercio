using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class FiscalConfiguration : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string LegalName { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public string? GrossIncomeTaxId { get; set; }
    public int PointOfSale { get; set; } = 1;
    public InvoiceType DefaultInvoiceType { get; set; } = InvoiceType.InvoiceB;
    public FiscalIntegrationMode IntegrationMode { get; set; } = FiscalIntegrationMode.Mock;
    public bool UseSandbox { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? CertificateReference { get; set; }
    public string? PrivateKeyReference { get; set; }
    public string? ApiBaseUrl { get; set; }
    public string? Observations { get; set; }
    public DateTime? LastConnectionCheckAtUtc { get; set; }
    public ICollection<CommercialInvoice> Invoices { get; set; } = new List<CommercialInvoice>();
}
