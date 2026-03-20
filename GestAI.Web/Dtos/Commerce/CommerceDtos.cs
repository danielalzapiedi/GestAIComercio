namespace GestAI.Web.Dtos;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public enum UnitOfMeasure
{
    Unit = 0,
    Kilogram = 1,
    Meter = 2,
    SquareMeter = 3,
    CubicMeter = 4,
    Liter = 5,
    Bag = 6,
    Bundle = 7
}

public enum CustomerType
{
    Consumer = 0,
    Company = 1,
    Mixed = 2
}

public sealed record LookupDto(int Id, string Name);
public sealed record PlatformTenantListItemDto(int Id, string Name, bool IsActive, string OwnerUserId, string OwnerName, string OwnerEmail, DateTime CreatedAtUtc, int UsersCount);
public sealed record PlatformTenantDetailDto(int Id, string Name, bool IsActive, string OwnerUserId, string OwnerName, string OwnerEmail, DateTime CreatedAtUtc);
public sealed class CreateTenantCommand
{
    public string Name { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
}

public sealed class UpdateTenantCommand
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed record BranchListItemDto(int Id, string Name, string Code, bool IsActive, int WarehousesCount, DateTime CreatedAtUtc);
public sealed record BranchDetailDto(int Id, string Name, string Code, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed class BranchUpsertCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed record WarehouseListItemDto(int Id, int BranchId, string BranchName, string Name, bool IsMain, bool IsActive, DateTime CreatedAtUtc);
public sealed record WarehouseDetailDto(int Id, int BranchId, string Name, bool IsMain, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed class WarehouseUpsertCommand
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record CategoryTreeItemDto(int Id, string Name, bool IsActive, int? ParentCategoryId, List<CategoryTreeItemDto> Children);
public sealed record CategoryListItemDto(int Id, string Name, int? ParentCategoryId, string? ParentCategoryName, bool IsActive, DateTime CreatedAtUtc);
public sealed record CategoryDetailDto(int Id, string Name, int? ParentCategoryId, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed class CategoryUpsertCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record ProductListItemDto(int Id, string Name, string InternalCode, string? Barcode, string CategoryName, string Brand, UnitOfMeasure UnitOfMeasure, decimal SalePrice, bool IsActive, int VariantsCount);
public sealed record ProductDetailDto(int Id, string Name, string InternalCode, string? Barcode, string Description, int CategoryId, string Brand, UnitOfMeasure UnitOfMeasure, decimal Cost, decimal SalePrice, decimal MinimumStock, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed class ProductUpsertCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public decimal Cost { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record ProductVariantListItemDto(int Id, int ProductId, string ProductName, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive);
public sealed record ProductVariantDetailDto(int Id, int ProductId, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed class ProductVariantUpsertCommand
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string AttributesSummary { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record CustomerListItemDto(int Id, string Name, string? DocumentNumber, string Phone, string City, CustomerType CustomerType, bool IsActive);
public sealed record CustomerDetailDto(int Id, string Name, string? DocumentNumber, string Phone, string Address, string City, CustomerType CustomerType, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed class CustomerUpsertCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record SupplierListItemDto(int Id, string Name, string TaxId, string Phone, bool IsActive);
public sealed record SupplierDetailDto(int Id, string Name, string TaxId, string Phone, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);
public sealed class SupplierUpsertCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed record ProductSeedDataDto(IReadOnlyList<LookupDto> Categories, IReadOnlyList<string> Brands, IReadOnlyList<UnitOfMeasure> Units);
