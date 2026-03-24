using GestAI.Domain.Common;
using GestAI.Domain.Entities;

namespace GestAI.Domain.Entities.Commerce;

public sealed class DocumentSequence : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty;
    public int PointOfSale { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; }
}
