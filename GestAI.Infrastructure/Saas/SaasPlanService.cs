using GestAI.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GestAI.Infrastructure.Saas;

public sealed class SaasPlanService(IAppDbContext db) : ISaasPlanService
{
    public Task<(bool Success, string? ErrorCode, string? Message)> ValidateUserCreationAsync(int accountId, CancellationToken ct)
        => ValidateAsync(accountId, ct, async plan =>
        {
            var count = await db.AccountUsers.AsNoTracking().CountAsync(x => x.AccountId == accountId, ct);
            return count < plan.MaxUsers ? null : ("plan_user_limit", $"Tu plan permite hasta {plan.MaxUsers} usuarios.");
        });

    private async Task<(bool Success, string? ErrorCode, string? Message)> ValidateAsync(int accountId, CancellationToken ct, Func<dynamic, Task<(string Code, string Message)?>> validator)
    {
        var plan = await db.AccountSubscriptionPlans.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.IsActive)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => new { x.PlanDefinition.MaxUsers })
            .FirstOrDefaultAsync(ct);

        if (plan is null) return (false, "plan_required", "La cuenta no tiene un plan activo.");
        var error = await validator(plan);
        return error is null ? (true, null, null) : (false, error.Value.Code, error.Value.Message);
    }
}
