using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GestAI.Application.Commerce;

public sealed record GetTenantListQuery(string? Search = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<PlatformTenantListItemDto>>>;
public sealed record GetTenantByIdQuery(int TenantId) : IRequest<AppResult<PlatformTenantDetailDto>>;
public sealed record CreateTenantCommand(string Name, string OwnerFirstName, string OwnerLastName, string OwnerEmail, string OwnerPassword) : IRequest<AppResult<int>>;
public sealed record UpdateTenantCommand(int TenantId, string Name, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleTenantStatusCommand(int TenantId, bool IsActive) : IRequest<AppResult>;

public sealed record GetBranchesQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<BranchListItemDto>>>;
public sealed record GetBranchByIdQuery(int Id) : IRequest<AppResult<BranchDetailDto>>;
public sealed record CreateBranchCommand(string Name, string Code, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateBranchCommand(int Id, string Name, string Code, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleBranchStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetWarehousesQuery(int? BranchId = null, string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<WarehouseListItemDto>>>;
public sealed record GetWarehouseByIdQuery(int Id) : IRequest<AppResult<WarehouseDetailDto>>;
public sealed record CreateWarehouseCommand(int BranchId, string Name, bool IsMain, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateWarehouseCommand(int Id, int BranchId, string Name, bool IsMain, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleWarehouseStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetCategoriesQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<CategoryListItemDto>>>;
public sealed record GetCategoryTreeQuery(bool? IsActive = null) : IRequest<AppResult<List<CategoryTreeItemDto>>>;
public sealed record GetCategoryByIdQuery(int Id) : IRequest<AppResult<CategoryDetailDto>>;
public sealed record CreateCategoryCommand(string Name, int? ParentCategoryId, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateCategoryCommand(int Id, string Name, int? ParentCategoryId, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleCategoryStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetProductsQuery(string? Search = null, int? CategoryId = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<ProductListItemDto>>>;
public sealed record GetProductByIdQuery(int Id) : IRequest<AppResult<ProductDetailDto>>;
public sealed record GetProductSeedDataQuery : IRequest<AppResult<ProductSeedDataDto>>;
public sealed record CreateProductCommand(string Name, string InternalCode, string? Barcode, string Description, int CategoryId, string Brand, UnitOfMeasure UnitOfMeasure, decimal Cost, decimal SalePrice, decimal MinimumStock, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateProductCommand(int Id, string Name, string InternalCode, string? Barcode, string Description, int CategoryId, string Brand, UnitOfMeasure UnitOfMeasure, decimal Cost, decimal SalePrice, decimal MinimumStock, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleProductStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetProductVariantsQuery(int ProductId) : IRequest<AppResult<List<ProductVariantListItemDto>>>;
public sealed record GetProductVariantByIdQuery(int Id) : IRequest<AppResult<ProductVariantDetailDto>>;
public sealed record CreateProductVariantCommand(int ProductId, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateProductVariantCommand(int Id, int ProductId, string Name, string InternalCode, string? Barcode, string AttributesSummary, decimal Cost, decimal SalePrice, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleProductVariantStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetCustomersQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<CustomerListItemDto>>>;
public sealed record GetCustomerByIdQuery(int Id) : IRequest<AppResult<CustomerDetailDto>>;
public sealed record CreateCustomerCommand(string Name, string? DocumentNumber, string Phone, string Address, string City, CustomerType CustomerType, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateCustomerCommand(int Id, string Name, string? DocumentNumber, string Phone, string Address, string City, CustomerType CustomerType, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleCustomerStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

public sealed record GetSuppliersQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<SupplierListItemDto>>>;
public sealed record GetSupplierByIdQuery(int Id) : IRequest<AppResult<SupplierDetailDto>>;
public sealed record CreateSupplierCommand(string Name, string TaxId, string Phone, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdateSupplierCommand(int Id, string Name, string TaxId, string Phone, bool IsActive) : IRequest<AppResult>;
public sealed record ToggleSupplierStatusCommand(int Id, bool IsActive) : IRequest<AppResult>;

file static class CommerceFeatureHelpers
{
    public const int MaxPageSize = 100;

    public static async Task<int?> GetRequiredAccountIdAsync(IUserAccessService access, CancellationToken ct)
        => await access.GetCurrentAccountIdAsync(ct);

    public static void TouchCreate(AuditableEntity entity, ICurrentUser current)
    {
        entity.CreatedAtUtc = DateTime.UtcNow;
        entity.CreatedByUserId = current.UserId;
        entity.ModifiedAtUtc = null;
        entity.ModifiedByUserId = null;
    }

    public static void TouchUpdate(AuditableEntity entity, ICurrentUser current)
    {
        entity.ModifiedAtUtc = DateTime.UtcNow;
        entity.ModifiedByUserId = current.UserId;
    }

    public static IQueryable<T> ApplySearch<T>(IQueryable<T> query, string? search, Func<string, IQueryable<T>, IQueryable<T>> apply)
        => string.IsNullOrWhiteSpace(search) ? query : apply(search.Trim(), query);

    public static PagedResult<T> ToPaged<T>(IReadOnlyList<T> items, int total, int page, int pageSize)
        => new(items, total, page, pageSize);

    public static void AddPagingRules<T>(AbstractValidator<T> validator, Expression<Func<T, int>> pageSelector, Expression<Func<T, int>> pageSizeSelector)
    {
        validator.RuleFor(pageSelector).GreaterThanOrEqualTo(1);
        validator.RuleFor(pageSizeSelector).InclusiveBetween(1, MaxPageSize);
    }

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireModuleAccessAsync(IUserAccessService access, SaasModule module, CancellationToken ct)
    {
        var accountId = await access.GetCurrentAccountIdAsync(ct);
        if (!accountId.HasValue)
            return (false, 0, "account_required", "No se encontró una cuenta activa.");

        if (!await access.HasModuleAccessAsync(accountId.Value, module, ct))
            return (false, accountId.Value, "forbidden", "No tiene permisos para acceder a este módulo.");

        return (true, accountId.Value, string.Empty, string.Empty);
    }

    public static async Task<bool> CreatesCategoryCycleAsync(IAppDbContext db, int accountId, int categoryId, int? parentCategoryId, CancellationToken ct)
    {
        if (!parentCategoryId.HasValue) return false;

        var visited = new HashSet<int>();
        var currentParentId = parentCategoryId;
        while (currentParentId.HasValue)
        {
            if (!visited.Add(currentParentId.Value)) return true;
            if (currentParentId.Value == categoryId) return true;

            currentParentId = await db.ProductCategories.AsNoTracking()
                .Where(x => x.AccountId == accountId && x.Id == currentParentId.Value)
                .Select(x => x.ParentCategoryId)
                .FirstOrDefaultAsync(ct);
        }

        return false;
    }
}

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

public sealed class GetBranchesQueryValidator : AbstractValidator<GetBranchesQuery>
{
    public GetBranchesQueryValidator()
    {
        CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
    }
}

public sealed class GetBranchesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetBranchesQuery, AppResult<PagedResult<BranchListItemDto>>>
{
    public async Task<AppResult<PagedResult<BranchListItemDto>>> Handle(GetBranchesQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Branches, ct);
        if (!scope.Success) return AppResult<PagedResult<BranchListItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;

        var query = db.Branches.AsNoTracking().Where(x => x.AccountId == accountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        }
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BranchListItemDto(x.Id, x.Name, x.Code, x.IsActive, x.Warehouses.Count(w => w.IsActive), x.CreatedAtUtc))
            .ToListAsync(ct);

        return AppResult<PagedResult<BranchListItemDto>>.Ok(new PagedResult<BranchListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetBranchByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetBranchByIdQuery, AppResult<BranchDetailDto>>
{
    public async Task<AppResult<BranchDetailDto>> Handle(GetBranchByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Branches, ct);
        if (!scope.Success) return AppResult<BranchDetailDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;

        var item = await db.Branches.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new BranchDetailDto(x.Id, x.Name, x.Code, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);

        return item is null ? AppResult<BranchDetailDto>.Fail("not_found", "Sucursal no encontrada.") : AppResult<BranchDetailDto>.Ok(item);
    }
}

public sealed class CreateBranchCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateBranchCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateBranchCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Branches, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (await db.Branches.AnyAsync(x => x.AccountId == accountId && x.Code == request.Code.Trim(), ct))
            return AppResult<int>.Fail("duplicate_code", "Ya existe una sucursal con ese código.");

        var entity = new Branch
        {
            AccountId = accountId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.Branches.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Branch", entity.Id, "created", $"Sucursal creada: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateBranchCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateBranchCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateBranchCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Branches, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Branches.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Sucursal no encontrada.");
        if (await db.Branches.AnyAsync(x => x.AccountId == accountId && x.Code == request.Code.Trim() && x.Id != request.Id, ct))
            return AppResult.Fail("duplicate_code", "Ya existe otra sucursal con ese código.");
        entity.Name = request.Name.Trim();
        entity.Code = request.Code.Trim();
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Branch", entity.Id, "updated", $"Sucursal actualizada: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleBranchStatusCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ToggleBranchStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleBranchStatusCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Branches, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Branches.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Sucursal no encontrada.");
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Branch", entity.Id, request.IsActive ? "activated" : "deactivated", $"Sucursal {(request.IsActive ? "activada" : "desactivada")}: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class GetWarehousesQueryValidator : AbstractValidator<GetWarehousesQuery>
{
    public GetWarehousesQueryValidator()
    {
        CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
    }
}

public sealed class GetWarehousesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetWarehousesQuery, AppResult<PagedResult<WarehouseListItemDto>>>
{
    public async Task<AppResult<PagedResult<WarehouseListItemDto>>> Handle(GetWarehousesQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Warehouses, ct);
        if (!scope.Success) return AppResult<PagedResult<WarehouseListItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var query = db.Warehouses.AsNoTracking().Where(x => x.AccountId == accountId);
        if (request.BranchId.HasValue) query = query.Where(x => x.BranchId == request.BranchId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.Branch.Name.Contains(search));
        }
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Branch.Name).ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WarehouseListItemDto(x.Id, x.BranchId, x.Branch.Name, x.Name, x.IsMain, x.IsActive, x.CreatedAtUtc))
            .ToListAsync(ct);
        return AppResult<PagedResult<WarehouseListItemDto>>.Ok(new PagedResult<WarehouseListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetWarehouseByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetWarehouseByIdQuery, AppResult<WarehouseDetailDto>>
{
    public async Task<AppResult<WarehouseDetailDto>> Handle(GetWarehouseByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Warehouses, ct);
        if (!scope.Success) return AppResult<WarehouseDetailDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var item = await db.Warehouses.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new WarehouseDetailDto(x.Id, x.BranchId, x.Name, x.IsMain, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);
        return item is null ? AppResult<WarehouseDetailDto>.Fail("not_found", "Depósito no encontrado.") : AppResult<WarehouseDetailDto>.Ok(item);
    }
}

public sealed class CreateWarehouseCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateWarehouseCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Warehouses, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var branch = await db.Branches.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.BranchId, ct);
        if (branch is null) return AppResult<int>.Fail("branch_not_found", "Sucursal no encontrada.");
        if (await db.Warehouses.AnyAsync(x => x.AccountId == accountId && x.BranchId == request.BranchId && x.Name == request.Name.Trim(), ct))
            return AppResult<int>.Fail("duplicate_name", "Ya existe un depósito con ese nombre en la sucursal.");

        if (request.IsMain)
        {
            var currentMain = await db.Warehouses.Where(x => x.AccountId == accountId && x.BranchId == request.BranchId && x.IsMain).ToListAsync(ct);
            foreach (var item in currentMain)
            {
                item.IsMain = false;
                CommerceFeatureHelpers.TouchUpdate(item, current);
            }
        }

        var entity = new Warehouse
        {
            AccountId = accountId,
            BranchId = request.BranchId,
            Name = request.Name.Trim(),
            IsMain = request.IsMain,
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.Warehouses.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Warehouse", entity.Id, "created", $"Depósito creado: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateWarehouseCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateWarehouseCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Warehouses, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Depósito no encontrado.");
        var branch = await db.Branches.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.BranchId, ct);
        if (branch is null) return AppResult.Fail("branch_not_found", "Sucursal no encontrada.");
        if (await db.Warehouses.AnyAsync(x => x.AccountId == accountId && x.BranchId == request.BranchId && x.Name == request.Name.Trim() && x.Id != request.Id, ct))
            return AppResult.Fail("duplicate_name", "Ya existe otro depósito con ese nombre en la sucursal.");

        if (request.IsMain)
        {
            var currentMain = await db.Warehouses.Where(x => x.AccountId == accountId && x.BranchId == request.BranchId && x.IsMain && x.Id != request.Id).ToListAsync(ct);
            foreach (var item in currentMain)
            {
                item.IsMain = false;
                CommerceFeatureHelpers.TouchUpdate(item, current);
            }
        }

        entity.BranchId = request.BranchId;
        entity.Name = request.Name.Trim();
        entity.IsMain = request.IsMain;
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Warehouse", entity.Id, "updated", $"Depósito actualizado: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleWarehouseStatusCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ToggleWarehouseStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleWarehouseStatusCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Warehouses, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Depósito no encontrado.");
        entity.IsActive = request.IsActive;
        if (!request.IsActive) entity.IsMain = false;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Warehouse", entity.Id, request.IsActive ? "activated" : "deactivated", $"Depósito {(request.IsActive ? "activado" : "desactivado")}: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
    }
}

public sealed class GetCategoriesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCategoriesQuery, AppResult<PagedResult<CategoryListItemDto>>>
{
    public async Task<AppResult<PagedResult<CategoryListItemDto>>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Categories, ct);
        if (!scope.Success) return AppResult<PagedResult<CategoryListItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var query = db.ProductCategories.AsNoTracking().Where(x => x.AccountId == accountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search));
        }
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CategoryListItemDto(x.Id, x.Name, x.ParentCategoryId, x.ParentCategory != null ? x.ParentCategory.Name : null, x.IsActive, x.CreatedAtUtc))
            .ToListAsync(ct);
        return AppResult<PagedResult<CategoryListItemDto>>.Ok(new PagedResult<CategoryListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetCategoryTreeQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCategoryTreeQuery, AppResult<List<CategoryTreeItemDto>>>
{
    public async Task<AppResult<List<CategoryTreeItemDto>>> Handle(GetCategoryTreeQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Categories, ct);
        if (!scope.Success) return AppResult<List<CategoryTreeItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var categories = await db.ProductCategories.AsNoTracking()
            .Where(x => x.AccountId == accountId && (!request.IsActive.HasValue || x.IsActive == request.IsActive.Value))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        List<CategoryTreeItemDto> Build(int? parentId)
            => categories.Where(x => x.ParentCategoryId == parentId)
                .Select(x => new CategoryTreeItemDto(x.Id, x.Name, x.IsActive, x.ParentCategoryId, Build(x.Id)))
                .ToList();

        return AppResult<List<CategoryTreeItemDto>>.Ok(Build(null));
    }
}

public sealed class GetCategoryByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCategoryByIdQuery, AppResult<CategoryDetailDto>>
{
    public async Task<AppResult<CategoryDetailDto>> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Categories, ct);
        if (!scope.Success) return AppResult<CategoryDetailDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var item = await db.ProductCategories.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new CategoryDetailDto(x.Id, x.Name, x.ParentCategoryId, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);
        return item is null ? AppResult<CategoryDetailDto>.Fail("not_found", "Categoría no encontrada.") : AppResult<CategoryDetailDto>.Ok(item);
    }
}

public sealed class CreateCategoryCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateCategoryCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Categories, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (request.ParentCategoryId.HasValue && !await db.ProductCategories.AnyAsync(x => x.AccountId == accountId && x.Id == request.ParentCategoryId.Value, ct))
            return AppResult<int>.Fail("parent_not_found", "La categoría padre no existe.");
        if (await db.ProductCategories.AnyAsync(x => x.AccountId == accountId && x.ParentCategoryId == request.ParentCategoryId && x.Name == request.Name.Trim(), ct))
            return AppResult<int>.Fail("duplicate_name", "Ya existe una categoría con ese nombre en el mismo nivel.");
        var entity = new ProductCategory
        {
            AccountId = accountId,
            Name = request.Name.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.ProductCategories.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "ProductCategory", entity.Id, "created", $"Categoría creada: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateCategoryCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateCategoryCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Categories, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (request.ParentCategoryId == request.Id) return AppResult.Fail("invalid_parent", "Una categoría no puede ser padre de sí misma.");
        var entity = await db.ProductCategories.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Categoría no encontrada.");
        if (request.ParentCategoryId.HasValue && !await db.ProductCategories.AnyAsync(x => x.AccountId == accountId && x.Id == request.ParentCategoryId.Value, ct))
            return AppResult.Fail("parent_not_found", "La categoría padre no existe.");
        if (await CommerceFeatureHelpers.CreatesCategoryCycleAsync(db, accountId, request.Id, request.ParentCategoryId, ct))
            return AppResult.Fail("invalid_parent", "La categoría padre seleccionada genera una jerarquía circular.");
        if (await db.ProductCategories.AnyAsync(x => x.AccountId == accountId && x.ParentCategoryId == request.ParentCategoryId && x.Name == request.Name.Trim() && x.Id != request.Id, ct))
            return AppResult.Fail("duplicate_name", "Ya existe otra categoría con ese nombre en el mismo nivel.");
        entity.Name = request.Name.Trim();
        entity.ParentCategoryId = request.ParentCategoryId;
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "ProductCategory", entity.Id, "updated", $"Categoría actualizada: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleCategoryStatusCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ToggleCategoryStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleCategoryStatusCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Categories, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.ProductCategories.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Categoría no encontrada.");
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "ProductCategory", entity.Id, request.IsActive ? "activated" : "deactivated", $"Categoría {(request.IsActive ? "activada" : "desactivada")}: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.InternalCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.InternalCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0);
    }
}

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
    }
}

public sealed class GetProductsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetProductsQuery, AppResult<PagedResult<ProductListItemDto>>>
{
    public async Task<AppResult<PagedResult<ProductListItemDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult<PagedResult<ProductListItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var query = db.Products.AsNoTracking().Where(x => x.AccountId == accountId);
        if (request.CategoryId.HasValue) query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.InternalCode.Contains(search) || (x.Barcode != null && x.Barcode.Contains(search)) || x.Brand.Contains(search));
        }
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductListItemDto(x.Id, x.Name, x.InternalCode, x.Barcode, x.Category.Name, x.Brand, x.UnitOfMeasure, x.SalePrice, x.IsActive, x.Variants.Count(v => v.IsActive)))
            .ToListAsync(ct);
        return AppResult<PagedResult<ProductListItemDto>>.Ok(new PagedResult<ProductListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetProductByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetProductByIdQuery, AppResult<ProductDetailDto>>
{
    public async Task<AppResult<ProductDetailDto>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult<ProductDetailDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var item = await db.Products.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new ProductDetailDto(x.Id, x.Name, x.InternalCode, x.Barcode, x.Description, x.CategoryId, x.Brand, x.UnitOfMeasure, x.Cost, x.SalePrice, x.MinimumStock, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);
        return item is null ? AppResult<ProductDetailDto>.Fail("not_found", "Producto no encontrado.") : AppResult<ProductDetailDto>.Ok(item);
    }
}

public sealed class GetProductSeedDataQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetProductSeedDataQuery, AppResult<ProductSeedDataDto>>
{
    public async Task<AppResult<ProductSeedDataDto>> Handle(GetProductSeedDataQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult<ProductSeedDataDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var categories = await db.ProductCategories.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        var brands = await db.Products.AsNoTracking().Where(x => x.AccountId == accountId && x.Brand != string.Empty).Select(x => x.Brand).Distinct().OrderBy(x => x).ToListAsync(ct);
        return AppResult<ProductSeedDataDto>.Ok(new ProductSeedDataDto(categories, brands, Enum.GetValues<UnitOfMeasure>()));
    }
}

public sealed class CreateProductCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateProductCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (!await db.ProductCategories.AnyAsync(x => x.AccountId == accountId && x.Id == request.CategoryId, ct))
            return AppResult<int>.Fail("category_not_found", "La categoría no existe.");
        if (await db.Products.AnyAsync(x => x.AccountId == accountId && x.InternalCode == request.InternalCode.Trim(), ct))
            return AppResult<int>.Fail("duplicate_code", "Ya existe un producto con ese código interno.");
        var entity = new Product
        {
            AccountId = accountId,
            Name = request.Name.Trim(),
            InternalCode = request.InternalCode.Trim(),
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            Brand = request.Brand.Trim(),
            UnitOfMeasure = request.UnitOfMeasure,
            Cost = request.Cost,
            SalePrice = request.SalePrice,
            MinimumStock = request.MinimumStock,
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.Products.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Product", entity.Id, "created", $"Producto creado: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateProductCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateProductCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (!await db.ProductCategories.AnyAsync(x => x.AccountId == accountId && x.Id == request.CategoryId, ct))
            return AppResult.Fail("category_not_found", "La categoría no existe.");
        var entity = await db.Products.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Producto no encontrado.");
        if (await db.Products.AnyAsync(x => x.AccountId == accountId && x.InternalCode == request.InternalCode.Trim() && x.Id != request.Id, ct))
            return AppResult.Fail("duplicate_code", "Ya existe otro producto con ese código interno.");
        entity.Name = request.Name.Trim();
        entity.InternalCode = request.InternalCode.Trim();
        entity.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        entity.Description = request.Description.Trim();
        entity.CategoryId = request.CategoryId;
        entity.Brand = request.Brand.Trim();
        entity.UnitOfMeasure = request.UnitOfMeasure;
        entity.Cost = request.Cost;
        entity.SalePrice = request.SalePrice;
        entity.MinimumStock = request.MinimumStock;
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Product", entity.Id, "updated", $"Producto actualizado: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleProductStatusCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ToggleProductStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleProductStatusCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Products.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Producto no encontrado.");
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Product", entity.Id, request.IsActive ? "activated" : "deactivated", $"Producto {(request.IsActive ? "activado" : "desactivado")}: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateProductVariantCommandValidator : AbstractValidator<CreateProductVariantCommand>
{
    public CreateProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.InternalCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x.AttributesSummary).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.InternalCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x.AttributesSummary).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class GetProductVariantsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetProductVariantsQuery, AppResult<List<ProductVariantListItemDto>>>
{
    public async Task<AppResult<List<ProductVariantListItemDto>>> Handle(GetProductVariantsQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult<List<ProductVariantListItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var items = await db.ProductVariants.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.ProductId == request.ProductId)
            .OrderBy(x => x.Name)
            .Select(x => new ProductVariantListItemDto(x.Id, x.ProductId, x.Product.Name, x.Name, x.InternalCode, x.Barcode, x.AttributesSummary, x.Cost, x.SalePrice, x.IsActive))
            .ToListAsync(ct);
        return AppResult<List<ProductVariantListItemDto>>.Ok(items);
    }
}

public sealed class GetProductVariantByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetProductVariantByIdQuery, AppResult<ProductVariantDetailDto>>
{
    public async Task<AppResult<ProductVariantDetailDto>> Handle(GetProductVariantByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult<ProductVariantDetailDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var item = await db.ProductVariants.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new ProductVariantDetailDto(x.Id, x.ProductId, x.Name, x.InternalCode, x.Barcode, x.AttributesSummary, x.Cost, x.SalePrice, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);
        return item is null ? AppResult<ProductVariantDetailDto>.Fail("not_found", "Variante no encontrada.") : AppResult<ProductVariantDetailDto>.Ok(item);
    }
}

public sealed class CreateProductVariantCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateProductVariantCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateProductVariantCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (!await db.Products.AnyAsync(x => x.AccountId == accountId && x.Id == request.ProductId, ct))
            return AppResult<int>.Fail("product_not_found", "El producto no existe.");
        if (await db.ProductVariants.AnyAsync(x => x.AccountId == accountId && x.InternalCode == request.InternalCode.Trim(), ct))
            return AppResult<int>.Fail("duplicate_code", "Ya existe una variante con ese código interno.");
        var entity = new ProductVariant
        {
            AccountId = accountId,
            ProductId = request.ProductId,
            Name = request.Name.Trim(),
            InternalCode = request.InternalCode.Trim(),
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            AttributesSummary = request.AttributesSummary.Trim(),
            Cost = request.Cost,
            SalePrice = request.SalePrice,
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.ProductVariants.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "ProductVariant", entity.Id, "created", $"Variante creada: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateProductVariantCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateProductVariantCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateProductVariantCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (!await db.Products.AnyAsync(x => x.AccountId == accountId && x.Id == request.ProductId, ct))
            return AppResult.Fail("product_not_found", "El producto no existe.");
        var entity = await db.ProductVariants.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Variante no encontrada.");
        if (await db.ProductVariants.AnyAsync(x => x.AccountId == accountId && x.InternalCode == request.InternalCode.Trim() && x.Id != request.Id, ct))
            return AppResult.Fail("duplicate_code", "Ya existe otra variante con ese código interno.");
        entity.ProductId = request.ProductId;
        entity.Name = request.Name.Trim();
        entity.InternalCode = request.InternalCode.Trim();
        entity.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        entity.AttributesSummary = request.AttributesSummary.Trim();
        entity.Cost = request.Cost;
        entity.SalePrice = request.SalePrice;
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "ProductVariant", entity.Id, "updated", $"Variante actualizada: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleProductVariantStatusCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ToggleProductVariantStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleProductVariantStatusCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.ProductVariants.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Variante no encontrada.");
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "ProductVariant", entity.Id, request.IsActive ? "activated" : "deactivated", $"Variante {(request.IsActive ? "activada" : "desactivada")}: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(250);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
    }
}

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(250);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
    }
}

public sealed class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
    {
        CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
    }
}

public sealed class GetCustomersQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCustomersQuery, AppResult<PagedResult<CustomerListItemDto>>>
{
    public async Task<AppResult<PagedResult<CustomerListItemDto>>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Customers, ct);
        if (!scope.Success) return AppResult<PagedResult<CustomerListItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var query = db.Customers.AsNoTracking().Where(x => x.AccountId == accountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || (x.DocumentNumber != null && x.DocumentNumber.Contains(search)) || x.Phone.Contains(search) || x.City.Contains(search));
        }
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomerListItemDto(x.Id, x.Name, x.DocumentNumber, x.Phone, x.City, x.CustomerType, x.IsActive))
            .ToListAsync(ct);
        return AppResult<PagedResult<CustomerListItemDto>>.Ok(new PagedResult<CustomerListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetCustomerByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCustomerByIdQuery, AppResult<CustomerDetailDto>>
{
    public async Task<AppResult<CustomerDetailDto>> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Customers, ct);
        if (!scope.Success) return AppResult<CustomerDetailDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var item = await db.Customers.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new CustomerDetailDto(x.Id, x.Name, x.DocumentNumber, x.Phone, x.Address, x.City, x.CustomerType, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);
        return item is null ? AppResult<CustomerDetailDto>.Fail("not_found", "Cliente no encontrado.") : AppResult<CustomerDetailDto>.Ok(item);
    }
}

public sealed class CreateCustomerCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateCustomerCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Customers, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = new Customer
        {
            AccountId = accountId,
            Name = request.Name.Trim(),
            DocumentNumber = string.IsNullOrWhiteSpace(request.DocumentNumber) ? null : request.DocumentNumber.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            CustomerType = request.CustomerType,
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.Customers.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Customer", entity.Id, "created", $"Cliente creado: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateCustomerCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateCustomerCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Customers, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Customers.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Cliente no encontrado.");
        entity.Name = request.Name.Trim();
        entity.DocumentNumber = string.IsNullOrWhiteSpace(request.DocumentNumber) ? null : request.DocumentNumber.Trim();
        entity.Phone = request.Phone.Trim();
        entity.Address = request.Address.Trim();
        entity.City = request.City.Trim();
        entity.CustomerType = request.CustomerType;
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Customer", entity.Id, "updated", $"Cliente actualizado: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleCustomerStatusCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ToggleCustomerStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleCustomerStatusCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Customers, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Customers.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Cliente no encontrado.");
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Customer", entity.Id, request.IsActive ? "activated" : "deactivated", $"Cliente {(request.IsActive ? "activado" : "desactivado")}: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(180);
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
    }
}

public sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(180);
        RuleFor(x => x.TaxId).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
    }
}

public sealed class GetSuppliersQueryValidator : AbstractValidator<GetSuppliersQuery>
{
    public GetSuppliersQueryValidator()
    {
        CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
    }
}

public sealed class GetSuppliersQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSuppliersQuery, AppResult<PagedResult<SupplierListItemDto>>>
{
    public async Task<AppResult<PagedResult<SupplierListItemDto>>> Handle(GetSuppliersQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Suppliers, ct);
        if (!scope.Success) return AppResult<PagedResult<SupplierListItemDto>>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var query = db.Suppliers.AsNoTracking().Where(x => x.AccountId == accountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.TaxId.Contains(search) || x.Phone.Contains(search));
        }
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SupplierListItemDto(x.Id, x.Name, x.TaxId, x.Phone, x.IsActive))
            .ToListAsync(ct);
        return AppResult<PagedResult<SupplierListItemDto>>.Ok(new PagedResult<SupplierListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetSupplierByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSupplierByIdQuery, AppResult<SupplierDetailDto>>
{
    public async Task<AppResult<SupplierDetailDto>> Handle(GetSupplierByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Suppliers, ct);
        if (!scope.Success) return AppResult<SupplierDetailDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var item = await db.Suppliers.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new SupplierDetailDto(x.Id, x.Name, x.TaxId, x.Phone, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);
        return item is null ? AppResult<SupplierDetailDto>.Fail("not_found", "Proveedor no encontrado.") : AppResult<SupplierDetailDto>.Ok(item);
    }
}

public sealed class CreateSupplierCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateSupplierCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Suppliers, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (await db.Suppliers.AnyAsync(x => x.AccountId == accountId && x.TaxId == request.TaxId.Trim(), ct))
            return AppResult<int>.Fail("duplicate_taxid", "Ya existe un proveedor con ese CUIT/DNI.");
        var entity = new Supplier
        {
            AccountId = accountId,
            Name = request.Name.Trim(),
            TaxId = request.TaxId.Trim(),
            Phone = request.Phone.Trim(),
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.Suppliers.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Supplier", entity.Id, "created", $"Proveedor creado: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateSupplierCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateSupplierCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateSupplierCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Suppliers, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Suppliers.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Proveedor no encontrado.");
        if (await db.Suppliers.AnyAsync(x => x.AccountId == accountId && x.TaxId == request.TaxId.Trim() && x.Id != request.Id, ct))
            return AppResult.Fail("duplicate_taxid", "Ya existe otro proveedor con ese CUIT/DNI.");
        entity.Name = request.Name.Trim();
        entity.TaxId = request.TaxId.Trim();
        entity.Phone = request.Phone.Trim();
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Supplier", entity.Id, "updated", $"Proveedor actualizado: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class ToggleSupplierStatusCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ToggleSupplierStatusCommand, AppResult>
{
    public async Task<AppResult> Handle(ToggleSupplierStatusCommand request, CancellationToken ct)
    {
        var scope = await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Suppliers, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.Suppliers.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Proveedor no encontrado.");
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "Supplier", entity.Id, request.IsActive ? "activated" : "deactivated", $"Proveedor {(request.IsActive ? "activado" : "desactivado")}: {entity.Name}", ct);
        return AppResult.Ok();
    }
}
