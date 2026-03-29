using GestAI.Application.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestAI.Api.Controllers;

[ApiController]
[Route("api/commerce")]
[Authorize]
public sealed partial class CommerceController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommerceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public sealed record ToggleBody(bool IsActive);

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBranchesQuery(search, isActive, page, pageSize), ct));

    [HttpGet("branches/{id:int}")]
    public async Task<IActionResult> GetBranch(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetBranchByIdQuery(id), ct));

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("branches/{id:int}")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { Id = id }, ct));

    [HttpPost("branches/{id:int}/status")]
    public async Task<IActionResult> ToggleBranch(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleBranchStatusCommand(id, body.IsActive), ct));

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses([FromQuery] int? branchId = null, [FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetWarehousesQuery(branchId, search, isActive, page, pageSize), ct));

    [HttpGet("warehouses/{id:int}")]
    public async Task<IActionResult> GetWarehouse(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetWarehouseByIdQuery(id), ct));

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("warehouses/{id:int}")]
    public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] UpdateWarehouseCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { Id = id }, ct));

    [HttpPost("warehouses/{id:int}/status")]
    public async Task<IActionResult> ToggleWarehouse(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleWarehouseStatusCommand(id, body.IsActive), ct));

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCustomersQuery(search, isActive, page, pageSize), ct));

    [HttpGet("customers/{id:int}")]
    public async Task<IActionResult> GetCustomer(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCustomerByIdQuery(id), ct));

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("customers/{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { Id = id }, ct));

    [HttpPost("customers/{id:int}/status")]
    public async Task<IActionResult> ToggleCustomer(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleCustomerStatusCommand(id, body.IsActive), ct));


    [HttpGet("release6/seed")]
    public async Task<IActionResult> GetRelease6Seed(CancellationToken ct)
        => Ok(await _mediator.Send(new GetRelease6SeedDataQuery(), ct));

    [HttpGet("fiscal/configuration")]
    public async Task<IActionResult> GetFiscalConfiguration(CancellationToken ct)
        => Ok(await _mediator.Send(new GetFiscalConfigurationQuery(), ct));

    [HttpPut("fiscal/configuration")]
    public async Task<IActionResult> UpsertFiscalConfiguration([FromBody] UpsertFiscalConfigurationCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("fiscal/credentials")]
    public async Task<IActionResult> UploadFiscalCredential([FromBody] UploadFiscalCredentialCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpGet("reports/operational")]
    public async Task<IActionResult> GetOperationalReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int top = 10, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetOperationalReportQuery(from, to, top), ct));

    [HttpGet("release6/dashboard")]
    public async Task<IActionResult> GetRelease6Dashboard(CancellationToken ct)
        => Ok(await _mediator.Send(new GetRelease6DashboardQuery(), ct));

    [HttpGet("traceability")]
    public async Task<IActionResult> GetTraceability([FromQuery] string? entityName = null, [FromQuery] string? search = null, [FromQuery] int take = 100, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDocumentTraceabilityQuery(entityName, search, take), ct));

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] string? search = null, [FromQuery] InvoiceStatus? status = null, [FromQuery] int? saleId = null, [FromQuery] int? customerId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetInvoicesQuery(search, status, saleId, customerId, page, pageSize), ct));

    [HttpGet("invoices/{id:int}")]
    public async Task<IActionResult> GetInvoice(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetInvoiceByIdQuery(id), ct));

    [HttpGet("invoices/{id:int}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInvoicePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("invoices/{id:int}/submit-arca")]
    public async Task<IActionResult> SubmitInvoiceToArca(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new SubmitInvoiceToArcaCommand(id), ct));

    [HttpGet("delivery-notes")]
    public async Task<IActionResult> GetDeliveryNotes([FromQuery] string? search = null, [FromQuery] DeliveryNoteStatus? status = null, [FromQuery] int? saleId = null, [FromQuery] int? warehouseId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDeliveryNotesQuery(search, status, saleId, warehouseId, page, pageSize), ct));

    [HttpGet("delivery-notes/{id:int}")]
    public async Task<IActionResult> GetDeliveryNote(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetDeliveryNoteByIdQuery(id), ct));

    [HttpGet("delivery-notes/{id:int}/pdf")]
    public async Task<IActionResult> GetDeliveryNotePdf(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDeliveryNotePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("delivery-notes")]
    public async Task<IActionResult> CreateDeliveryNote([FromBody] CreateDeliveryNoteCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSuppliersQuery(search, isActive, page, pageSize), ct));

    [HttpGet("suppliers/{id:int}")]
    public async Task<IActionResult> GetSupplier(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSupplierByIdQuery(id), ct));

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("suppliers/{id:int}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { Id = id }, ct));

    [HttpPost("suppliers/{id:int}/status")]
    public async Task<IActionResult> ToggleSupplier(int id, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleSupplierStatusCommand(id, body.IsActive), ct));


    [HttpGet("supplier-accounts")]
    public async Task<IActionResult> GetSupplierAccounts([FromQuery] string? search = null, [FromQuery] bool? onlyWithBalance = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSupplierAccountsQuery(search, onlyWithBalance), ct));

    [HttpGet("supplier-accounts/{supplierId:int}")]
    public async Task<IActionResult> GetSupplierAccount(int supplierId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSupplierAccountBySupplierIdQuery(supplierId), ct));


}
