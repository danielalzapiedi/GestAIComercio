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


public enum InvoiceType
{
    InvoiceA = 0,
    InvoiceB = 1,
    InvoiceC = 2,
    CreditNoteA = 3,
    CreditNoteB = 4,
    CreditNoteC = 5
}

public enum InvoiceStatus
{
    Draft = 0,
    PendingAuthorization = 1,
    Authorized = 2,
    Rejected = 3,
    IntegrationError = 4,
    Cancelled = 5
}

public enum DeliveryNoteStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyDelivered = 2,
    Delivered = 3,
    Cancelled = 4
}

public enum FiscalIntegrationMode
{
    Mock = 0,
    ArcaWsfe = 1
}

public enum FiscalSubmissionStatus
{
    Pending = 0,
    Authorized = 1,
    Rejected = 2,
    Error = 3
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

public enum CustomerAccountMovementType
{
    SaleDocument = 0,
    Collection = 1,
    Adjustment = 2
}

public enum CashSessionStatus
{
    Open = 0,
    Closed = 1
}

public enum CashMovementDirection
{
    In = 0,
    Out = 1
}

public enum CashMovementOriginType
{
    Opening = 0,
    Manual = 1,
    CustomerCollection = 2,
    SupplierPayment = 3,
    ClosingAdjustment = 4
}

public enum PaymentMethod
{
    Cash = 0,
    Transfer = 1,
    DebitCard = 2,
    CreditCard = 3,
    Check = 4,
    AccountCredit = 5,
    Other = 6
}
