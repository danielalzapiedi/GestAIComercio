using GestAI.Domain.Common;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class StockMovement : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int? CounterpartWarehouseId { get; set; }
    public Warehouse? CounterpartWarehouse { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal QuantityDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? ReferenceGroup { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
