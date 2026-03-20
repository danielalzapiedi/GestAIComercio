using GestAI.Domain.Enums;

namespace GestAI.Application.Commerce;

public sealed record LookupDto(int Id, string Name);

public sealed record PlatformTenantListItemDto(
    int Id,
    string Name,
    bool IsActive,
    string OwnerUserId,
    string OwnerName,
    string OwnerEmail,
    DateTime CreatedAtUtc,
    int UsersCount);

public sealed record PlatformTenantDetailDto(
    int Id,
    string Name,
    bool IsActive,
    string OwnerUserId,
    string OwnerName,
    string OwnerEmail,
    DateTime CreatedAtUtc);

public sealed record BranchListItemDto(int Id, string Name, string Code, bool IsActive, int WarehousesCount, DateTime CreatedAtUtc);
public sealed record BranchDetailDto(int Id, string Name, string Code, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);

public sealed record WarehouseListItemDto(int Id, int BranchId, string BranchName, string Name, bool IsMain, bool IsActive, DateTime CreatedAtUtc);
public sealed record WarehouseDetailDto(int Id, int BranchId, string Name, bool IsMain, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);

public sealed record CategoryTreeItemDto(int Id, string Name, bool IsActive, int? ParentCategoryId, List<CategoryTreeItemDto> Children);
public sealed record CategoryListItemDto(int Id, string Name, int? ParentCategoryId, string? ParentCategoryName, bool IsActive, DateTime CreatedAtUtc);
public sealed record CategoryDetailDto(int Id, string Name, int? ParentCategoryId, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);

public sealed record ProductListItemDto(int Id, string Name, string InternalCode, string? Barcode, string CategoryName, string Brand, UnitOfMeasure UnitOfMeasure, decimal SalePrice, bool IsActive, int VariantsCount);
public sealed record ProductDetailDto(int Id, string Name, string InternalCode, string? Barcode, string Description, int CategoryId, string Brand, UnitOfMeasure UnitOfMeasure, decimal Cost, decimal SalePrice, decimal MinimumStock, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);

public sealed record ProductVariantListItemDto(int Id, int ProductId, string ProductName, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive);
public sealed record ProductVariantDetailDto(int Id, int ProductId, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);

public sealed record CustomerListItemDto(int Id, string Name, string? DocumentNumber, string Phone, string City, CustomerType CustomerType, bool IsActive);
public sealed record CustomerDetailDto(int Id, string Name, string? DocumentNumber, string Phone, string Address, string City, CustomerType CustomerType, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);

public sealed record SupplierListItemDto(int Id, string Name, string TaxId, string Phone, bool IsActive);
public sealed record SupplierDetailDto(int Id, string Name, string TaxId, string Phone, bool IsActive, string CreatedByUserId, DateTime CreatedAtUtc, string? ModifiedByUserId, DateTime? ModifiedAtUtc);

public sealed record ProductSeedDataDto(IReadOnlyList<LookupDto> Categories, IReadOnlyList<string> Brands, IReadOnlyList<UnitOfMeasure> Units);
