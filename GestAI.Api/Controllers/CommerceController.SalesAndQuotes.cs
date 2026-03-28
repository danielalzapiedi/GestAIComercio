using GestAI.Application.Commerce;
using GestAI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestAI.Api.Controllers;

public sealed partial class CommerceController
{
    [HttpGet("commercial/seed")]
    public async Task<IActionResult> GetCommercialSeed(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCommercialDocumentSeedDataQuery(), ct));

    [HttpGet("quotes")]
    public async Task<IActionResult> GetQuotes([FromQuery] string? search = null, [FromQuery] QuoteStatus? status = null, [FromQuery] int? customerId = null, [FromQuery] bool? onlyConvertible = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetQuotesQuery(search, status, customerId, onlyConvertible, page, pageSize), ct));

    [HttpGet("quotes/{id:int}")]
    public async Task<IActionResult> GetQuote(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetQuoteByIdQuery(id), ct));

    [HttpGet("quotes/{id:int}/pdf")]
    public async Task<IActionResult> GetQuotePdf(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetQuotePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("quotes")]
    public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("quotes/{id:int}")]
    public async Task<IActionResult> UpdateQuote(int id, [FromBody] UpdateQuoteCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { Id = id }, ct));

    [HttpPost("quotes/{id:int}/convert-to-sale")]
    public async Task<IActionResult> ConvertQuoteToSale(int id, [FromBody] ConvertQuoteToSaleCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { QuoteId = id }, ct));

    [HttpGet("sales")]
    public async Task<IActionResult> GetSales([FromQuery] string? search = null, [FromQuery] SaleStatus? status = null, [FromQuery] int? customerId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSalesQuery(search, status, customerId, page, pageSize), ct));

    [HttpGet("sales/{id:int}")]
    public async Task<IActionResult> GetSale(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSaleByIdQuery(id), ct));

    [HttpGet("sales/{id:int}/pdf")]
    public async Task<IActionResult> GetSalePdf(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSalePdfQuery(id), ct);
        if (!result.Success || result.Data is null)
            return result.ErrorCode == "not_found"
                ? NotFound(new { result.ErrorCode, result.Message })
                : BadRequest(new { result.ErrorCode, result.Message });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("sales")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("sales/{id:int}")]
    public async Task<IActionResult> UpdateSale(int id, [FromBody] UpdateSaleCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command with { Id = id }, ct));

    [HttpPost("sales/quick")]
    public async Task<IActionResult> CreateQuickSale([FromBody] CreateQuickSaleCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));
}
