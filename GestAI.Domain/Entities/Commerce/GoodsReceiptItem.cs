using GestAI.Domain.Common;
using GestAI.Domain.Entities;

namespace GestAI.Domain.Entities.Commerce;

public sealed class GoodsReceiptItem : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public int PurchaseDocumentItemId { get; set; }
    public PurchaseDocumentItem PurchaseDocumentItem { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineSubtotal { get; set; }
    public int SortOrder { get; set; }
}
