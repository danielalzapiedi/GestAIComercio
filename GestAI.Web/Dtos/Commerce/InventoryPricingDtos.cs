namespace GestAI.Web.Dtos;

public enum StockMovementType
{
    Inbound = 0,
    Outbound = 1,
    Adjustment = 2,
    TransferOut = 3,
    TransferIn = 4
}

public enum PriceListBaseMode
{
    SalePrice = 0,
    Cost = 1,
    Manual = 2
}

public enum PriceListTargetType
{
    Product = 0,
    Variant = 1
}

public enum BulkPriceAdjustmentType
{
    Percentage = 0,
    FixedAmount = 1
}

public sealed record InventorySkuLookupDto(int ProductId, int? ProductVariantId, string Label, string InternalCode, bool HasVariants);
public sealed record InventorySeedDataDto(IReadOnlyList<LookupDto> Warehouses, IReadOnlyList<InventorySkuLookupDto> Skus, IReadOnlyList<LookupDto> Categories);
public sealed record InventoryStockItemDto(int ProductId, int? ProductVariantId, string ProductName, string SkuName, string InternalCode, string WarehouseName, int WarehouseId, decimal QuantityOnHand, decimal TotalQuantity, decimal MinimumStock, bool IsLowStock, DateTime? LastMovementAtUtc);
public sealed record InventoryOverviewDto(IReadOnlyList<InventoryStockItemDto> Items, decimal TotalUnits, int WarehousesWithStock, int LowStockCount, int DistinctSkus);
public sealed record StockMovementListItemDto(int Id, int ProductId, int? ProductVariantId, string SkuName, string InternalCode, string WarehouseName, int WarehouseId, string? CounterpartWarehouseName, StockMovementType MovementType, decimal QuantityDelta, string Reason, string? Note, string CreatedByUserId, DateTime OccurredAtUtc);

public sealed record PriceListListItemDto(int Id, string Name, PriceListBaseMode BaseMode, PriceListTargetType TargetType, bool IsActive, int ItemsCount, DateTime CreatedAtUtc, DateTime? ModifiedAtUtc);
public sealed record PriceListDetailDto(int Id, string Name, PriceListBaseMode BaseMode, PriceListTargetType TargetType, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed record PriceListItemDto(int Id, int PriceListId, int ProductId, int? ProductVariantId, string SkuName, string InternalCode, decimal BasePrice, decimal Price, bool IsActive, DateTime CreatedAtUtc, DateTime? ModifiedAtUtc);
public sealed record PriceListSeedDataDto(IReadOnlyList<LookupDto> Categories, IReadOnlyList<InventorySkuLookupDto> ProductSkus, IReadOnlyList<InventorySkuLookupDto> VariantSkus);
public sealed record BulkPriceUpdateResultDto(int UpdatedItems, int CreatedItems, int SkippedItems, string Summary);

public sealed class RecordStockMovementCommand
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int WarehouseId { get; set; }
    public int? CounterpartWarehouseId { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
}

public sealed class PriceListUpsertCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PriceListBaseMode BaseMode { get; set; }
    public PriceListTargetType TargetType { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PriceListItemUpsertCommand
{
    public int PriceListId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BulkPriceUpdateCommand
{
    public int PriceListId { get; set; }
    public BulkPriceAdjustmentType AdjustmentType { get; set; }
    public decimal Value { get; set; }
    public bool IncludeInactiveProducts { get; set; }
    public int? CategoryId { get; set; }
}

public sealed class ProductImportPreviewCommand
{
    public string CsvContent { get; set; } = string.Empty;
    public bool UpsertExisting { get; set; }
}

public sealed class ApplyProductImportCommand
{
    public string CsvContent { get; set; } = string.Empty;
    public bool UpsertExisting { get; set; }
}

public sealed record ProductImportPreviewRowDto(int RowNumber, string InternalCode, string Name, bool IsValid, bool WillCreateProduct, bool WillUpdateProduct, bool WillCreateVariant, bool WillUpdateVariant, string Message);
public sealed record ProductImportPreviewDto(int TotalRows, int ValidRows, int ErrorRows, IReadOnlyList<ProductImportPreviewRowDto> Rows);
public sealed record ProductImportResultDto(int ProcessedRows, int CreatedProducts, int UpdatedProducts, int CreatedVariants, int UpdatedVariants, int ErrorRows, IReadOnlyList<string> Messages);
