using GestAI.Domain.Enums;

namespace GestAI.Application.Commerce;

public sealed record InventorySkuLookupDto(int ProductId, int? ProductVariantId, string Label, string InternalCode, bool HasVariants);
public sealed record InventorySeedDataDto(IReadOnlyList<LookupDto> Warehouses, IReadOnlyList<InventorySkuLookupDto> Skus, IReadOnlyList<LookupDto> Categories);

public sealed record InventoryStockItemDto(
    int ProductId,
    int? ProductVariantId,
    string ProductName,
    string SkuName,
    string InternalCode,
    string WarehouseName,
    int WarehouseId,
    decimal QuantityOnHand,
    decimal TotalQuantity,
    decimal MinimumStock,
    bool IsLowStock,
    DateTime? LastMovementAtUtc);

public sealed record InventoryOverviewDto(
    IReadOnlyList<InventoryStockItemDto> Items,
    decimal TotalUnits,
    int WarehousesWithStock,
    int LowStockCount,
    int DistinctSkus);

public sealed record StockMovementListItemDto(
    int Id,
    int ProductId,
    int? ProductVariantId,
    string SkuName,
    string InternalCode,
    string WarehouseName,
    int WarehouseId,
    string? CounterpartWarehouseName,
    StockMovementType MovementType,
    decimal QuantityDelta,
    string Reason,
    string? Note,
    string CreatedByUserId,
    DateTime OccurredAtUtc);

public sealed record PriceListListItemDto(int Id, string Name, PriceListBaseMode BaseMode, PriceListTargetType TargetType, bool IsActive, int ItemsCount, DateTime CreatedAtUtc, DateTime? ModifiedAtUtc);
public sealed record PriceListDetailDto(int Id, string Name, PriceListBaseMode BaseMode, PriceListTargetType TargetType, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed record PriceListItemDto(int Id, int PriceListId, int ProductId, int? ProductVariantId, string SkuName, string InternalCode, decimal BasePrice, decimal Price, bool IsActive, DateTime CreatedAtUtc, DateTime? ModifiedAtUtc);
public sealed record PriceListSeedDataDto(IReadOnlyList<LookupDto> Categories, IReadOnlyList<InventorySkuLookupDto> ProductSkus, IReadOnlyList<InventorySkuLookupDto> VariantSkus);
public sealed record BulkPriceUpdateResultDto(int UpdatedItems, int CreatedItems, int SkippedItems, string Summary);

public sealed record ProductImportPreviewRowDto(int RowNumber, string InternalCode, string Name, bool IsValid, bool WillCreateProduct, bool WillUpdateProduct, bool WillCreateVariant, bool WillUpdateVariant, string Message);
public sealed record ProductImportPreviewDto(int TotalRows, int ValidRows, int ErrorRows, IReadOnlyList<ProductImportPreviewRowDto> Rows);
public sealed record ProductImportResultDto(int ProcessedRows, int CreatedProducts, int UpdatedProducts, int CreatedVariants, int UpdatedVariants, int ErrorRows, IReadOnlyList<string> Messages);
