using GestAI.Application.Abstractions;
using GestAI.Domain.Entities;
using GestAI.Domain.Entities.Commerce;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GestAI.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<User>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountUser> AccountUsers => Set<AccountUser>();
    public DbSet<AccountSubscriptionPlan> AccountSubscriptionPlans => Set<AccountSubscriptionPlan>();
    public DbSet<SaasPlanDefinition> SaasPlanDefinitions => Set<SaasPlanDefinition>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductWarehouseStock> ProductWarehouseStocks => Set<ProductWarehouseStock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    DbSet<User> IAppDbContext.Users => Set<User>();

    public async Task<IDbContextTransactionAdapter> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default)
    {
        var tx = await Database.BeginTransactionAsync(isolationLevel, ct);
        return new EfTxAdapter(tx);
    }

    public Task<int> ExecuteSqlInterpolatedAsync(FormattableString sql, CancellationToken ct = default)
        => Database.ExecuteSqlInterpolatedAsync(sql, ct);

    private sealed class EfTxAdapter : IDbContextTransactionAdapter
    {
        private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _tx;
        public EfTxAdapter(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx) => _tx = tx;
        public Task CommitAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct = default) => _tx.RollbackAsync(ct);
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
