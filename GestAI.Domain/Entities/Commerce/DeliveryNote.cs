using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Domain.Entities.Commerce;

public sealed class DeliveryNote : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Number { get; set; } = string.Empty;
    public DeliveryNoteStatus Status { get; set; } = DeliveryNoteStatus.Draft;
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int? CommercialInvoiceId { get; set; }
    public CommercialInvoice? CommercialInvoice { get; set; }
    public DateTime DeliveredAtUtc { get; set; }
    public string? Observations { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public ICollection<DeliveryNoteItem> Items { get; set; } = new List<DeliveryNoteItem>();
}
