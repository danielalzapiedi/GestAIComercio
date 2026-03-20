using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class Warehouse : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public bool IsActive { get; set; } = true;
}
