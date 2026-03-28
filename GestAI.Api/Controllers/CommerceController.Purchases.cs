using GestAI.Application.Commerce;
using GestAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestAI.Api.Controllers;

public sealed partial class CommerceController
{
    [HttpGet("purchases/seed")]
    public async Task<IActionResult> GetPurchaseSeed(CancellationToken ct)
        => Ok(await _mediator.Send(new GetPurchaseSeedDataQuery(), ct));

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases([FromQuery] string? search = null, [FromQuery] PurchaseDocumentStatus? status = null, [FromQuery] int? supplierId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetPurchasesQuery(search, status, supplierId, page, pageSize), ct));

    [HttpGet("purchases/{id:int}")]
    public async Task<IActionResult> GetPurchase(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPurchaseByIdQuery(id), ct));

    [HttpPost("purchases")]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDocumentCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("purchases/{id:int}")]
    public async Task<IActionResult> UpdatePurchase(int id, [FromBody] UpdatePurchaseDocumentCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { Id = id }, ct));

    [HttpPost("purchases/{id:int}/receipts")]
    public async Task<IActionResult> CreateGoodsReceipt(int id, [FromBody] CreateGoodsReceiptCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { PurchaseDocumentId = id }, ct));
}
