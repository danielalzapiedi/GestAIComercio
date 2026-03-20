using GestAI.Domain.Common;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class Customer : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; } = CustomerType.Mixed;
    public bool IsActive { get; set; } = true;
}
