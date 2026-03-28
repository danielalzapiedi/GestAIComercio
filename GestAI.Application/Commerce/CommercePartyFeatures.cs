using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Entities.Commerce;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestAI.Application.Commerce;


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
        var pageSize = Math.Clamp(request.PageSize, 1, CommerceFeatureHelpers.MaxPageSize);
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
        var pageSize = Math.Clamp(request.PageSize, 1, CommerceFeatureHelpers.MaxPageSize);
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
