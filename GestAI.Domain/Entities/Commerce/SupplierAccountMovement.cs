using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class SupplierAccountMovement : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public SupplierAccountMovementType MovementType { get; set; } = SupplierAccountMovementType.PurchaseDocument;
    public int? PurchaseDocumentId { get; set; }
    public PurchaseDocument? PurchaseDocument { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Note { get; set; }
}
