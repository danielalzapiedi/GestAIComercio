using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class DocumentChangeLog : Entity
{
    public int AccountId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? ChangedFields { get; set; }
    public string? RelatedDocumentNumber { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
