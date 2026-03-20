using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class Branch : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
