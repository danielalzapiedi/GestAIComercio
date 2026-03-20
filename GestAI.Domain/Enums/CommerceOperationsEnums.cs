namespace GestAI.Domain.Enums;

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


public enum QuoteStatus
{
    Draft = 0,
    Sent = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Converted = 5
}

public enum SaleStatus
{
    Draft = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}


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
