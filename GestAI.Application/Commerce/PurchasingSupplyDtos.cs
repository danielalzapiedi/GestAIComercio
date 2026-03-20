using GestAI.Domain.Enums;

namespace GestAI.Application.Commerce;

public sealed record PurchaseSeedDataDto(
    IReadOnlyList<LookupDto> Suppliers,
    IReadOnlyList<LookupDto> Warehouses,
    IReadOnlyList<PurchaseSkuLookupDto> Products,
    IReadOnlyList<PurchaseSkuLookupDto> Variants,
    string CostStrategy,
    string CostStrategyDescription);

public sealed record PurchaseSkuLookupDto(
    int ProductId,
    int? ProductVariantId,
    string Label,
    string InternalCode,
    decimal CurrentCost,
    bool IsActive);

public sealed record PurchaseLineDto(
    int Id,
    int ProductId,
    int? ProductVariantId,
    string Description,
    string InternalCode,
    decimal QuantityOrdered,
    decimal QuantityReceived,
    decimal PendingQuantity,
    decimal UnitCost,
    decimal LineSubtotal,
    int SortOrder,
    decimal CurrentRecordedCost);

public sealed record GoodsReceiptListItemDto(
    int Id,
    string Number,
    int WarehouseId,
    string WarehouseName,
    DateTime ReceivedAtUtc,
    decimal TotalQuantity,
    decimal TotalCost,
    int ItemsCount,
    string CreatedByUserId);

public sealed record GoodsReceiptDetailDto(
    int Id,
    string Number,
    int WarehouseId,
    string WarehouseName,
    DateTime ReceivedAtUtc,
    string? Observations,
    decimal TotalQuantity,
    decimal TotalCost,
    IReadOnlyList<GoodsReceiptLineDto> Items,
    string CreatedByUserId,
    DateTime CreatedAtUtc);

public sealed record GoodsReceiptLineDto(
    int PurchaseDocumentItemId,
    int ProductId,
    int? ProductVariantId,
    string Description,
    string InternalCode,
    decimal QuantityReceived,
    decimal UnitCost,
    decimal LineSubtotal,
    int SortOrder);

public sealed record PurchaseListItemDto(
    int Id,
    string Number,
    PurchaseDocumentType DocumentType,
    PurchaseDocumentStatus Status,
    int SupplierId,
    string SupplierName,
    DateTime IssuedAtUtc,
    string? SupplierDocumentNumber,
    decimal Subtotal,
    decimal Total,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    int ItemsCount,
    DateTime? LastReceiptAtUtc);

public sealed record PurchaseDetailDto(
    int Id,
    string Number,
    PurchaseDocumentType DocumentType,
    PurchaseDocumentStatus Status,
    int SupplierId,
    string SupplierName,
    DateTime IssuedAtUtc,
    string? SupplierDocumentNumber,
    string? Observations,
    decimal Subtotal,
    decimal Total,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal PendingQuantity,
    IReadOnlyList<PurchaseLineDto> Items,
    IReadOnlyList<GoodsReceiptListItemDto> Receipts,
    SupplierAccountSummaryDto SupplierAccount,
    string CostStrategy,
    string CostStrategyDescription,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? ModifiedByUserId,
    DateTime? ModifiedAtUtc,
    bool CanEdit,
    bool CanReceive);

public sealed record SupplierAccountSummaryDto(
    int SupplierId,
    string SupplierName,
    decimal Balance,
    decimal Debit,
    decimal Credit,
    int MovementsCount,
    DateTime? LastMovementAtUtc,
    IReadOnlyList<SupplierAccountMovementDto> RecentMovements);

public sealed record SupplierAccountMovementDto(
    int Id,
    SupplierAccountMovementType MovementType,
    string ReferenceNumber,
    string Description,
    DateTime IssuedAtUtc,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal BalanceImpact,
    int? PurchaseDocumentId,
    string? Note);

public sealed record SupplierAccountListItemDto(
    int SupplierId,
    string SupplierName,
    string TaxId,
    bool IsActive,
    decimal Balance,
    decimal Debit,
    decimal Credit,
    int DocumentsCount,
    DateTime? LastMovementAtUtc);
