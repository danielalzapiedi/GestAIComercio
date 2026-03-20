using GestAI.Domain.Common;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class PriceList : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public PriceListBaseMode BaseMode { get; set; }
    public PriceListTargetType TargetType { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}
