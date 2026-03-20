using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class PurchaseDocument : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Number { get; set; } = string.Empty;
    public PurchaseDocumentType DocumentType { get; set; } = PurchaseDocumentType.PurchaseDocument;
    public PurchaseDocumentStatus Status { get; set; } = PurchaseDocumentStatus.Draft;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime IssuedAtUtc { get; set; }
    public string? SupplierDocumentNumber { get; set; }
    public string? Observations { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public ICollection<PurchaseDocumentItem> Items { get; set; } = new List<PurchaseDocumentItem>();
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
    public ICollection<SupplierAccountMovement> SupplierAccountMovements { get; set; } = new List<SupplierAccountMovement>();
}
