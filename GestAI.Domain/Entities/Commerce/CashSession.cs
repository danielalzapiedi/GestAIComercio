using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class CashSession : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;
    public DateTime OpenedAtUtc { get; set; }
    public string OpenedByUserId { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedByUserId { get; set; }
    public decimal? ClosingBalanceExpected { get; set; }
    public decimal? ClosingBalanceDeclared { get; set; }
    public string? Note { get; set; }
    public ICollection<CashMovement> Movements { get; set; } = new List<CashMovement>();
}
