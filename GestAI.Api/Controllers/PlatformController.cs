using GestAI.Application.Commerce;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestAI.Api.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize(Roles = "SuperAdmin")]
public sealed class PlatformController(IMediator mediator) : ControllerBase
{
    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetTenantListQuery(search, page, pageSize), ct));

    [HttpGet("tenants/{tenantId:int}")]
    public async Task<IActionResult> GetTenant(int tenantId, CancellationToken ct)
        => Ok(await mediator.Send(new GetTenantByIdQuery(tenantId), ct));

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("tenants/{tenantId:int}")]
    public async Task<IActionResult> UpdateTenant(int tenantId, [FromBody] UpdateTenantCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { TenantId = tenantId }, ct));

    public sealed record ToggleBody(bool IsActive);

    [HttpPost("tenants/{tenantId:int}/status")]
    public async Task<IActionResult> ToggleTenant(int tenantId, [FromBody] ToggleBody body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleTenantStatusCommand(tenantId, body.IsActive), ct));
}
