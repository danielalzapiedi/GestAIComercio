using GestAI.Application.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestAI.Api.Controllers;

[ApiController]
[Route("api/commerce/purchases")]
[Authorize]
public sealed class CommercePurchasesController(IMediator mediator) : ControllerBase
{
    [HttpGet("seed")]
    public async Task<IActionResult> GetPurchaseSeed(CancellationToken ct)
        => Ok(await mediator.Send(new GetPurchaseSeedDataQuery(), ct));

    [HttpGet]
    public async Task<IActionResult> GetPurchases([FromQuery] string? search = null, [FromQuery] PurchaseDocumentStatus? status = null, [FromQuery] int? supplierId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetPurchasesQuery(search, status, supplierId, page, pageSize), ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPurchase(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPurchaseByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDocumentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePurchase(int id, [FromBody] UpdatePurchaseDocumentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    [HttpPost("{id:int}/receipts")]
    public async Task<IActionResult> CreateGoodsReceipt(int id, [FromBody] CreateGoodsReceiptCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { PurchaseDocumentId = id }, ct));
}
