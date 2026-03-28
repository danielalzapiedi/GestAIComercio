using GestAI.Application.Common;
using GestAI.Domain.Enums;
using MediatR;

namespace GestAI.Application.Commerce;

public sealed record GetCategoriesQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<CategoryListItemDto>>>;
public sealed record GetCategoryTreeQuery(bool? IsActive = null) : IRequest<AppResult<List<CategoryTreeItemDto>>>;
public sealed record GetCategoryByIdQuery(int Id) : IRequest<AppResult<CategoryDetailDto>>;
public sealed record CreateCategoryCommand(string Name, int? ParentCategoryId, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateCategoryCommand(int Id, string Name, int? ParentCategoryId, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleCategoryStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetProductsQuery(string? Search = null, int? CategoryId = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<ProductListItemDto>>>;
public sealed record GetProductByIdQuery(int Id) : IRequest<AppResult<ProductDetailDto>>;
public sealed record GetProductSeedDataQuery : IRequest<AppResult<ProductSeedDataDto>>;
public sealed record CreateProductCommand(string Name, string InternalCode, string? Barcode, string Description, int CategoryId, string Brand, UnitOfMeasure UnitOfMeasure, decimal Cost, decimal SalePrice, decimal MinimumStock, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateProductCommand(int Id, string Name, string InternalCode, string? Barcode, string Description, int CategoryId, string Brand, UnitOfMeasure UnitOfMeasure, decimal Cost, decimal SalePrice, decimal MinimumStock, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleProductStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetProductVariantsQuery(int ProductId) : IRequest<AppResult<List<ProductVariantListItemDto>>>;
public sealed record GetProductVariantByIdQuery(int Id) : IRequest<AppResult<ProductVariantDetailDto>>;
public sealed record CreateProductVariantCommand(int ProductId, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateProductVariantCommand(int Id, int ProductId, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleProductVariantStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;
