using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class Sale : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Number { get; set; } = string.Empty;
    public SaleStatus Status { get; set; } = SaleStatus.Draft;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime IssuedAtUtc { get; set; }
    public string? Observations { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public int? SourceQuoteId { get; set; }
    public Quote? SourceQuote { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<CommercialInvoice> Invoices { get; set; } = new List<CommercialInvoice>();
    public ICollection<DeliveryNote> DeliveryNotes { get; set; } = new List<DeliveryNote>();
}
