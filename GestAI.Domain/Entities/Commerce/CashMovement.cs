using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class CashMovement : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public int CashSessionId { get; set; }
    public CashSession CashSession { get; set; } = null!;
    public CashMovementDirection Direction { get; set; } = CashMovementDirection.In;
    public CashMovementOriginType OriginType { get; set; } = CashMovementOriginType.Manual;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public decimal Amount { get; set; }
    public string Concept { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public CustomerAccountMovement? CustomerAccountMovement { get; set; }
    public SupplierAccountMovement? SupplierAccountMovement { get; set; }
}
