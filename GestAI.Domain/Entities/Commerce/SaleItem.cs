using GestAI.Domain.Common;
using GestAI.Domain.Entities;

namespace GestAI.Domain.Entities.Commerce;

public sealed class SaleItem : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineSubtotal { get; set; }
    public int SortOrder { get; set; }
    public ICollection<CommercialInvoiceItem> CommercialInvoiceItems { get; set; } = new List<CommercialInvoiceItem>();
    public ICollection<DeliveryNoteItem> DeliveryNoteItems { get; set; } = new List<DeliveryNoteItem>();
}
