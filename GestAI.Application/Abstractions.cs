using GestAI.Domain.Entities;
using GestAI.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;

namespace GestAI.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<AccountUser> AccountUsers { get; }
    DbSet<AccountSubscriptionPlan> AccountSubscriptionPlans { get; }
    DbSet<SaasPlanDefinition> SaasPlanDefinitions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductWarehouseStock> ProductWarehouseStocks { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<PriceList> PriceLists { get; }
    DbSet<PriceListItem> PriceListItems { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<User> Users { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransactionAdapter> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default);
    Task<int> ExecuteSqlInterpolatedAsync(FormattableString sql, CancellationToken ct = default);
}

public interface IDbContextTransactionAdapter : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

public interface IDateTime
{
    DateTime UtcNow { get; }
}

public interface ICurrentUser
{
    string UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    bool IsInRole(string role);
}

public interface IUserAccessService
{
    Task<int?> GetCurrentAccountIdAsync(CancellationToken ct);
    Task<AccountUser?> GetMembershipAsync(int accountId, CancellationToken ct);
    Task<bool> HasModuleAccessAsync(int accountId, GestAI.Domain.Enums.SaasModule module, CancellationToken ct);
}

public interface ISaasPlanService
{
    Task<(bool Success, string? ErrorCode, string? Message)> ValidateUserCreationAsync(int accountId, CancellationToken ct);
}

public interface IAuditService
{
    Task WriteAsync(int accountId, int? propertyId, string entityName, int? entityId, string action, string summary, CancellationToken ct);
}

public interface IIdentityService
{
    Task<(bool Success, string? UserId, string? Error)> CreateUserIfNotExistsAsync(string email, string password, CancellationToken ct);
    Task<(bool Success, string? UserId, string? Error)> CreateUserIfNotExistsAsync(string email, string password, CancellationToken ct, string firstName, string lastName, bool isActive, int? defaultPropertyId, int defaultAccountId);
    Task<(bool Success, string? UserId, string? Error)> FindUserIdByEmailAsync(string email, CancellationToken ct);
}

public interface IAccountResolver
{
    Task<int?> GetCurrentAccountIdAsync(string userId, CancellationToken ct);
    Task<bool> HasAccessAsync(string userId, int accountId, CancellationToken ct);
}
