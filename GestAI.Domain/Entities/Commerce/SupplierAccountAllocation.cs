using GestAI.Domain.Common;
using GestAI.Domain.Entities;

namespace GestAI.Domain.Entities.Commerce;

public sealed class SupplierAccountAllocation : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int SourceMovementId { get; set; }
    public SupplierAccountMovement SourceMovement { get; set; } = null!;
    public int TargetMovementId { get; set; }
    public SupplierAccountMovement TargetMovement { get; set; } = null!;
    public DateTime AppliedAtUtc { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
