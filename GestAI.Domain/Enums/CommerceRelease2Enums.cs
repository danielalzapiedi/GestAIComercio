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
