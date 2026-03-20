namespace GestAI.Web.Dtos;

public enum PurchaseDocumentType
{
    PurchaseDocument = 0,
    PurchaseOrder = 1
}

public enum PurchaseDocumentStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public enum SupplierAccountMovementType
{
    PurchaseDocument = 0,
    Payment = 1,
    Adjustment = 2
}

public sealed record PurchaseSeedDataDto(IReadOnlyList<LookupDto> Suppliers, IReadOnlyList<LookupDto> Warehouses, IReadOnlyList<PurchaseSkuLookupDto> Products, IReadOnlyList<PurchaseSkuLookupDto> Variants, string CostStrategy, string CostStrategyDescription);
public sealed record PurchaseSkuLookupDto(int ProductId, int? ProductVariantId, string Label, string InternalCode, decimal CurrentCost, bool IsActive);
public sealed record PurchaseLineDto(int Id, int ProductId, int? ProductVariantId, string Description, string InternalCode, decimal QuantityOrdered, decimal QuantityReceived, decimal PendingQuantity, decimal UnitCost, decimal LineSubtotal, int SortOrder, decimal CurrentRecordedCost);
public sealed record GoodsReceiptListItemDto(int Id, string Number, int WarehouseId, string WarehouseName, DateTime ReceivedAtUtc, decimal TotalQuantity, decimal TotalCost, int ItemsCount, string CreatedByUserId);
public sealed record GoodsReceiptDetailDto(int Id, string Number, int WarehouseId, string WarehouseName, DateTime ReceivedAtUtc, string? Observations, decimal TotalQuantity, decimal TotalCost, IReadOnlyList<GoodsReceiptLineDto> Items, string CreatedByUserId, DateTime CreatedAtUtc);
public sealed record GoodsReceiptLineDto(int PurchaseDocumentItemId, int ProductId, int? ProductVariantId, string Description, string InternalCode, decimal QuantityReceived, decimal UnitCost, decimal LineSubtotal, int SortOrder);
public sealed record PurchaseListItemDto(int Id, string Number, PurchaseDocumentType DocumentType, PurchaseDocumentStatus Status, int SupplierId, string SupplierName, DateTime IssuedAtUtc, string? SupplierDocumentNumber, decimal Subtotal, decimal Total, decimal OrderedQuantity, decimal ReceivedQuantity, int ItemsCount, DateTime? LastReceiptAtUtc);
public sealed record PurchaseDetailDto(int Id, string Number, PurchaseDocumentType DocumentType, PurchaseDocumentStatus Status, int SupplierId, string SupplierName, DateTime IssuedAtUtc, string? SupplierDocumentNumber, string? Observations, decimal Subtotal, decimal Total, decimal OrderedQuantity, decimal ReceivedQuantity, decimal PendingQuantity, IReadOnlyList<PurchaseLineDto> Items, IReadOnlyList<GoodsReceiptListItemDto> Receipts, SupplierAccountSummaryDto SupplierAccount, string CostStrategy, string CostStrategyDescription, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc, bool CanEdit, bool CanReceive);
public sealed record SupplierAccountSummaryDto(int SupplierId, string SupplierName, decimal Balance, decimal Debit, decimal Credit, int MovementsCount, DateTime? LastMovementAtUtc, IReadOnlyList<SupplierAccountMovementDto> RecentMovements);
public sealed record SupplierAccountMovementDto(int Id, SupplierAccountMovementType MovementType, string ReferenceNumber, string Description, DateTime IssuedAtUtc, decimal DebitAmount, decimal CreditAmount, decimal BalanceImpact, int? PurchaseDocumentId, string? Note);
public sealed record SupplierAccountListItemDto(int SupplierId, string SupplierName, string TaxId, bool IsActive, decimal Balance, decimal Debit, decimal Credit, int DocumentsCount, DateTime? LastMovementAtUtc);

public sealed class PurchaseUpsertCommand
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public PurchaseDocumentType DocumentType { get; set; } = PurchaseDocumentType.PurchaseDocument;
    public PurchaseDocumentStatus Status { get; set; } = PurchaseDocumentStatus.Draft;
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public string? SupplierDocumentNumber { get; set; }
    public string? Observations { get; set; }
    public List<PurchaseLineFormModel> Items { get; set; } = new();
}

public sealed class PurchaseLineFormModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; } = 1;
    public decimal QuantityReceived { get; set; }
    public decimal PendingQuantity => Math.Max(0m, QuantityOrdered - QuantityReceived);
    public decimal UnitCost { get; set; }
    public decimal CurrentRecordedCost { get; set; }
    public decimal LineSubtotal => Math.Round(QuantityOrdered * UnitCost, 2, MidpointRounding.AwayFromZero);
}

public sealed class GoodsReceiptCommand
{
    public int PurchaseDocumentId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Observations { get; set; }
    public List<GoodsReceiptLineFormModel> Items { get; set; } = new();
}

public sealed class GoodsReceiptLineFormModel
{
    public int PurchaseDocumentItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public decimal PendingQuantity { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineSubtotal => Math.Round(QuantityReceived * UnitCost, 2, MidpointRounding.AwayFromZero);
}
