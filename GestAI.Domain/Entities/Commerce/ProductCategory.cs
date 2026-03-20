using GestAI.Domain.Common;

namespace GestAI.Domain.Entities.Commerce;

public sealed class ProductCategory : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public ProductCategory? ParentCategory { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
