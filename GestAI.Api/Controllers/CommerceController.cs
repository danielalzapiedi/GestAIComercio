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


    [HttpGet("commercial/seed")]
    public async Task<IActionResult> GetCommercialSeed(CancellationToken ct)
        => Ok(await mediator.Send(new GetCommercialDocumentSeedDataQuery(), ct));

    [HttpGet("quotes")]
    public async Task<IActionResult> GetQuotes([FromQuery] string? search = null, [FromQuery] QuoteStatus? status = null, [FromQuery] int? customerId = null, [FromQuery] bool? onlyConvertible = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetQuotesQuery(search, status, customerId, onlyConvertible, page, pageSize), ct));

    [HttpGet("quotes/{id:int}")]
    public async Task<IActionResult> GetQuote(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetQuoteByIdQuery(id), ct));

    [HttpGet("quotes/{id:int}/pdf")]
    public async Task<IActionResult> GetQuotePdf(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetQuotePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("quotes")]
    public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("quotes/{id:int}")]
    public async Task<IActionResult> UpdateQuote(int id, [FromBody] UpdateQuoteCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("quotes/{id:int}/convert-to-sale")]
    public async Task<IActionResult> ConvertQuoteToSale(int id, [FromBody] ConvertQuoteToSaleCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { QuoteId = id }, ct));

    [HttpGet("sales")]
    public async Task<IActionResult> GetSales([FromQuery] string? search = null, [FromQuery] SaleStatus? status = null, [FromQuery] int? customerId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetSalesQuery(search, status, customerId, page, pageSize), ct));

    [HttpGet("sales/{id:int}")]
    public async Task<IActionResult> GetSale(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetSaleByIdQuery(id), ct));

    [HttpGet("sales/{id:int}/pdf")]
    public async Task<IActionResult> GetSalePdf(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSalePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("sales")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));


    [HttpGet("release6/seed")]
    public async Task<IActionResult> GetRelease6Seed(CancellationToken ct)
        => Ok(await mediator.Send(new GetRelease6SeedDataQuery(), ct));

    [HttpGet("fiscal/configuration")]
    public async Task<IActionResult> GetFiscalConfiguration(CancellationToken ct)
        => Ok(await mediator.Send(new GetFiscalConfigurationQuery(), ct));

    [HttpPut("fiscal/configuration")]
    public async Task<IActionResult> UpsertFiscalConfiguration([FromBody] UpsertFiscalConfigurationCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("fiscal/credentials")]
    public async Task<IActionResult> UploadFiscalCredential([FromBody] UploadFiscalCredentialCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("reports/operational")]
    public async Task<IActionResult> GetOperationalReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int top = 10, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetOperationalReportQuery(from, to, top), ct));

    [HttpGet("release6/dashboard")]
    public async Task<IActionResult> GetRelease6Dashboard(CancellationToken ct)
        => Ok(await mediator.Send(new GetRelease6DashboardQuery(), ct));

    [HttpGet("traceability")]
    public async Task<IActionResult> GetTraceability([FromQuery] string? entityName = null, [FromQuery] string? search = null, [FromQuery] int take = 100, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetDocumentTraceabilityQuery(entityName, search, take), ct));

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] string? search = null, [FromQuery] InvoiceStatus? status = null, [FromQuery] int? saleId = null, [FromQuery] int? customerId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetInvoicesQuery(search, status, saleId, customerId, page, pageSize), ct));

    [HttpGet("invoices/{id:int}")]
    public async Task<IActionResult> GetInvoice(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetInvoiceByIdQuery(id), ct));

    [HttpGet("invoices/{id:int}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoicePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("invoices/{id:int}/submit-arca")]
    public async Task<IActionResult> SubmitInvoiceToArca(int id, CancellationToken ct)
        => Ok(await mediator.Send(new SubmitInvoiceToArcaCommand(id), ct));

    [HttpGet("delivery-notes")]
    public async Task<IActionResult> GetDeliveryNotes([FromQuery] string? search = null, [FromQuery] DeliveryNoteStatus? status = null, [FromQuery] int? saleId = null, [FromQuery] int? warehouseId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetDeliveryNotesQuery(search, status, saleId, warehouseId, page, pageSize), ct));

    [HttpGet("delivery-notes/{id:int}")]
    public async Task<IActionResult> GetDeliveryNote(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetDeliveryNoteByIdQuery(id), ct));

    [HttpGet("delivery-notes/{id:int}/pdf")]
    public async Task<IActionResult> GetDeliveryNotePdf(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDeliveryNotePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("delivery-notes")]
    public async Task<IActionResult> CreateDeliveryNote([FromBody] CreateDeliveryNoteCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("sales/{id:int}")]
    public async Task<IActionResult> UpdateSale(int id, [FromBody] UpdateSaleCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("sales/quick")]
    public async Task<IActionResult> CreateQuickSale([FromBody] CreateQuickSaleCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

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


    [HttpGet("purchases/seed")]
    public async Task<IActionResult> GetPurchaseSeed(CancellationToken ct)
        => Ok(await mediator.Send(new GetPurchaseSeedDataQuery(), ct));

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases([FromQuery] string? search = null, [FromQuery] PurchaseDocumentStatus? status = null, [FromQuery] int? supplierId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetPurchasesQuery(search, status, supplierId, page, pageSize), ct));

    [HttpGet("purchases/{id:int}")]
    public async Task<IActionResult> GetPurchase(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPurchaseByIdQuery(id), ct));

    [HttpPost("purchases")]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDocumentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("purchases/{id:int}")]
    public async Task<IActionResult> UpdatePurchase(int id, [FromBody] UpdatePurchaseDocumentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("purchases/{id:int}/receipts")]
    public async Task<IActionResult> CreateGoodsReceipt(int id, [FromBody] CreateGoodsReceiptCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { PurchaseDocumentId = id }, ct));

    [HttpGet("supplier-accounts")]
    public async Task<IActionResult> GetSupplierAccounts([FromQuery] string? search = null, [FromQuery] bool? onlyWithBalance = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetSupplierAccountsQuery(search, onlyWithBalance), ct));

    [HttpGet("supplier-accounts/{supplierId:int}")]
    public async Task<IActionResult> GetSupplierAccount(int supplierId, CancellationToken ct)
        => Ok(await mediator.Send(new GetSupplierAccountBySupplierIdQuery(supplierId), ct));


    [HttpGet("cash")]
    public async Task<IActionResult> GetCashDashboard(CancellationToken ct)
        => Ok(await mediator.Send(new GetCashDashboardQuery(), ct));

    [HttpPost("cash/open")]
    public async Task<IActionResult> OpenCash([FromBody] OpenCashSessionCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("cash/close")]
    public async Task<IActionResult> CloseCash([FromBody] CloseCashSessionCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("cash/movements")]
    public async Task<IActionResult> CreateCashMovement([FromBody] CreateCashManualMovementCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("customer-collections")]
    public async Task<IActionResult> CreateCustomerCollection([FromBody] CreateCustomerCollectionCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPost("supplier-payments")]
    public async Task<IActionResult> CreateSupplierPayment([FromBody] CreateSupplierPaymentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("customer-current-accounts")]
    public async Task<IActionResult> GetCustomerCurrentAccounts([FromQuery] string? search = null, [FromQuery] bool? onlyWithBalance = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetCustomerCurrentAccountsQuery(search, onlyWithBalance), ct));

    [HttpGet("customer-current-accounts/{customerId:int}")]
    public async Task<IActionResult> GetCustomerCurrentAccount(int customerId, CancellationToken ct)
        => Ok(await mediator.Send(new GetCustomerCurrentAccountByCustomerIdQuery(customerId), ct));

    [HttpGet("supplier-current-accounts")]
    public async Task<IActionResult> GetSupplierCurrentAccounts([FromQuery] string? search = null, [FromQuery] bool? onlyWithBalance = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetSupplierCurrentAccountsQuery(search, onlyWithBalance), ct));

    [HttpGet("supplier-current-accounts/{supplierId:int}")]
    public async Task<IActionResult> GetSupplierCurrentAccountV2(int supplierId, CancellationToken ct)
        => Ok(await mediator.Send(new GetSupplierCurrentAccountBySupplierIdQuery(supplierId), ct));

}
