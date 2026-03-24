using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class CommercialInvoice : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Number { get; set; } = string.Empty;
    public int PointOfSale { get; set; }
    public int SequentialNumber { get; set; }
    public InvoiceType InvoiceType { get; set; } = InvoiceType.InvoiceB;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int? FiscalConfigurationId { get; set; }
    public FiscalConfiguration? FiscalConfiguration { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public string CurrencyCode { get; set; } = "ARS";
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal OtherTaxesAmount { get; set; }
    public decimal Total { get; set; }
    public string? FiscalStatusDetail { get; set; }
    public string? Cae { get; set; }
    public DateTime? CaeDueDateUtc { get; set; }
    public DateTime? LastSubmissionAtUtc { get; set; }
    public ICollection<CommercialInvoiceItem> Items { get; set; } = new List<CommercialInvoiceItem>();
    public ICollection<FiscalDocumentSubmission> FiscalSubmissions { get; set; } = new List<FiscalDocumentSubmission>();
    public ICollection<DeliveryNote> DeliveryNotes { get; set; } = new List<DeliveryNote>();
}
