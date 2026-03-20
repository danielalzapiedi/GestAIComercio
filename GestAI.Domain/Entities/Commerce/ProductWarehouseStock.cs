using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class ProductWarehouseStock : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public decimal QuantityOnHand { get; set; }
    public DateTime? LastMovementAtUtc { get; set; }
}
