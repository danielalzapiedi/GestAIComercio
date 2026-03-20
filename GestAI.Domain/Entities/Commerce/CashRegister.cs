using GestAI.Domain.Common;
using GestAI.Domain.Entities;

namespace GestAI.Domain.Entities.Commerce;

public sealed class CashRegister : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public ICollection<CashSession> Sessions { get; set; } = new List<CashSession>();
}
