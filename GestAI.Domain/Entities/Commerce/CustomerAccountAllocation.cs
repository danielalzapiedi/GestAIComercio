using GestAI.Domain.Common;
using GestAI.Domain.Entities;

namespace GestAI.Domain.Entities.Commerce;

public sealed class CustomerAccountAllocation : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int SourceMovementId { get; set; }
    public CustomerAccountMovement SourceMovement { get; set; } = null!;
    public int TargetMovementId { get; set; }
    public CustomerAccountMovement TargetMovement { get; set; } = null!;
    public DateTime AppliedAtUtc { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
