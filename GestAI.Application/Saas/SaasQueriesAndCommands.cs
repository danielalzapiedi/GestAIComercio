using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestAI.Application.Saas;

public sealed class GetCurrentUserAccessQueryHandler(IAppDbContext db, ICurrentUser current, IUserAccessService access)
    : IRequestHandler<GetCurrentUserAccessQuery, AppResult<CurrentUserAccessDto>>
{
    public async Task<AppResult<CurrentUserAccessDto>> Handle(GetCurrentUserAccessQuery request, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstAsync(x => x.Id == current.UserId, ct);
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        InternalUserRole? role = null;
        var isOwner = false;
        SaasPlanDefinition? plan = null;

        if (accountId.HasValue)
        {
            var accountData = await db.Accounts.AsNoTracking()
                .Where(x => x.Id == accountId.Value)
                .Select(x => new
                {
                    IsOwner = x.OwnerUserId == current.UserId,
                    Role = x.Users.Where(u => u.UserId == current.UserId && u.IsActive).Select(u => (InternalUserRole?)u.Role).FirstOrDefault(),
                    Plan = x.SubscriptionPlans.Where(p => p.IsActive).OrderByDescending(p => p.StartedAtUtc)
                        .Select(p => p.PlanDefinition)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            if (accountData is not null)
            {
                isOwner = accountData.IsOwner;
                role = isOwner ? InternalUserRole.Owner : accountData.Role;
                plan = accountData.Plan;
            }
        }

        var modules = Enum.GetValues<SaasModule>()
            .Select(module => new ModuleAccessDto(module, SaasPermissionMap.HasAccess(role, plan, module, isOwner)))
            .ToList();

        return AppResult<CurrentUserAccessDto>.Ok(new CurrentUserAccessDto(accountId, role, isOwner, user.IsActive, modules));
    }
}

public sealed class GetAccountSummaryQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetAccountSummaryQuery, AppResult<AccountSummaryDto>>
{
    public async Task<AppResult<AccountSummaryDto>> Handle(GetAccountSummaryQuery request, CancellationToken ct)
    {
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        if (!accountId.HasValue) return AppResult<AccountSummaryDto>.Fail("account_required", "No se encontró una cuenta activa.");

        var account = await db.Accounts.AsNoTracking()
            .Where(x => x.Id == accountId.Value)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.IsActive,
                x.CreatedAtUtc,
                Plan = x.SubscriptionPlans.Where(p => p.IsActive).OrderByDescending(p => p.StartedAtUtc)
                    .Select(p => new
                    {
                        p.StartedAtUtc,
                        p.IsActive,
                        p.PlanDefinition.Name,
                        p.PlanDefinition.Code,
                        p.PlanDefinition.MaxProperties,
                        p.PlanDefinition.MaxUnits,
                        p.PlanDefinition.MaxUsers,
                        p.PlanDefinition.IncludesReports,
                        p.PlanDefinition.IncludesOperations
                    }).FirstOrDefault(),
                UsersCount = x.Users.Count()
            })
            .FirstOrDefaultAsync(ct);

        if (account is null || account.Plan is null) return AppResult<AccountSummaryDto>.Fail("account_required", "No se encontró el resumen de cuenta.");

        return AppResult<AccountSummaryDto>.Ok(new AccountSummaryDto(
            account.Id,
            account.Name,
            account.IsActive,
            account.CreatedAtUtc,
            account.Plan.Name,
            account.Plan.Code,
            account.Plan.IsActive ? "Activo" : "Inactivo",
            account.Plan.StartedAtUtc,
            account.Plan.MaxProperties,
            account.Plan.MaxUnits,
            account.Plan.MaxUsers,
            account.Plan.IncludesReports,
            account.Plan.IncludesOperations,
            0,
            0,
            account.UsersCount));
    }
}

public sealed class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
}

public sealed class UpdateAccountCommandHandler(IAppDbContext db, IUserAccessService access, IAuditService audit)
    : IRequestHandler<UpdateAccountCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        if (!accountId.HasValue) return AppResult.Fail("account_required", "No se encontró una cuenta activa.");

        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId.Value, ct);
        if (account is null) return AppResult.Fail("not_found", "Cuenta no encontrada.");

        account.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(account.Id, null, "Account", account.Id, "updated", $"Cuenta actualizada: {account.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class GetAccountUsersQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetAccountUsersQuery, AppResult<List<AccountUserListItemDto>>>
{
    public async Task<AppResult<List<AccountUserListItemDto>>> Handle(GetAccountUsersQuery request, CancellationToken ct)
    {
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        if (!accountId.HasValue) return AppResult<List<AccountUserListItemDto>>.Fail("account_required", "No se encontró una cuenta activa.");

        var users = await db.AccountUsers.AsNoTracking()
            .Where(x => x.AccountId == accountId.Value)
            .OrderBy(x => x.User.Nombre)
            .ThenBy(x => x.User.Apellido)
            .Select(x => new AccountUserListItemDto(
                x.UserId,
                $"{x.User.Nombre} {x.User.Apellido}".Trim(),
                x.User.Email!,
                x.IsActive,
                x.Role,
                x.User.LastLoginAtUtc,
                x.InvitedAtUtc))
            .ToListAsync(ct);

        return AppResult<List<AccountUserListItemDto>>.Ok(users);
    }
}

public sealed class UpsertAccountUserCommandValidator : AbstractValidator<UpsertAccountUserCommand>
{
    public UpsertAccountUserCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(180);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8)
            .When(x => string.IsNullOrWhiteSpace(x.UserId));
    }
}

public sealed class UpsertAccountUserCommandHandler(IAppDbContext db, IUserAccessService access, ISaasPlanService planService, IIdentityService identity, IAuditService audit)
    : IRequestHandler<UpsertAccountUserCommand, AppResult<string>>
{
    public async Task<AppResult<string>> Handle(UpsertAccountUserCommand request, CancellationToken ct)
    {
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        if (!accountId.HasValue) return AppResult<string>.Fail("account_required", "No se encontró una cuenta activa.");

        if (request.UserId is null)
        {
            var validate = await planService.ValidateUserCreationAsync(accountId.Value, ct);
            if (!validate.Success) return AppResult<string>.Fail(validate.ErrorCode!, validate.Message!);

            var create = await identity.CreateUserIfNotExistsAsync(request.Email.Trim(), request.Password!, ct, request.Nombre.Trim(), request.Apellido.Trim(), request.IsActive, null, accountId.Value);
            if (!create.Success || string.IsNullOrWhiteSpace(create.UserId))
                return AppResult<string>.Fail("identity_error", create.Error ?? "No se pudo crear el usuario.");

            db.AccountUsers.Add(new AccountUser
            {
                AccountId = accountId.Value,
                UserId = create.UserId,
                Role = request.Role,
                IsActive = request.IsActive,
                CanManageConfiguration = request.Role is InternalUserRole.Admin or InternalUserRole.Owner,
                InvitedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(accountId.Value, null, "AccountUser", null, "created", $"Usuario creado: {request.Nombre} {request.Apellido} ({request.Email}) - rol {request.Role}", ct);
            return AppResult<string>.Ok(create.UserId);
        }

        var membership = await db.AccountUsers.Include(x => x.User).FirstOrDefaultAsync(x => x.AccountId == accountId.Value && x.UserId == request.UserId, ct);
        if (membership is null) return AppResult<string>.Fail("not_found", "Usuario no encontrado.");

        membership.Role = request.Role;
        membership.IsActive = request.IsActive;
        membership.CanManageConfiguration = request.Role is InternalUserRole.Admin or InternalUserRole.Owner;
        membership.User.Nombre = request.Nombre.Trim();
        membership.User.Apellido = request.Apellido.Trim();
        membership.User.Email = request.Email.Trim();
        membership.User.UserName = request.Email.Trim();
        membership.User.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
        membership.User.NormalizedUserName = request.Email.Trim().ToUpperInvariant();
        membership.User.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId.Value, null, "AccountUser", null, "updated", $"Usuario actualizado: {request.Nombre} {request.Apellido} ({request.Email}) - rol {request.Role} - activo: {(request.IsActive ? "sí" : "no")}", ct);
        return AppResult<string>.Ok(membership.UserId);
    }
}

public sealed class ToggleAccountUserStatusCommandHandler(IAppDbContext db, IUserAccessService access, IAuditService audit)
    : IRequestHandler<ToggleAccountUserStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleAccountUserStatusCommand request, CancellationToken ct)
    {
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        if (!accountId.HasValue) return AppResult.Fail("account_required", "No se encontró una cuenta activa.");
        if (!await access.HasModuleAccessAsync(accountId.Value, SaasModule.Users, ct))
            return AppResult.Fail("forbidden", "No tenés permisos para administrar usuarios.");

        var membership = await db.AccountUsers.Include(x => x.User).FirstOrDefaultAsync(x => x.AccountId == accountId.Value && x.UserId == request.UserId, ct);
        if (membership is null) return AppResult.Fail("not_found", "Usuario no encontrado.");

        membership.IsActive = request.IsActive;
        membership.User.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId.Value, null, "AccountUser", null, request.IsActive ? "activated" : "deactivated", $"Usuario {(request.IsActive ? "activado" : "desactivado")}: {membership.User.Nombre} {membership.User.Apellido}", ct);
        return AppResult.Ok();
    }
}

public sealed class GetAccountAuditQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetAccountAuditQuery, AppResult<List<AccountAuditItemDto>>>
{
    public async Task<AppResult<List<AccountAuditItemDto>>> Handle(GetAccountAuditQuery request, CancellationToken ct)
    {
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        if (!accountId.HasValue) return AppResult<List<AccountAuditItemDto>>.Fail("account_required", "No se encontró una cuenta activa.");

        var query = db.AuditLogs.AsNoTracking().Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(request.EntityName)) query = query.Where(x => x.EntityName == request.EntityName);
        if (!string.IsNullOrWhiteSpace(request.UserName)) query = query.Where(x => x.UserName != null && x.UserName.Contains(request.UserName));
        if (request.From.HasValue) query = query.Where(x => x.CreatedAtUtc >= request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (request.To.HasValue) query = query.Where(x => x.CreatedAtUtc < request.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var items = await query.OrderByDescending(x => x.CreatedAtUtc).Take(request.Take)
            .Select(x => new AccountAuditItemDto(x.CreatedAtUtc, x.EntityName, x.Action, x.Summary ?? string.Empty, x.UserName))
            .ToListAsync(ct);
        return AppResult<List<AccountAuditItemDto>>.Ok(items);
    }
}
