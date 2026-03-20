using GestAI.Domain.Enums;

namespace GestAI.Application.Commerce;

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

public sealed record AllocationInputDto(int DocumentId, decimal Amount, string? Note);
