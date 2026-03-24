using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class CommercialInvoiceItem : AuditableEntity
{
    public int AccountId { get; set; }
    public int CommercialInvoiceId { get; set; }
    public CommercialInvoice CommercialInvoice { get; set; } = null!;
    public int SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public int SortOrder { get; set; }
}
