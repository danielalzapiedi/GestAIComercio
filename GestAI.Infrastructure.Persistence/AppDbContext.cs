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
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<CustomerAccountMovement> CustomerAccountMovements => Set<CustomerAccountMovement>();
    public DbSet<CustomerAccountAllocation> CustomerAccountAllocations => Set<CustomerAccountAllocation>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseDocument> PurchaseDocuments => Set<PurchaseDocument>();
    public DbSet<PurchaseDocumentItem> PurchaseDocumentItems => Set<PurchaseDocumentItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<SupplierAccountMovement> SupplierAccountMovements => Set<SupplierAccountMovement>();
    public DbSet<SupplierAccountAllocation> SupplierAccountAllocations => Set<SupplierAccountAllocation>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<FiscalConfiguration> FiscalConfigurations => Set<FiscalConfiguration>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    public DbSet<CommercialInvoice> CommercialInvoices => Set<CommercialInvoice>();
    public DbSet<CommercialInvoiceItem> CommercialInvoiceItems => Set<CommercialInvoiceItem>();
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    public DbSet<DeliveryNoteItem> DeliveryNoteItems => Set<DeliveryNoteItem>();
    public DbSet<FiscalDocumentSubmission> FiscalDocumentSubmissions => Set<FiscalDocumentSubmission>();
    public DbSet<DocumentChangeLog> DocumentChangeLogs => Set<DocumentChangeLog>();
    DbSet<User> IAppDbContext.Users => Set<User>();

    public async Task<IDbContextTransactionAdapter> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default)
    {
        if (string.Equals(Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            return NoOpTxAdapter.Instance;

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

    private sealed class NoOpTxAdapter : IDbContextTransactionAdapter
    {
        public static NoOpTxAdapter Instance { get; } = new();
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
