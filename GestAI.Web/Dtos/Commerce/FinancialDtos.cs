namespace GestAI.Web.Dtos;

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

public sealed record CashDashboardDto(
    CashRegisterSummaryDto Register,
    CashSessionSummaryDto? OpenSession,
    decimal CurrentBalance,
    decimal TotalIn,
    decimal TotalOut,
    IReadOnlyList<CashMovementListItemDto> RecentMovements);

public sealed record CashRegisterSummaryDto(int Id, string Name, string Code, bool IsDefault, bool IsActive, int? BranchId, string? BranchName);

public sealed record CashSessionSummaryDto(
    int Id,
    CashSessionStatus Status,
    DateTime OpenedAtUtc,
    string OpenedByUserId,
    decimal OpeningBalance,
    decimal CurrentBalance,
    decimal TotalIn,
    decimal TotalOut,
    DateTime? ClosedAtUtc,
    decimal? ClosingBalanceExpected,
    decimal? ClosingBalanceDeclared,
    string? Note);

public sealed record CashMovementListItemDto(
    int Id,
    CashMovementDirection Direction,
    CashMovementOriginType OriginType,
    PaymentMethod PaymentMethod,
    string ReferenceNumber,
    DateTime OccurredAtUtc,
    decimal Amount,
    string Concept,
    string? Observations,
    int? CustomerId,
    string? CustomerName,
    int? SupplierId,
    string? SupplierName);

public sealed record CurrentAccountListItemDto(
    int PartyId,
    string PartyName,
    string? TaxIdentifier,
    bool IsActive,
    decimal Balance,
    decimal Debit,
    decimal Credit,
    int OpenDocumentsCount,
    DateTime? LastMovementAtUtc);

public sealed record CurrentAccountSummaryDto(
    int PartyId,
    string PartyName,
    string? TaxIdentifier,
    decimal Balance,
    decimal Debit,
    decimal Credit,
    decimal PendingDocumentsAmount,
    int OpenDocumentsCount,
    DateTime? LastMovementAtUtc,
    IReadOnlyList<CurrentAccountMovementDto> Movements,
    IReadOnlyList<PendingDocumentDto> PendingDocuments,
    IReadOnlyList<CurrentAccountAllocationDto> RecentAllocations);

public sealed record CurrentAccountMovementDto(
    int Id,
    string MovementType,
    PaymentMethod PaymentMethod,
    string ReferenceNumber,
    string Description,
    DateTime IssuedAtUtc,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal AppliedAmount,
    decimal PendingAmount,
    decimal BalanceImpact,
    int? DocumentId,
    string? Note);

public sealed record PendingDocumentDto(
    int DocumentId,
    string ReferenceNumber,
    DateTime IssuedAtUtc,
    decimal Total,
    decimal AppliedAmount,
    decimal PendingAmount,
    string Description);

public sealed record CurrentAccountAllocationDto(
    int Id,
    DateTime AppliedAtUtc,
    decimal Amount,
    string SourceReference,
    string TargetReference,
    string? Note);

public sealed class AllocationInputDto
{
    public int DocumentId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public sealed class OpenCashSessionCommand
{
    public decimal OpeningBalance { get; set; }
    public string? Note { get; set; }
}

public sealed class CloseCashSessionCommand
{
    public decimal ClosingBalanceDeclared { get; set; }
    public string? Note { get; set; }
}

public sealed class CreateCashManualMovementCommand
{
    public CashMovementDirection Direction { get; set; } = CashMovementDirection.In;
    public decimal Amount { get; set; }
    public string Concept { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Observations { get; set; }
}

public sealed class CreateCustomerCollectionCommand
{
    public int CustomerId { get; set; }
    public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public bool ImpactCash { get; set; } = true;
    public string Concept { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public List<AllocationInputDto> Allocations { get; set; } = new();
}

public sealed class CreateSupplierPaymentCommand
{
    public int SupplierId { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public bool ImpactCash { get; set; } = true;
    public string Concept { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public List<AllocationInputDto> Allocations { get; set; } = new();
}
