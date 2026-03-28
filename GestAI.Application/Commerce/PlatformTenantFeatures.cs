using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestAI.Application.Commerce;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerFirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.OwnerLastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress().MaximumLength(180);
        RuleFor(x => x.OwnerPassword).NotEmpty().MinimumLength(8);
    }
}

public sealed class GetTenantListQueryValidator : AbstractValidator<GetTenantListQuery>
{
    public GetTenantListQueryValidator()
    {
        CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
    }
}

public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class GetTenantListQueryHandler(IAppDbContext db, ICurrentUser current)
    : IRequestHandler<GetTenantListQuery, AppResult<PagedResult<PlatformTenantListItemDto>>>
{
    public async Task<AppResult<PagedResult<PlatformTenantListItemDto>>> Handle(GetTenantListQuery request, CancellationToken ct)
    {
        if (!current.IsInRole("SuperAdmin"))
            return AppResult<PagedResult<PlatformTenantListItemDto>>.Fail("forbidden", "Solo un super administrador puede ver tenants.");

        var query = db.Accounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.Users.Any(u => u.User.Email != null && u.User.Email.Contains(search)));
        }

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PlatformTenantListItemDto(
                x.Id,
                x.Name,
                x.IsActive,
                x.OwnerUserId,
                db.Users.Where(u => u.Id == x.OwnerUserId).Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefault() ?? string.Empty,
                db.Users.Where(u => u.Id == x.OwnerUserId).Select(u => u.Email ?? string.Empty).FirstOrDefault() ?? string.Empty,
                x.CreatedAtUtc,
                x.Users.Count(u => u.IsActive)))
            .ToListAsync(ct);

        return AppResult<PagedResult<PlatformTenantListItemDto>>.Ok(CommerceFeatureHelpers.ToPaged(items, total, page, pageSize));
    }
}

public sealed class GetTenantByIdQueryHandler(IAppDbContext db, ICurrentUser current)
    : IRequestHandler<GetTenantByIdQuery, AppResult<PlatformTenantDetailDto>>
{
    public async Task<AppResult<PlatformTenantDetailDto>> Handle(GetTenantByIdQuery request, CancellationToken ct)
    {
        if (!current.IsInRole("SuperAdmin")) return AppResult<PlatformTenantDetailDto>.Fail("forbidden", "Solo un super administrador puede ver tenants.");

        var item = await db.Accounts.AsNoTracking()
            .Where(x => x.Id == request.TenantId)
            .Select(x => new PlatformTenantDetailDto(
                x.Id,
                x.Name,
                x.IsActive,
                x.OwnerUserId,
                db.Users.Where(u => u.Id == x.OwnerUserId).Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefault() ?? string.Empty,
                db.Users.Where(u => u.Id == x.OwnerUserId).Select(u => u.Email ?? string.Empty).FirstOrDefault() ?? string.Empty,
                x.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);

        return item is null
            ? AppResult<PlatformTenantDetailDto>.Fail("not_found", "Tenant no encontrado.")
            : AppResult<PlatformTenantDetailDto>.Ok(item);
    }
}

public sealed class CreateTenantCommandHandler(IAppDbContext db, IIdentityService identity, IAuditService audit, ICurrentUser current)
    : IRequestHandler<CreateTenantCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateTenantCommand request, CancellationToken ct)
    {
        if (!current.IsInRole("SuperAdmin")) return AppResult<int>.Fail("forbidden", "Solo un super administrador puede crear tenants.");

        var owner = await identity.CreateUserIfNotExistsAsync(request.OwnerEmail.Trim(), request.OwnerPassword, ct, request.OwnerFirstName.Trim(), request.OwnerLastName.Trim(), true, null, 0);
        if (!owner.Success || string.IsNullOrWhiteSpace(owner.UserId))
            return AppResult<int>.Fail("identity_error", owner.Error ?? "No se pudo crear el dueño del tenant.");

        var account = new Account
        {
            Name = request.Name.Trim(),
            OwnerUserId = owner.UserId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        var defaultPlanId = await db.SaasPlanDefinitions
            .Where(x => x.Code == SaasPlanCode.Pro)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct)
            ?? await db.SaasPlanDefinitions.OrderBy(x => x.Id).Select(x => x.Id).FirstAsync(ct);

        db.AccountSubscriptionPlans.Add(new AccountSubscriptionPlan
        {
            AccountId = account.Id,
            PlanDefinitionId = defaultPlanId,
            IsActive = true,
            StartedAtUtc = DateTime.UtcNow
        });

        var existingMembership = await db.AccountUsers.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.UserId == owner.UserId, ct);
        if (existingMembership is null)
        {
            db.AccountUsers.Add(new AccountUser
            {
                AccountId = account.Id,
                UserId = owner.UserId,
                Role = InternalUserRole.Owner,
                IsActive = true,
                CanManageConfiguration = true,
                InvitedAtUtc = DateTime.UtcNow
            });
        }

        var ownerUser = await db.Users.FirstAsync(x => x.Id == owner.UserId, ct);
        if (ownerUser.DefaultAccountId == 0)
            ownerUser.DefaultAccountId = account.Id;

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(account.Id, null, "Account", account.Id, "created", $"Tenant creado: {account.Name}", ct);
        return AppResult<int>.Ok(account.Id);
    }
}

public sealed class UpdateTenantCommandHandler(IAppDbContext db, IAuditService audit, ICurrentUser current)
    : IRequestHandler<UpdateTenantCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateTenantCommand request, CancellationToken ct)
    {
        if (!current.IsInRole("SuperAdmin")) return AppResult.Fail("forbidden", "Solo un super administrador puede editar tenants.");
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.TenantId, ct);
        if (account is null) return AppResult.Fail("not_found", "Tenant no encontrado.");
        account.Name = request.Name.Trim();
        account.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(account.Id, null, "Account", account.Id, "updated", $"Tenant actualizado: {account.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleTenantStatusCommandHandler(IAppDbContext db, IAuditService audit, ICurrentUser current)
    : IRequestHandler<ToggleTenantStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleTenantStatusCommand request, CancellationToken ct)
    {
        if (!current.IsInRole("SuperAdmin")) return AppResult.Fail("forbidden", "Solo un super administrador puede cambiar el estado de tenants.");
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.TenantId, ct);
        if (account is null) return AppResult.Fail("not_found", "Tenant no encontrado.");
        account.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(account.Id, null, "Account", account.Id, request.IsActive ? "activated" : "deactivated", $"Tenant {(request.IsActive ? "activado" : "desactivado")}: {account.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
    }
}

public sealed class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
    }
}
