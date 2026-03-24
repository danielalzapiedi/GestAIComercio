namespace GestAI.Web.Dtos;

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

public sealed record FiscalConfigurationDto(
    int Id,
    string LegalName,
    string TaxIdentifier,
    string? GrossIncomeTaxId,
    int PointOfSale,
    InvoiceType DefaultInvoiceType,
    FiscalIntegrationMode IntegrationMode,
    bool UseSandbox,
    bool IsActive,
    string? CertificateReference,
    string? PrivateKeyReference,
    string? ApiBaseUrl,
    string? Observations,
    DateTime? LastConnectionCheckAtUtc,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? ModifiedByUserId,
    DateTime? ModifiedAtUtc);

public sealed class UpsertFiscalConfigurationCommandDto
{
    public int Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public string? GrossIncomeTaxId { get; set; }
    public int PointOfSale { get; set; } = 1;
    public InvoiceType DefaultInvoiceType { get; set; } = InvoiceType.InvoiceB;
    public FiscalIntegrationMode IntegrationMode { get; set; } = FiscalIntegrationMode.Mock;
    public bool UseSandbox { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? CertificateReference { get; set; }
    public string? PrivateKeyReference { get; set; }
    public string? ApiBaseUrl { get; set; }
    public string? Observations { get; set; }
}

public sealed class UploadFiscalCredentialCommandDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentBase64 { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public bool IsPrivateKey { get; set; }
}

public sealed record InvoiceLineDto(
    int Id,
    int SaleItemId,
    int ProductId,
    int? ProductVariantId,
    string Description,
    string InternalCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    decimal TaxRate,
    decimal TaxAmount,
    int SortOrder);

public sealed record FiscalSubmissionDto(
    int Id,
    int AttemptNumber,
    FiscalSubmissionStatus Status,
    DateTime RequestedAtUtc,
    DateTime? RespondedAtUtc,
    string RequestPayload,
    string? ResponsePayload,
    string? ErrorMessage,
    string? ExternalReference);

public sealed record CommercialInvoiceListItemDto(
    int Id,
    string Number,
    InvoiceType InvoiceType,
    InvoiceStatus Status,
    int SaleId,
    string SaleNumber,
    int CustomerId,
    string CustomerName,
    int PointOfSale,
    int SequentialNumber,
    DateTime IssuedAtUtc,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string? Cae,
    DateTime? CaeDueDateUtc,
    DateTime? LastSubmissionAtUtc);

public sealed record CommercialInvoiceDetailDto(
    int Id,
    string Number,
    InvoiceType InvoiceType,
    InvoiceStatus Status,
    int SaleId,
    string SaleNumber,
    int CustomerId,
    string CustomerName,
    int? FiscalConfigurationId,
    int PointOfSale,
    int SequentialNumber,
    DateTime IssuedAtUtc,
    decimal Subtotal,
    decimal TaxAmount,
    decimal OtherTaxesAmount,
    decimal Total,
    string CurrencyCode,
    string? FiscalStatusDetail,
    string? Cae,
    DateTime? CaeDueDateUtc,
    DateTime? LastSubmissionAtUtc,
    IReadOnlyList<InvoiceLineDto> Items,
    IReadOnlyList<FiscalSubmissionDto> Submissions,
    IReadOnlyList<LookupDto> RelatedDeliveryNotes,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? ModifiedByUserId,
    DateTime? ModifiedAtUtc);

public sealed record DeliveryNoteLineDto(
    int Id,
    int SaleItemId,
    int ProductId,
    int? ProductVariantId,
    string Description,
    string InternalCode,
    decimal QuantityOrdered,
    decimal QuantityDelivered,
    decimal RemainingQuantity,
    int SortOrder);

public sealed record DeliveryNoteListItemDto(
    int Id,
    string Number,
    DeliveryNoteStatus Status,
    int SaleId,
    string SaleNumber,
    int CustomerId,
    string CustomerName,
    int WarehouseId,
    string WarehouseName,
    int? CommercialInvoiceId,
    string? CommercialInvoiceNumber,
    DateTime DeliveredAtUtc,
    decimal TotalQuantity,
    decimal PendingQuantity);

public sealed record DeliveryNoteDetailDto(
    int Id,
    string Number,
    DeliveryNoteStatus Status,
    int SaleId,
    string SaleNumber,
    int CustomerId,
    string CustomerName,
    int WarehouseId,
    string WarehouseName,
    int? CommercialInvoiceId,
    string? CommercialInvoiceNumber,
    DateTime DeliveredAtUtc,
    string? Observations,
    decimal TotalQuantity,
    decimal PendingQuantity,
    IReadOnlyList<DeliveryNoteLineDto> Items,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? ModifiedByUserId,
    DateTime? ModifiedAtUtc);

public sealed class CreateInvoiceCommandDto
{
    public int SaleId { get; set; }
    public int? FiscalConfigurationId { get; set; }
    public InvoiceType? InvoiceType { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public decimal TaxRate { get; set; } = 0.21m;
}

public sealed class CreateDeliveryNoteCommandDto
{
    public int SaleId { get; set; }
    public int WarehouseId { get; set; }
    public int? CommercialInvoiceId { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string? Observations { get; set; }
    public List<CreateDeliveryNoteLineDto> Items { get; set; } = new();
}

public sealed class CreateDeliveryNoteLineDto
{
    public int SaleItemId { get; set; }
    public decimal QuantityDelivered { get; set; }
}

public sealed record ReportingMetricDto(string Title, string Value, string Hint, string Tone);
public sealed record PeriodAmountDto(DateTime Date, decimal Amount, int Count);
public sealed record TopProductReportDto(int ProductId, int? ProductVariantId, string Description, string InternalCode, decimal Quantity, decimal Revenue, int Documents);
public sealed record StockStatusReportDto(int ProductId, int? ProductVariantId, string Description, string InternalCode, string WarehouseName, decimal QuantityOnHand, decimal MinimumStock, bool IsCritical);
public sealed record OperationalReportDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ReportingMetricDto> Metrics,
    IReadOnlyList<PeriodAmountDto> SalesByDay,
    IReadOnlyList<PeriodAmountDto> PurchasesByDay,
    IReadOnlyList<TopProductReportDto> TopProducts,
    IReadOnlyList<StockStatusReportDto> Stock,
    decimal CustomerBalance,
    decimal SupplierBalance);

public sealed record Release6DashboardDto(
    decimal SalesVolume,
    decimal InvoicedAmount,
    int PendingInvoices,
    int PendingDeliveryNotes,
    int ActiveCustomers,
    int ActiveSuppliers,
    int CriticalStock,
    IReadOnlyList<ReportingMetricDto> Highlights);

public sealed record DocumentChangeLogDto(
    int Id,
    string EntityName,
    int EntityId,
    string DocumentNumber,
    string Action,
    string Summary,
    string? ChangedFields,
    string? RelatedDocumentNumber,
    string? UserName,
    DateTime ChangedAtUtc);

public sealed record DocumentTraceabilityDto(
    IReadOnlyList<DocumentChangeLogDto> History,
    IReadOnlyList<AccountAuditItemDto> AuditTrail);

public sealed record Release6SeedDataDto(
    IReadOnlyList<LookupDto> Sales,
    IReadOnlyList<LookupDto> Warehouses,
    IReadOnlyList<LookupDto> Invoices,
    FiscalConfigurationDto? FiscalConfiguration);
