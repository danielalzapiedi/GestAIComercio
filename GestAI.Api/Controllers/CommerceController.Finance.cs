using GestAI.Application.Commerce;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GestAI.Api.Controllers;

public sealed partial class CommerceController : ControllerBase
{
    [HttpGet("cash")]
    public async Task<IActionResult> GetCashDashboard(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCashDashboardQuery(), ct));

    [HttpPost("cash/open")]
    public async Task<IActionResult> OpenCash([FromBody] OpenCashSessionCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("cash/close")]
    public async Task<IActionResult> CloseCash([FromBody] CloseCashSessionCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("cash/movements")]
    public async Task<IActionResult> CreateCashMovement([FromBody] CreateCashManualMovementCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("customer-collections")]
    public async Task<IActionResult> CreateCustomerCollection([FromBody] CreateCustomerCollectionCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("supplier-payments")]
    public async Task<IActionResult> CreateSupplierPayment([FromBody] CreateSupplierPaymentCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpGet("customer-current-accounts")]
    public async Task<IActionResult> GetCustomerCurrentAccounts([FromQuery] string? search = null, [FromQuery] bool? onlyWithBalance = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCustomerCurrentAccountsQuery(search, onlyWithBalance), ct));

    [HttpGet("customer-current-accounts/{customerId:int}")]
    public async Task<IActionResult> GetCustomerCurrentAccount(int customerId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCustomerCurrentAccountByCustomerIdQuery(customerId), ct));

    [HttpGet("supplier-current-accounts")]
    public async Task<IActionResult> GetSupplierCurrentAccounts([FromQuery] string? search = null, [FromQuery] bool? onlyWithBalance = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSupplierCurrentAccountsQuery(search, onlyWithBalance), ct));

    [HttpGet("supplier-current-accounts/{supplierId:int}")]
    public async Task<IActionResult> GetSupplierCurrentAccountV2(int supplierId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSupplierCurrentAccountBySupplierIdQuery(supplierId), ct));
}
