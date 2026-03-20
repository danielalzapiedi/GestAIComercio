using GestAI.Application.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestAI.Api.Controllers;

[ApiController]
[Route("api/commerce")]
[Authorize]
public sealed class CommerceController(IMediator mediator) : ControllerBase
{
    public sealed record ToggleBody(bool IsActive);

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetBranchesQuery(search, isActive, page, pageSize), ct));

    [HttpGet("branches/{id:int}")]
    public async Task<IActionResult> GetBranch(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetBranchByIdQuery(id), ct));

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("branches/{id:int}")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("branches/{id:int}/status")]
    public async Task<IActionResult> ToggleBranch(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleBranchStatusCommand(id, body.IsActive), ct));

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses([FromQuery] int? branchId = null, [FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetWarehousesQuery(branchId, search, isActive, page, pageSize), ct));

    [HttpGet("warehouses/{id:int}")]
    public async Task<IActionResult> GetWarehouse(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetWarehouseByIdQuery(id), ct));

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("warehouses/{id:int}")]
    public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] UpdateWarehouseCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("warehouses/{id:int}/status")]
    public async Task<IActionResult> ToggleWarehouse(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleWarehouseStatusCommand(id, body.IsActive), ct));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetCategoriesQuery(search, isActive, page, pageSize), ct));

    [HttpGet("categories/tree")]
    public async Task<IActionResult> GetCategoryTree([FromQuery] bool? isActive = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetCategoryTreeQuery(isActive), ct));

    [HttpGet("categories/{id:int}")]
    public async Task<IActionResult> GetCategory(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetCategoryByIdQuery(id), ct));

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("categories/{id:int}/status")]
    public async Task<IActionResult> ToggleCategory(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleCategoryStatusCommand(id, body.IsActive), ct));

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? search = null, [FromQuery] int? categoryId = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetProductsQuery(search, categoryId, isActive, page, pageSize), ct));

    [HttpGet("products/seed")]
    public async Task<IActionResult> GetProductSeedData(CancellationToken ct)
        => Ok(await mediator.Send(new GetProductSeedDataQuery(), ct));

    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> GetProduct(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetProductByIdQuery(id), ct));

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("products/{id:int}/status")]
    public async Task<IActionResult> ToggleProduct(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleProductStatusCommand(id, body.IsActive), ct));

    [HttpGet("products/{productId:int}/variants")]
    public async Task<IActionResult> GetVariants(int productId, CancellationToken ct)
        => Ok(await mediator.Send(new GetProductVariantsQuery(productId), ct));

    [HttpGet("product-variants/{id:int}")]
    public async Task<IActionResult> GetVariant(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetProductVariantByIdQuery(id), ct));

    [HttpPost("products/{productId:int}/variants")]
    public async Task<IActionResult> CreateVariant(int productId, [FromBody] CreateProductVariantCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { ProductId = productId }, ct));

    [HttpPut("product-variants/{id:int}")]
    public async Task<IActionResult> UpdateVariant(int id, [FromBody] UpdateProductVariantCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("product-variants/{id:int}/status")]
    public async Task<IActionResult> ToggleVariant(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleProductVariantStatusCommand(id, body.IsActive), ct));

    [HttpGet("inventory/seed")]
    public async Task<IActionResult> GetInventorySeed(CancellationToken ct)
        => Ok(await mediator.Send(new GetInventorySeedDataQuery(), ct));

    [HttpGet("inventory/stocks")]
    public async Task<IActionResult> GetInventoryStocks([FromQuery] int? warehouseId = null, [FromQuery] int? productId = null, [FromQuery] int? productVariantId = null, [FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetInventoryOverviewQuery(warehouseId, productId, productVariantId, search), ct));

    [HttpGet("inventory/movements")]
    public async Task<IActionResult> GetInventoryMovements([FromQuery] int? warehouseId = null, [FromQuery] int? productId = null, [FromQuery] int? productVariantId = null, [FromQuery] StockMovementType? movementType = null, [FromQuery] int take = 30, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetStockMovementsQuery(warehouseId, productId, productVariantId, movementType, take), ct));

    [HttpPost("inventory/movements")]
    public async Task<IActionResult> CreateInventoryMovement([FromBody] RecordStockMovementCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("price-lists/seed")]
    public async Task<IActionResult> GetPriceListSeed(CancellationToken ct)
        => Ok(await mediator.Send(new GetPriceListSeedDataQuery(), ct));

    [HttpGet("price-lists")]
    public async Task<IActionResult> GetPriceLists([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetPriceListsQuery(search, isActive, page, pageSize), ct));

    [HttpGet("price-lists/{id:int}")]
    public async Task<IActionResult> GetPriceList(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPriceListByIdQuery(id), ct));

    [HttpGet("price-lists/{id:int}/items")]
    public async Task<IActionResult> GetPriceListItems(int id, [FromQuery] string? search = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetPriceListItemsQuery(id, search), ct));

    [HttpPost("price-lists")]
    public async Task<IActionResult> CreatePriceList([FromBody] CreatePriceListCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("price-lists/{id:int}")]
    public async Task<IActionResult> UpdatePriceList(int id, [FromBody] UpdatePriceListCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("price-lists/{id:int}/items")]
    public async Task<IActionResult> SetPriceListItem(int id, [FromBody] SetPriceListItemCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { PriceListId = id }, ct));

    [HttpPost("price-lists/{id:int}/bulk-update")]
    public async Task<IActionResult> ApplyPriceListAdjustment(int id, [FromBody] ApplyPriceListAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { PriceListId = id }, ct));

    [HttpPost("products/import/preview")]
    public async Task<IActionResult> PreviewProductImport([FromBody] PreviewProductImportCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("products/import")]
    public async Task<IActionResult> ApplyProductImport([FromBody] ApplyProductImportCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetCustomersQuery(search, isActive, page, pageSize), ct));

    [HttpGet("customers/{id:int}")]
    public async Task<IActionResult> GetCustomer(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetCustomerByIdQuery(id), ct));

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("customers/{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("customers/{id:int}/status")]
    public async Task<IActionResult> ToggleCustomer(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleCustomerStatusCommand(id, body.IsActive), ct));

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetSuppliersQuery(search, isActive, page, pageSize), ct));

    [HttpGet("suppliers/{id:int}")]
    public async Task<IActionResult> GetSupplier(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetSupplierByIdQuery(id), ct));

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("suppliers/{id:int}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("suppliers/{id:int}/status")]
    public async Task<IActionResult> ToggleSupplier(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleSupplierStatusCommand(id, body.IsActive), ct));
}
