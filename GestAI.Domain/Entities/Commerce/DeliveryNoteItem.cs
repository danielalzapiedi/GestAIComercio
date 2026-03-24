using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class DeliveryNoteItem : AuditableEntity
{
    public int AccountId { get; set; }
    public int DeliveryNoteId { get; set; }
    public DeliveryNote DeliveryNote { get; set; } = null!;
    public int SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityDelivered { get; set; }
    public int SortOrder { get; set; }
}
