using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class CustomerAccountMovement : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public CustomerAccountMovementType MovementType { get; set; } = CustomerAccountMovementType.SaleDocument;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int? CashMovementId { get; set; }
    public CashMovement? CashMovement { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ICollection<CustomerAccountAllocation> AllocationsAsSource { get; set; } = new List<CustomerAccountAllocation>();
    public ICollection<CustomerAccountAllocation> AllocationsAsTarget { get; set; } = new List<CustomerAccountAllocation>();
}
