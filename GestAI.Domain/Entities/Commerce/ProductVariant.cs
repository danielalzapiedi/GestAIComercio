using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class ProductVariant : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string AttributesSummary { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; } = true;
}
