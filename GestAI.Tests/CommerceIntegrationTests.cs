using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Application.Commerce;
using GestAI.Application.Saas;
using GestAI.Domain.Entities;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using GestAI.Infrastructure.Persistence;
using GestAI.Infrastructure.Commerce;
using GestAI.Infrastructure.Saas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace GestAI.Tests;

public sealed class CommerceIntegrationTests
{

    [Fact]
    public async Task CreateQuote_ComputesTotals_AndPersistsSnapshotItems()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-quotes@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Demo", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto comercial",
            InternalCode = "DOC-1",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 25,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new CreateQuoteCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreateQuoteCommand(customer.Id, QuoteStatus.Sent, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), "Observación", new[]
        {
            new CommercialLineInput(product.Id, null, null, 2, 25, 1),
            new CommercialLineInput(product.Id, null, "Producto comercial (bonificado)", 1, 20, 2)
        }), CancellationToken.None);

        Assert.True(result.Success);
        var quote = await db.Quotes.Include(x => x.Items).SingleAsync(x => x.Id == result.Data);
        Assert.Equal("P-000001", quote.Number);
        Assert.Equal(70, quote.Total);
        Assert.Equal(2, quote.Items.Count);
        Assert.Contains(quote.Items, x => x.Description == "Producto comercial");
        Assert.Contains(quote.Items, x => x.Description == "Producto comercial (bonificado)");
    }

    [Fact]
    public async Task ConvertQuoteToSale_CreatesSale_AndMarksQuoteAsConverted()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-convert@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Demo", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto comercial",
            InternalCode = "DOC-2",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 30,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var createQuote = new CreateQuoteCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var quoteResult = await createQuote.Handle(new CreateQuoteCommand(customer.Id, QuoteStatus.Approved, DateTime.UtcNow, DateTime.UtcNow.AddDays(5), null, new[]
        {
            new CommercialLineInput(product.Id, null, null, 3, 30, 1)
        }), CancellationToken.None);

        var convert = new ConvertQuoteToSaleCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        Assert.NotNull(quoteResult.Data);
        var quoteId = quoteResult.Data!;

        var result = await convert.Handle(new ConvertQuoteToSaleCommand(quoteId, SaleStatus.Confirmed, DateTime.UtcNow, null), CancellationToken.None);

        Assert.True(result.Success);
        var quote = await db.Quotes.SingleAsync(x => x.Id == quoteId);
        var sale = await db.Sales.Include(x => x.Items).SingleAsync(x => x.Id == result.Data);
        Assert.Equal(QuoteStatus.Converted, quote.Status);
        Assert.Equal(quote.Id, sale.SourceQuoteId);
        Assert.Equal(90, sale.Total);
        Assert.Single(sale.Items);
    }


    [Fact]
    public async Task Smoke_CriticalCommerceFlow_QuoteToSaleToInvoice_WorksEndToEnd()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-smoke@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Smoke", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto Smoke",
            InternalCode = "SMK-1",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 45,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var createQuote = new CreateQuoteCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var quoteResult = await createQuote.Handle(new CreateQuoteCommand(customer.Id, QuoteStatus.Approved, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), "Smoke", new[]
        {
            new CommercialLineInput(product.Id, null, null, 2, 45, 1)
        }), CancellationToken.None);

        Assert.True(quoteResult.Success);
        Assert.NotNull(quoteResult.Data);

        var convert = new ConvertQuoteToSaleCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var saleResult = await convert.Handle(new ConvertQuoteToSaleCommand(quoteResult.Data!, SaleStatus.Confirmed, DateTime.UtcNow, "Smoke convert"), CancellationToken.None);

        Assert.True(saleResult.Success);
        Assert.NotNull(saleResult.Data);

        db.FiscalConfigurations.Add(new FiscalConfiguration
        {
            AccountId = fixture.Account.Id,
            LegalName = "Smoke SA",
            TaxIdentifier = "30712345678",
            PointOfSale = 1,
            DefaultInvoiceType = InvoiceType.InvoiceB,
            IntegrationMode = FiscalIntegrationMode.Mock,
            UseSandbox = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fixture.CurrentUser.UserId
        });
        await db.SaveChangesAsync();

        var createInvoice = new CreateInvoiceCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var invoiceResult = await createInvoice.Handle(new CreateInvoiceCommand(saleResult.Data!, null, InvoiceType.InvoiceB, DateTime.UtcNow, 0.21m), CancellationToken.None);

        Assert.True(invoiceResult.Success);
        Assert.NotNull(invoiceResult.Data);

        var invoice = await db.CommercialInvoices.SingleAsync(x => x.Id == invoiceResult.Data);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public async Task CreateQuickSale_UsesCurrentSalePrice_ForSkuSnapshot()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-quick@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Mostrador", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Consumer, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto rápido",
            InternalCode = "QK-1",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 40,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new CreateQuickSaleCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreateQuickSaleCommand(customer.Id, SaleStatus.Confirmed, DateTime.UtcNow, "Mostrador", new[]
        {
            new QuickCommercialLineDto(product.Id, null, 2)
        }), CancellationToken.None);

        Assert.True(result.Success);
        var sale = await db.Sales.Include(x => x.Items).SingleAsync(x => x.Id == result.Data);
        Assert.Equal(80, sale.Total);
        Assert.Equal("Producto rápido", sale.Items.Single().Description);
        Assert.Equal(40, sale.Items.Single().UnitPrice);
    }

    [Fact]
    public async Task GetBranchesQuery_Fails_WhenUserHasNoModuleAccess()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = "owner-no-plan", UserName = "owner1@test", Email = "owner1@test", Nombre = "Owner", Apellido = "One", DefaultAccountId = 0 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var account = new Account { Name = "Cuenta sin plan", OwnerUserId = user.Id, IsActive = true };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        user.DefaultAccountId = account.Id;
        db.Branches.Add(new Branch { AccountId = account.Id, Name = "Casa Central", Code = "CC", IsActive = true });
        await db.SaveChangesAsync();

        var current = new TestCurrentUser(user.Id);
        var access = new UserAccessService(db, current);
        var handler = new GetBranchesQueryHandler(db, access);

        var result = await handler.Handle(new GetBranchesQuery(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task CreateWarehouse_WithIsMain_ResetsPreviousMainWarehouse()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-main@test");

        var branch = new Branch { AccountId = fixture.Account.Id, Name = "Sucursal 1", Code = "S1", IsActive = true };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var currentMain = new Warehouse
        {
            AccountId = fixture.Account.Id,
            BranchId = branch.Id,
            Name = "Depósito principal",
            IsMain = true,
            IsActive = true
        };
        db.Warehouses.Add(currentMain);
        await db.SaveChangesAsync();

        var handler = new CreateWarehouseCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());

        var result = await handler.Handle(new CreateWarehouseCommand(branch.Id, "Depósito nuevo", true, true), CancellationToken.None);

        Assert.True(result.Success);
        var warehouses = await db.Warehouses.Where(x => x.BranchId == branch.Id).OrderBy(x => x.Id).ToListAsync();
        Assert.False(warehouses.Single(x => x.Id == currentMain.Id).IsMain);
        Assert.True(warehouses.Single(x => x.Id == result.Data!).IsMain);
    }

    [Fact]
    public async Task UpdateCategory_Fails_WhenParentSelectionCreatesCycle()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-categories@test");

        var parent = new ProductCategory { AccountId = fixture.Account.Id, Name = "Padre", IsActive = true };
        var child = new ProductCategory { AccountId = fixture.Account.Id, Name = "Hijo", ParentCategory = parent, IsActive = true };
        db.ProductCategories.AddRange(parent, child);
        await db.SaveChangesAsync();

        var handler = new UpdateCategoryCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());

        var result = await handler.Handle(new UpdateCategoryCommand(parent.Id, "Padre", child.Id, true), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("invalid_parent", result.ErrorCode);
    }

    [Fact]
    public async Task CreateProductVariant_Fails_WhenInternalCodeAlreadyExistsInAccount()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-products@test");

        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto base",
            InternalCode = "PROD-1",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 1,
            SalePrice = 2,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.ProductVariants.Add(new ProductVariant
        {
            AccountId = fixture.Account.Id,
            ProductId = product.Id,
            Name = "Variante existente",
            InternalCode = "VAR-1",
            AttributesSummary = "Rojo",
            Cost = 1,
            SalePrice = 2,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var handler = new CreateProductVariantCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreateProductVariantCommand(product.Id, "Variante nueva", "VAR-1", null, "Azul", 1, 3, true), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("duplicate_code", result.ErrorCode);
    }

    [Fact]
    public async Task CreateTenant_CreatesAccountMembershipAndDefaultPlan()
    {
        await using var db = CreateDbContext();
        db.SaasPlanDefinitions.Add(new SaasPlanDefinition
        {
            Code = SaasPlanCode.Pro,
            Name = "Pro",
            MaxProperties = 3,
            MaxUnits = 20,
            MaxUsers = 5,
            IncludesOperations = true,
            IncludesPublicPortal = true,
            IncludesReports = true
        });
        await db.SaveChangesAsync();

        var identity = new TestIdentityService("tenant-owner");
        db.Users.Add(new User { Id = "tenant-owner", UserName = "tenant@owner.test", Email = "tenant@owner.test", Nombre = "Tenant", Apellido = "Owner", DefaultAccountId = 0 });
        await db.SaveChangesAsync();

        var handler = new CreateTenantCommandHandler(
            db,
            identity,
            new TestAuditService(),
            new TestCurrentUser("super-admin", "SuperAdmin"));

        var result = await handler.Handle(
            new CreateTenantCommand("Tenant Uno", "Tenant", "Owner", "tenant@owner.test", "Admin123$"),
            CancellationToken.None);

        Assert.True(result.Success);
        var createdAccount = await db.Accounts.Include(x => x.Users).Include(x => x.SubscriptionPlans).SingleAsync(x => x.Id == result.Data!);
        Assert.Equal("Tenant Uno", createdAccount.Name);
        Assert.Contains(createdAccount.Users, x => x.UserId == "tenant-owner" && x.Role == InternalUserRole.Owner);
        Assert.Contains(createdAccount.SubscriptionPlans, x => x.IsActive);
        Assert.Equal(createdAccount.Id, await db.Users.Where(x => x.Id == "tenant-owner").Select(x => x.DefaultAccountId).SingleAsync());
    }

    [Fact]
    public async Task GetAccountUsers_Fails_WhenUserHasNoUsersModuleAccess()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = "owner-no-users", UserName = "owner-users@test", Email = "owner-users@test", Nombre = "Owner", Apellido = "Users", DefaultAccountId = 0 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var account = new Account { Name = "Cuenta sin users", OwnerUserId = user.Id, IsActive = true };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        user.DefaultAccountId = account.Id;
        db.AccountUsers.Add(new AccountUser { AccountId = account.Id, UserId = user.Id, Role = InternalUserRole.Owner, IsActive = true });
        await db.SaveChangesAsync();

        var access = new UserAccessService(db, new TestCurrentUser(user.Id));
        var handler = new GetAccountUsersQueryHandler(db, access);

        var result = await handler.Handle(new GetAccountUsersQuery(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task GetAccountAudit_Fails_WhenUserHasNoAuditModuleAccess()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = "owner-no-audit", UserName = "owner-audit@test", Email = "owner-audit@test", Nombre = "Owner", Apellido = "Audit", DefaultAccountId = 0 };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var account = new Account { Name = "Cuenta sin audit", OwnerUserId = user.Id, IsActive = true };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        user.DefaultAccountId = account.Id;
        db.AuditLogs.Add(new AuditLog { AccountId = account.Id, Action = "created", EntityName = "Account", Summary = "Seed" });
        await db.SaveChangesAsync();

        var access = new UserAccessService(db, new TestCurrentUser(user.Id));
        var handler = new GetAccountAuditQueryHandler(db, access);

        var result = await handler.Handle(new GetAccountAuditQuery(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task GetCurrentUserAccess_Employee_UsesAssignedModules()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-permissions@test");

        var employee = new User
        {
            Id = "employee-permissions",
            UserName = "employee-permissions@test",
            Email = "employee-permissions@test",
            Nombre = "Employee",
            Apellido = "Permissions",
            DefaultAccountId = fixture.Account.Id,
            IsActive = true
        };
        db.Users.Add(employee);
        db.AccountUsers.Add(new AccountUser
        {
            AccountId = fixture.Account.Id,
            UserId = employee.Id,
            Role = InternalUserRole.Employee,
            AllowedModules = "Users,Products",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var current = new TestCurrentUser(employee.Id);
        var access = new UserAccessService(db, current);
        var handler = new GetCurrentUserAccessQueryHandler(db, current, access);

        var result = await handler.Handle(new GetCurrentUserAccessQuery(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data!.Modules, x => x.Module == SaasModule.Users && x.Allowed);
        Assert.Contains(result.Data!.Modules, x => x.Module == SaasModule.Products && x.Allowed);
        Assert.Contains(result.Data!.Modules, x => x.Module == SaasModule.Configuration && !x.Allowed);
        Assert.Contains(result.Data!.Modules, x => x.Module == SaasModule.PlatformTenants && !x.Allowed);
    }

    [Fact]
    public async Task RecordStockMovement_TransferOut_UpdatesOriginAndDestinationStock()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-stock@test");

        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto stock",
            InternalCode = "STK-1",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 15,
            MinimumStock = 2,
            IsActive = true
        };
        db.Products.Add(product);

        var branch = new Branch { AccountId = fixture.Account.Id, Name = "Central", Code = "CTR", IsActive = true };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var origin = new Warehouse { AccountId = fixture.Account.Id, BranchId = branch.Id, Name = "Origen", IsMain = true, IsActive = true };
        var destination = new Warehouse { AccountId = fixture.Account.Id, BranchId = branch.Id, Name = "Destino", IsMain = false, IsActive = true };
        db.Warehouses.AddRange(origin, destination);
        await db.SaveChangesAsync();

        db.ProductWarehouseStocks.Add(new ProductWarehouseStock
        {
            AccountId = fixture.Account.Id,
            ProductId = product.Id,
            WarehouseId = origin.Id,
            QuantityOnHand = 10,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fixture.CurrentUser.UserId
        });
        await db.SaveChangesAsync();

        var handler = new RecordStockMovementCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());

        var result = await handler.Handle(new RecordStockMovementCommand(product.Id, null, origin.Id, destination.Id, StockMovementType.TransferOut, 4, "Reubicación", "Transferencia interna", DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(6, await db.ProductWarehouseStocks.Where(x => x.ProductId == product.Id && x.WarehouseId == origin.Id).Select(x => x.QuantityOnHand).SingleAsync());
        Assert.Equal(4, await db.ProductWarehouseStocks.Where(x => x.ProductId == product.Id && x.WarehouseId == destination.Id).Select(x => x.QuantityOnHand).SingleAsync());
        Assert.Equal(2, await db.StockMovements.CountAsync(x => x.ProductId == product.Id));
    }

    [Fact]
    public async Task ApplyPriceListAdjustment_CreatesItems_FromSalePriceBase()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-prices@test");

        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product
            {
                AccountId = fixture.Account.Id,
                Name = "Prod 1",
                InternalCode = "P1",
                Description = "P1",
                CategoryId = category.Id,
                Brand = "Marca",
                UnitOfMeasure = UnitOfMeasure.Unit,
                Cost = 10,
                SalePrice = 20,
                MinimumStock = 1,
                IsActive = true
            },
            new Product
            {
                AccountId = fixture.Account.Id,
                Name = "Prod 2",
                InternalCode = "P2",
                Description = "P2",
                CategoryId = category.Id,
                Brand = "Marca",
                UnitOfMeasure = UnitOfMeasure.Unit,
                Cost = 30,
                SalePrice = 50,
                MinimumStock = 1,
                IsActive = true
            });
        await db.SaveChangesAsync();

        var createList = new CreatePriceListCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var createListResult = await createList.Handle(new CreatePriceListCommand("Minorista", PriceListBaseMode.SalePrice, PriceListTargetType.Product, true), CancellationToken.None);
        Assert.True(createListResult.Success);
        var listId = createListResult.Data;

        var handler = new ApplyPriceListAdjustmentCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new ApplyPriceListAdjustmentCommand(listId, BulkPriceAdjustmentType.Percentage, 10, false, null), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, await db.PriceListItems.CountAsync(x => x.PriceListId == listId));
        Assert.Contains(await db.PriceListItems.Where(x => x.PriceListId == listId).Select(x => x.Price).ToListAsync(), x => x == 22);
        Assert.Contains(await db.PriceListItems.Where(x => x.PriceListId == listId).Select(x => x.Price).ToListAsync(), x => x == 55);
    }

    [Fact]
    public async Task PreviewProductImport_FailsRows_WhenCategoryDoesNotExist()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-import@test");

        var handler = new PreviewProductImportCommandHandler(db, fixture.Access);
        const string csv = "Name,InternalCode,Barcode,Description,CategoryName,Brand,UnitOfMeasure,Cost,SalePrice,MinimumStock,IsActive\n" +
                           "Producto demo,IMP-1,,Desc,Categoria Inexistente,Marca,Unit,10,20,1,true";

        var result = await handler.Handle(new PreviewProductImportCommand(csv, false), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data!.Rows);
        Assert.False(result.Data.Rows[0].IsValid);
    }

    [Fact]
    public async Task ApplyProductImport_AllowsMultipleRows_ForSameProductWithDifferentVariants()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-import-variants@test");

        db.ProductCategories.Add(new ProductCategory { AccountId = fixture.Account.Id, Name = "Indumentaria", IsActive = true });
        await db.SaveChangesAsync();

        var handler = new ApplyProductImportCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        const string csv = "Name,InternalCode,Barcode,Description,CategoryName,Brand,UnitOfMeasure,Cost,SalePrice,MinimumStock,IsActive,VariantName,VariantInternalCode,VariantBarcode,VariantAttributes,VariantCost,VariantSalePrice\n" +
                           "Remera básica,REM-001,,Remera lisa,Indumentaria,Marca,Unit,10,20,2,true,Talle M,REM-001-M,,Negra / M,10,20\n" +
                           "Remera básica,REM-001,,Remera lisa,Indumentaria,Marca,Unit,10,20,2,true,Talle L,REM-001-L,,Negra / L,10,20";

        var result = await handler.Handle(new ApplyProductImportCommand(csv, false), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, await db.Products.CountAsync(x => x.InternalCode == "REM-001"));
        Assert.Equal(2, await db.ProductVariants.CountAsync(x => x.Product.InternalCode == "REM-001"));
        Assert.Equal(1, result.Data!.CreatedProducts);
        Assert.Equal(2, result.Data.CreatedVariants);
    }

    [Fact]
    public async Task CreatePurchaseDocument_ComputesTotals_AndCreatesSupplierLedger()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-purchases@test");

        var supplier = new Supplier { AccountId = fixture.Account.Id, Name = "Proveedor Demo", TaxId = "30-123", Phone = "111", IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Suppliers.Add(supplier);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto compra",
            InternalCode = "BUY-1",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 20,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new CreatePurchaseDocumentCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreatePurchaseDocumentCommand(supplier.Id, PurchaseDocumentType.PurchaseDocument, PurchaseDocumentStatus.Issued, DateTime.UtcNow, "FAC-1", "Compra inicial", new[]
        {
            new PurchaseLineInput(product.Id, null, null, 3, 12, 1)
        }), CancellationToken.None);

        Assert.True(result.Success);
        var purchase = await db.PurchaseDocuments.Include(x => x.Items).SingleAsync(x => x.Id == result.Data);
        Assert.Equal("OC-000001", purchase.Number);
        Assert.Equal(36, purchase.Total);
        var movement = await db.SupplierAccountMovements.SingleAsync(x => x.PurchaseDocumentId == purchase.Id);
        Assert.Equal(36, movement.DebitAmount);
        Assert.Equal("OC-000001", movement.ReferenceNumber);
    }

    [Fact]
    public async Task CreateGoodsReceipt_UpdatesStockCostAndReceiptStatus()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-receipts@test");

        var supplier = new Supplier { AccountId = fixture.Account.Id, Name = "Proveedor Stock", TaxId = "30-222", Phone = "111", IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        var branch = new Branch { AccountId = fixture.Account.Id, Name = "Central", Code = "CTR", IsActive = true };
        db.Suppliers.Add(supplier);
        db.ProductCategories.Add(category);
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var warehouse = new Warehouse { AccountId = fixture.Account.Id, BranchId = branch.Id, Name = "Principal", IsMain = true, IsActive = true };
        db.Warehouses.Add(warehouse);

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto costo",
            InternalCode = "BUY-2",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 20,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.ProductWarehouseStocks.Add(new ProductWarehouseStock
        {
            AccountId = fixture.Account.Id,
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            QuantityOnHand = 5,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fixture.CurrentUser.UserId
        });
        await db.SaveChangesAsync();

        var createPurchase = new CreatePurchaseDocumentCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var purchaseResult = await createPurchase.Handle(new CreatePurchaseDocumentCommand(supplier.Id, PurchaseDocumentType.PurchaseDocument, PurchaseDocumentStatus.Issued, DateTime.UtcNow, null, null, new[]
        {
            new PurchaseLineInput(product.Id, null, null, 5, 20, 1)
        }), CancellationToken.None);
        var purchase = await db.PurchaseDocuments.Include(x => x.Items).SingleAsync(x => x.Id == purchaseResult.Data);

        var handler = new CreateGoodsReceiptCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreateGoodsReceiptCommand(purchase.Id, warehouse.Id, DateTime.UtcNow, "Ingreso total", new[]
        {
            new GoodsReceiptLineInput(purchase.Items.Single().Id, 5)
        }), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(10, await db.ProductWarehouseStocks.Where(x => x.ProductId == product.Id && x.WarehouseId == warehouse.Id).Select(x => x.QuantityOnHand).SingleAsync());
        Assert.Equal(15, await db.Products.Where(x => x.Id == product.Id).Select(x => x.Cost).SingleAsync());
        Assert.Equal(PurchaseDocumentStatus.Received, await db.PurchaseDocuments.Where(x => x.Id == purchase.Id).Select(x => x.Status).SingleAsync());
        Assert.Single(await db.GoodsReceipts.Include(x => x.Items).ToListAsync());
        Assert.Single(await db.StockMovements.Where(x => x.ReferenceGroup == $"purchase:{purchase.Id}").ToListAsync());
    }

    [Fact]
    public async Task CreateGoodsReceipt_SupportsPartialReceipt()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-partial@test");

        var supplier = new Supplier { AccountId = fixture.Account.Id, Name = "Proveedor Parcial", TaxId = "30-333", Phone = "111", IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        var branch = new Branch { AccountId = fixture.Account.Id, Name = "Central", Code = "CTR", IsActive = true };
        db.Suppliers.Add(supplier);
        db.ProductCategories.Add(category);
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var warehouse = new Warehouse { AccountId = fixture.Account.Id, BranchId = branch.Id, Name = "Principal", IsMain = true, IsActive = true };
        db.Warehouses.Add(warehouse);
        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto parcial",
            InternalCode = "BUY-3",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 8,
            SalePrice = 20,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var createPurchase = new CreatePurchaseDocumentCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var purchaseResult = await createPurchase.Handle(new CreatePurchaseDocumentCommand(supplier.Id, PurchaseDocumentType.PurchaseDocument, PurchaseDocumentStatus.Issued, DateTime.UtcNow, null, null, new[]
        {
            new PurchaseLineInput(product.Id, null, null, 10, 8, 1)
        }), CancellationToken.None);
        var purchase = await db.PurchaseDocuments.Include(x => x.Items).SingleAsync(x => x.Id == purchaseResult.Data);

        var handler = new CreateGoodsReceiptCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreateGoodsReceiptCommand(purchase.Id, warehouse.Id, DateTime.UtcNow, null, new[]
        {
            new GoodsReceiptLineInput(purchase.Items.Single().Id, 4)
        }), CancellationToken.None);

        Assert.True(result.Success);
        var updated = await db.PurchaseDocuments.Include(x => x.Items).SingleAsync(x => x.Id == purchase.Id);
        Assert.Equal(PurchaseDocumentStatus.PartiallyReceived, updated.Status);
        Assert.Equal(4, updated.Items.Single().QuantityReceived);
    }

    [Fact]
    public async Task GetSupplierAccountSummary_ReturnsAccumulatedDebtFromPurchases()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-supplier-account@test");

        var supplier = new Supplier { AccountId = fixture.Account.Id, Name = "Proveedor Cuenta", TaxId = "30-444", Phone = "111", IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Suppliers.Add(supplier);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto deuda",
            InternalCode = "BUY-4",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 5,
            SalePrice = 10,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var createPurchase = new CreatePurchaseDocumentCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        await createPurchase.Handle(new CreatePurchaseDocumentCommand(supplier.Id, PurchaseDocumentType.PurchaseDocument, PurchaseDocumentStatus.Issued, DateTime.UtcNow, "A", null, new[] { new PurchaseLineInput(product.Id, null, null, 2, 10, 1) }), CancellationToken.None);
        await createPurchase.Handle(new CreatePurchaseDocumentCommand(supplier.Id, PurchaseDocumentType.PurchaseDocument, PurchaseDocumentStatus.Issued, DateTime.UtcNow, "B", null, new[] { new PurchaseLineInput(product.Id, null, null, 1, 15, 1) }), CancellationToken.None);

        var handler = new GetSupplierAccountBySupplierIdQueryHandler(db, fixture.Access);
        var result = await handler.Handle(new GetSupplierAccountBySupplierIdQuery(supplier.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(35, result.Data!.Balance);
        Assert.Equal(2, result.Data.MovementsCount);
    }

    [Fact]
    public async Task CreateSale_PersistsCustomerAccountMovement_ForConfirmedSale()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-customer-account@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente CC", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto saldo",
            InternalCode = "SAL-1",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 10,
            SalePrice = 25,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new CreateSaleCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreateSaleCommand(customer.Id, SaleStatus.Confirmed, DateTime.UtcNow, "Cuenta corriente", new[]
        {
            new CommercialLineInput(product.Id, null, null, 2, 25, 1)
        }), CancellationToken.None);

        Assert.True(result.Success);
        var movement = await db.CustomerAccountMovements.SingleAsync(x => x.SaleId == result.Data);
        Assert.Equal(50m, movement.DebitAmount);
        Assert.Equal(0m, movement.CreditAmount);
        Assert.Equal(CustomerAccountMovementType.SaleDocument, movement.MovementType);
    }

    [Fact]
    public async Task DraftPurchase_DoesNotImpactSupplierCurrentAccount()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-draft-purchase@test");

        var supplier = new Supplier { AccountId = fixture.Account.Id, Name = "Proveedor Draft", TaxId = "30-555", Phone = "111", IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Suppliers.Add(supplier);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            AccountId = fixture.Account.Id,
            Name = "Producto compra",
            InternalCode = "BUY-DRAFT",
            Description = "Producto",
            CategoryId = category.Id,
            Brand = "Marca",
            UnitOfMeasure = UnitOfMeasure.Unit,
            Cost = 5,
            SalePrice = 10,
            MinimumStock = 0,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new CreatePurchaseDocumentCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new CreatePurchaseDocumentCommand(supplier.Id, PurchaseDocumentType.PurchaseDocument, PurchaseDocumentStatus.Draft, DateTime.UtcNow, null, null, new[]
        {
            new PurchaseLineInput(product.Id, null, null, 2, 10, 1)
        }), CancellationToken.None);

        Assert.True(result.Success);
        var movement = await db.SupplierAccountMovements.SingleAsync(x => x.PurchaseDocumentId == result.Data);
        Assert.Equal(0m, movement.DebitAmount);
    }

    [Fact]
    public async Task GetCustomerCurrentAccount_ReturnsPendingAfterPartialAllocation()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-partial-collection@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Parcial", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var saleMovement = new CustomerAccountMovement
        {
            AccountId = fixture.Account.Id,
            CustomerId = customer.Id,
            MovementType = CustomerAccountMovementType.SaleDocument,
            PaymentMethod = PaymentMethod.AccountCredit,
            ReferenceNumber = "V-000001",
            IssuedAtUtc = DateTime.UtcNow.AddDays(-2),
            DebitAmount = 100m,
            CreditAmount = 0m,
            Description = "Venta V-000001",
            SaleId = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fixture.CurrentUser.UserId
        };
        var collectionMovement = new CustomerAccountMovement
        {
            AccountId = fixture.Account.Id,
            CustomerId = customer.Id,
            MovementType = CustomerAccountMovementType.Collection,
            PaymentMethod = PaymentMethod.Cash,
            ReferenceNumber = "COB-000001",
            IssuedAtUtc = DateTime.UtcNow.AddDays(-1),
            DebitAmount = 0m,
            CreditAmount = 40m,
            Description = "Cobro parcial",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fixture.CurrentUser.UserId
        };
        db.CustomerAccountMovements.AddRange(saleMovement, collectionMovement);
        await db.SaveChangesAsync();

        db.CustomerAccountAllocations.Add(new CustomerAccountAllocation
        {
            AccountId = fixture.Account.Id,
            SourceMovementId = collectionMovement.Id,
            TargetMovementId = saleMovement.Id,
            AppliedAtUtc = DateTime.UtcNow,
            Amount = 40m,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fixture.CurrentUser.UserId
        });
        await db.SaveChangesAsync();

        var handler = new GetCustomerCurrentAccountByCustomerIdQueryHandler(db, fixture.Access);
        var result = await handler.Handle(new GetCustomerCurrentAccountByCustomerIdQuery(customer.Id), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(60m, result.Data!.PendingDocumentsAmount);
        Assert.Single(result.Data.PendingDocuments);
        Assert.Equal(60m, result.Data.PendingDocuments.Single().PendingAmount);
    }

    [Fact]
    public async Task OpenCashSession_CreatesOpeningSessionAndMovement()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-cash@test");

        var handler = new OpenCashSessionCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await handler.Handle(new OpenCashSessionCommand(150m, "Inicio turno mañana"), CancellationToken.None);

        Assert.True(result.Success);
        var session = await db.CashSessions.Include(x => x.Movements).SingleAsync(x => x.Id == result.Data);
        Assert.Equal(CashSessionStatus.Open, session.Status);
        Assert.Equal(150m, session.OpeningBalance);
        Assert.Single(session.Movements);
        Assert.Equal(CashMovementOriginType.Opening, session.Movements.Single().OriginType);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<CommerceFixture> SeedCommerceAccountAsync(AppDbContext db, string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = email,
            Email = email,
            Nombre = "Owner",
            Apellido = "Tester",
            DefaultAccountId = 0
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var account = new Account
        {
            Name = $"Cuenta {email}",
            OwnerUserId = user.Id,
            IsActive = true
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        user.DefaultAccountId = account.Id;
        db.SaasPlanDefinitions.Add(new SaasPlanDefinition
        {
            Code = SaasPlanCode.Pro,
            Name = $"Pro-{email}",
            MaxProperties = 3,
            MaxUnits = 20,
            MaxUsers = 5,
            IncludesOperations = true,
            IncludesPublicPortal = true,
            IncludesReports = true
        });
        await db.SaveChangesAsync();

        var planId = await db.SaasPlanDefinitions.OrderByDescending(x => x.Id).Select(x => x.Id).FirstAsync();
        db.AccountSubscriptionPlans.Add(new AccountSubscriptionPlan
        {
            AccountId = account.Id,
            PlanDefinitionId = planId,
            IsActive = true,
            StartedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUser(user.Id);
        var access = new UserAccessService(db, currentUser);
        return new CommerceFixture(account, currentUser, access);
    }


    [Fact]
    public async Task CreateInvoice_AndSubmitToMockArca_PersistsFiscalResult()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-invoice@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Fiscal", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product { AccountId = fixture.Account.Id, Name = "Producto A", InternalCode = "INV-1", Description = "Producto", CategoryId = category.Id, Brand = "Marca", UnitOfMeasure = UnitOfMeasure.Unit, Cost = 10, SalePrice = 100, MinimumStock = 1, IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var saleId = (await new CreateSaleCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService())
            .Handle(new CreateSaleCommand(customer.Id, SaleStatus.Confirmed, DateTime.UtcNow, null, new[] { new CommercialLineInput(product.Id, null, null, 2, 100, 1) }), CancellationToken.None)).Data;

        db.FiscalConfigurations.Add(new FiscalConfiguration
        {
            AccountId = fixture.Account.Id,
            LegalName = "Demo SA",
            TaxIdentifier = "30712345678",
            PointOfSale = 5,
            DefaultInvoiceType = InvoiceType.InvoiceB,
            IntegrationMode = FiscalIntegrationMode.Mock,
            UseSandbox = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = fixture.CurrentUser.UserId
        });
        await db.SaveChangesAsync();

        var create = new CreateInvoiceCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var created = await create.Handle(new CreateInvoiceCommand(saleId, null, InvoiceType.InvoiceB, DateTime.UtcNow, 0.21m), CancellationToken.None);
        Assert.True(created.Success);

        var submit = new SubmitInvoiceToArcaCommandHandler(
            db,
            fixture.Access,
            fixture.CurrentUser,
            new TestAuditService(),
            new FiscalIntegrationService(new TestHttpClientFactory(), new MemoryCache(new MemoryCacheOptions()), new TestWebHostEnvironment(), NullLogger<FiscalIntegrationService>.Instance));
        var submitted = await submit.Handle(new SubmitInvoiceToArcaCommand(created.Data), CancellationToken.None);
        Assert.True(submitted.Success);

        var invoice = await db.CommercialInvoices.Include(x => x.FiscalSubmissions).SingleAsync(x => x.Id == created.Data);
        Assert.Equal(InvoiceStatus.Authorized, invoice.Status);
        Assert.False(string.IsNullOrWhiteSpace(invoice.Cae));
        Assert.Single(invoice.FiscalSubmissions);
        Assert.Equal(FiscalSubmissionStatus.Authorized, invoice.FiscalSubmissions.Single().Status);
    }

    [Fact]
    public async Task CreateDeliveryNote_DecrementsWarehouseStock_AndTracksPending()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-delivery@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Entrega", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        var branch = new Branch { AccountId = fixture.Account.Id, Name = "Casa Central", Code = "CC", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var warehouse = new Warehouse { AccountId = fixture.Account.Id, BranchId = branch.Id, Name = "Principal", IsActive = true };
        var product = new Product { AccountId = fixture.Account.Id, Name = "Producto R", InternalCode = "REM-1", Description = "Producto", CategoryId = category.Id, Brand = "Marca", UnitOfMeasure = UnitOfMeasure.Unit, Cost = 10, SalePrice = 50, MinimumStock = 1, IsActive = true };
        db.Warehouses.Add(warehouse);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.ProductWarehouseStocks.Add(new ProductWarehouseStock { AccountId = fixture.Account.Id, WarehouseId = warehouse.Id, ProductId = product.Id, QuantityOnHand = 10, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = fixture.CurrentUser.UserId });
        await db.SaveChangesAsync();

        var saleId = (await new CreateSaleCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService())
            .Handle(new CreateSaleCommand(customer.Id, SaleStatus.Confirmed, DateTime.UtcNow, null, new[] { new CommercialLineInput(product.Id, null, null, 4, 50, 1) }), CancellationToken.None)).Data;

        var create = new CreateDeliveryNoteCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var result = await create.Handle(new CreateDeliveryNoteCommand(saleId, warehouse.Id, null, DateTime.UtcNow, "Entrega parcial", new[] { new CreateDeliveryNoteLineInput(await db.SaleItems.Where(x => x.SaleId == saleId).Select(x => x.Id).SingleAsync(), 3m) }), CancellationToken.None);

        Assert.True(result.Success);
        var stock = await db.ProductWarehouseStocks.SingleAsync(x => x.AccountId == fixture.Account.Id && x.WarehouseId == warehouse.Id && x.ProductId == product.Id);
        var note = await db.DeliveryNotes.Include(x => x.Items).SingleAsync(x => x.Id == result.Data);
        Assert.Equal(7m, stock.QuantityOnHand);
        Assert.Equal(DeliveryNoteStatus.PartiallyDelivered, note.Status);
        Assert.Equal(1m, note.PendingQuantity);
        Assert.Single(note.Items);
    }

    [Fact]
    public async Task UpsertFiscalConfiguration_AndCreateSale_WritesDocumentHistory()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-history@test");

        var fiscal = new UpsertFiscalConfigurationCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService());
        var fiscalResult = await fiscal.Handle(new UpsertFiscalConfigurationCommand(0, "Demo SA", "30712345678", null, 1, InvoiceType.InvoiceB, FiscalIntegrationMode.Mock, true, true, null, null, null, null), CancellationToken.None);
        Assert.True(fiscalResult.Success);

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Hist", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "General", IsActive = true };
        db.Customers.Add(customer);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();
        var product = new Product { AccountId = fixture.Account.Id, Name = "Producto H", InternalCode = "HIS-1", Description = "Producto", CategoryId = category.Id, Brand = "Marca", UnitOfMeasure = UnitOfMeasure.Unit, Cost = 10, SalePrice = 60, MinimumStock = 0, IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var saleResult = await new CreateSaleCommandHandler(db, fixture.Access, fixture.CurrentUser, new TestAuditService())
            .Handle(new CreateSaleCommand(customer.Id, SaleStatus.Confirmed, DateTime.UtcNow, "Alta inicial", new[] { new CommercialLineInput(product.Id, null, null, 1, 60, 1) }), CancellationToken.None);
        Assert.True(saleResult.Success);

        var history = await db.DocumentChangeLogs.Where(x => x.AccountId == fixture.Account.Id).OrderBy(x => x.Id).ToListAsync();
        Assert.Contains(history, x => x.EntityName == nameof(FiscalConfiguration));
        Assert.Contains(history, x => x.EntityName == "Sale");
    }

    [Theory]
    [InlineData(InvoiceType.InvoiceB, CustomerType.Consumer, 99, 5)]
    [InlineData(InvoiceType.InvoiceB, CustomerType.Mixed, 80, 1)]
    [InlineData(InvoiceType.InvoiceA, CustomerType.Company, 80, 1)]
    [InlineData(InvoiceType.InvoiceC, CustomerType.Company, 80, 5)]
    public void FiscalIntegrationService_ResolveRecipientIvaConditionId_UsesExpectedFallback(
        InvoiceType invoiceType,
        CustomerType customerType,
        int documentType,
        int expected)
    {
        var method = typeof(FiscalIntegrationService).GetMethod("ResolveRecipientIvaConditionId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new object[] { invoiceType, customerType, documentType });

        Assert.Equal(expected, Assert.IsType<int>(result));
    }

    [Fact]
    public async Task UploadFiscalCredential_Fails_WhenCertificateExtensionIsInvalid()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-fiscal-file@test");

        var store = new InMemoryFiscalCredentialStore();
        var handler = new UploadFiscalCredentialCommandHandler(fixture.Access, store);
        var payload = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        var result = await handler.Handle(new UploadFiscalCredentialCommand("fiscal.exe", payload, "application/octet-stream", false), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("invalid_extension", result.ErrorCode);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task UploadFiscalCredential_Fails_WhenFileExceedsLimit()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-fiscal-limit@test");

        var store = new InMemoryFiscalCredentialStore();
        var handler = new UploadFiscalCredentialCommandHandler(fixture.Access, store);
        var oversized = new byte[Release6Helpers.MaxFiscalCredentialBytes + 1];
        var payload = Convert.ToBase64String(oversized);

        var result = await handler.Handle(new UploadFiscalCredentialCommand("cert.pem", payload, "application/x-pem-file", false), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("file_too_large", result.ErrorCode);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task CommercialDocumentPdfService_BuildInvoicePdfAsync_ReturnsPdfBytes()
    {
        var service = new CommercialDocumentPdfService();
        var invoice = new CommercialInvoice
        {
            Number = "FC-0001-00000001",
            InvoiceType = InvoiceType.InvoiceB,
            Status = InvoiceStatus.Authorized,
            Sale = new Sale { Number = "V-000001" },
            Customer = new Customer { Name = "Cliente PDF" },
            PointOfSale = 1,
            IssuedAtUtc = DateTime.UtcNow,
            CurrencyCode = "ARS",
            Subtotal = 100m,
            TaxAmount = 21m,
            OtherTaxesAmount = 0m,
            Total = 121m,
            Cae = "12345678901234",
            CaeDueDateUtc = DateTime.UtcNow.Date.AddDays(10),
            FiscalStatusDetail = "Autorizada",
            Items = new List<CommercialInvoiceItem>
            {
                new()
                {
                    Description = "Producto PDF",
                    InternalCode = "PDF-1",
                    Quantity = 1,
                    UnitPrice = 100m,
                    LineSubtotal = 100m,
                    TaxAmount = 21m,
                    SortOrder = 1
                }
            }
        };

        var result = await service.BuildInvoicePdfAsync(invoice, CancellationToken.None);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task CommercialDocumentPdfService_BuildQuotePdfAsync_ReturnsPdfBytes()
    {
        var service = new CommercialDocumentPdfService();
        var quote = new Quote
        {
            Number = "P-000001",
            Status = QuoteStatus.Sent,
            Customer = new Customer { Name = "Cliente PDF" },
            IssuedAtUtc = DateTime.UtcNow,
            ValidUntilUtc = DateTime.UtcNow.Date.AddDays(7),
            Observations = "Presupuesto de prueba",
            Subtotal = 100m,
            Total = 100m,
            Items = new List<QuoteItem>
            {
                new()
                {
                    Description = "Producto PDF",
                    InternalCode = "PDF-1",
                    Quantity = 1,
                    UnitPrice = 100m,
                    LineSubtotal = 100m,
                    SortOrder = 1
                }
            }
        };

        var result = await service.BuildQuotePdfAsync(quote, CancellationToken.None);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task CommercialDocumentPdfService_BuildSalePdfAsync_ReturnsPdfBytes()
    {
        var service = new CommercialDocumentPdfService();
        var sale = new Sale
        {
            Number = "V-000001",
            Status = SaleStatus.Confirmed,
            Customer = new Customer { Name = "Cliente PDF" },
            IssuedAtUtc = DateTime.UtcNow,
            Observations = "Venta de prueba",
            Subtotal = 100m,
            Total = 100m,
            Items = new List<SaleItem>
            {
                new()
                {
                    Description = "Producto PDF",
                    InternalCode = "PDF-1",
                    Quantity = 1,
                    UnitPrice = 100m,
                    LineSubtotal = 100m,
                    SortOrder = 1
                }
            }
        };

        var result = await service.BuildSalePdfAsync(sale, CancellationToken.None);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task CommercialDocumentPdfService_BuildDeliveryNotePdfAsync_ReturnsPdfBytes()
    {
        var service = new CommercialDocumentPdfService();
        var note = new DeliveryNote
        {
            Number = "RM-0001-00000001",
            Status = DeliveryNoteStatus.Delivered,
            Sale = new Sale { Number = "V-000001" },
            Customer = new Customer { Name = "Cliente PDF" },
            Warehouse = new Warehouse { Name = "Principal" },
            DeliveredAtUtc = DateTime.UtcNow,
            TotalQuantity = 2m,
            PendingQuantity = 0m,
            Items = new List<DeliveryNoteItem>
            {
                new()
                {
                    Description = "Producto PDF",
                    InternalCode = "PDF-1",
                    QuantityOrdered = 2m,
                    QuantityDelivered = 2m,
                    SortOrder = 1
                }
            }
        };

        var result = await service.BuildDeliveryNotePdfAsync(note, CancellationToken.None);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task GetProductsQuery_PerformanceBudget_P95AndPayloadWithinThreshold()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-performance@test");

        var category = new ProductCategory { AccountId = fixture.Account.Id, Name = "Perf", IsActive = true };
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var products = Enumerable.Range(1, 120)
            .Select(i => new Product
            {
                AccountId = fixture.Account.Id,
                Name = $"Producto {i:D3}",
                InternalCode = $"PERF-{i:D3}",
                Description = "Perf test",
                CategoryId = category.Id,
                Brand = "Perf",
                UnitOfMeasure = UnitOfMeasure.Unit,
                Cost = 10,
                SalePrice = 20 + i,
                MinimumStock = 0,
                IsActive = true
            })
            .ToList();
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var variants = products
            .Select((p, i) => new ProductVariant
            {
                AccountId = fixture.Account.Id,
                ProductId = p.Id,
                Name = $"Variante {i + 1:D3}",
                InternalCode = $"VPERF-{i + 1:D3}",
                AttributesSummary = "Default",
                Cost = 10,
                SalePrice = p.SalePrice,
                IsActive = true
            })
            .ToList();
        db.ProductVariants.AddRange(variants);
        await db.SaveChangesAsync();

        var handler = new GetProductsQueryHandler(db, fixture.Access);
        var samplesMs = new List<long>();
        AppResult<PagedResult<ProductListItemDto>>? result = null;

        for (var i = 0; i < 15; i++)
        {
            var sw = Stopwatch.StartNew();
            result = await handler.Handle(new GetProductsQuery(Page: 1, PageSize: 20), CancellationToken.None);
            sw.Stop();
            samplesMs.Add(sw.ElapsedMilliseconds);
        }

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);

        var ordered = samplesMs.OrderBy(x => x).ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(result.Data).Length;

        Assert.True(p95 <= 500, $"p95 fuera de presupuesto: {p95}ms");
        Assert.True(payloadBytes <= 200_000, $"payload fuera de presupuesto: {payloadBytes} bytes");
    }

    [Fact]
    public async Task GetCategoriesQuery_StaysWithinPerformanceBudget()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-perf-categories@test");

        var categories = Enumerable.Range(1, 120)
            .Select(i => new ProductCategory
            {
                AccountId = fixture.Account.Id,
                Name = $"Categoría {i:D3}",
                IsActive = i % 5 != 0
            })
            .ToList();
        db.ProductCategories.AddRange(categories);
        await db.SaveChangesAsync();

        var handler = new GetCategoriesQueryHandler(db, fixture.Access);
        var samples = new List<long>();
        AppResult<PagedResult<CategoryListItemDto>>? result = null;

        for (var i = 0; i < 15; i++)
        {
            var sw = Stopwatch.StartNew();
            result = await handler.Handle(new GetCategoriesQuery(Page: 1, PageSize: 20), CancellationToken.None);
            sw.Stop();
            samples.Add(sw.ElapsedMilliseconds);
        }

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);

        var ordered = samples.OrderBy(x => x).ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(result.Data).Length;

        Assert.True(p95 <= 500, $"p95 fuera de presupuesto categories: {p95}ms");
        Assert.True(payloadBytes <= 200_000, $"payload fuera de presupuesto categories: {payloadBytes} bytes");
    }

    [Fact]
    public async Task GetSalesQuery_StaysWithinPerformanceBudget()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-perf-sales@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Perf", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var sales = Enumerable.Range(1, 90)
            .Select(i => new Sale
            {
                AccountId = fixture.Account.Id,
                Number = $"V-{i:D6}",
                Status = i % 3 == 0 ? SaleStatus.Confirmed : SaleStatus.Draft,
                CustomerId = customer.Id,
                IssuedAtUtc = DateTime.UtcNow.AddDays(-i),
                Subtotal = 100 + i,
                Total = 100 + i,
                CreatedByUserId = fixture.CurrentUser.UserId
            })
            .ToList();
        db.Sales.AddRange(sales);
        await db.SaveChangesAsync();

        var handler = new GetSalesQueryHandler(db, fixture.Access);
        var samples = new List<long>();
        AppResult<PagedResult<SaleListItemDto>>? result = null;

        for (var i = 0; i < 15; i++)
        {
            var sw = Stopwatch.StartNew();
            result = await handler.Handle(new GetSalesQuery(Page: 1, PageSize: 20), CancellationToken.None);
            sw.Stop();
            samples.Add(sw.ElapsedMilliseconds);
        }

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);

        var ordered = samples.OrderBy(x => x).ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(result.Data).Length;

        Assert.True(p95 <= 500, $"p95 fuera de presupuesto sales: {p95}ms");
        Assert.True(payloadBytes <= 200_000, $"payload fuera de presupuesto sales: {payloadBytes} bytes");
    }

    [Fact]
    public async Task GetQuotesQuery_StaysWithinPerformanceBudget()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-perf-quotes@test");

        var customer = new Customer { AccountId = fixture.Account.Id, Name = "Cliente Perf", Phone = "123", Address = "Dir", City = "Ciudad", CustomerType = CustomerType.Mixed, IsActive = true };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var quotes = Enumerable.Range(1, 90)
            .Select(i => new Quote
            {
                AccountId = fixture.Account.Id,
                Number = $"P-{i:D6}",
                Status = i % 4 == 0 ? QuoteStatus.Approved : QuoteStatus.Draft,
                CustomerId = customer.Id,
                IssuedAtUtc = DateTime.UtcNow.AddDays(-i),
                ValidUntilUtc = DateTime.UtcNow.AddDays(30 - i),
                Subtotal = 120 + i,
                Total = 120 + i,
                CreatedByUserId = fixture.CurrentUser.UserId
            })
            .ToList();
        db.Quotes.AddRange(quotes);
        await db.SaveChangesAsync();

        var handler = new GetQuotesQueryHandler(db, fixture.Access);
        var samples = new List<long>();
        AppResult<PagedResult<QuoteListItemDto>>? result = null;

        for (var i = 0; i < 15; i++)
        {
            var sw = Stopwatch.StartNew();
            result = await handler.Handle(new GetQuotesQuery(Page: 1, PageSize: 20), CancellationToken.None);
            sw.Stop();
            samples.Add(sw.ElapsedMilliseconds);
        }

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);

        var ordered = samples.OrderBy(x => x).ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(result.Data).Length;

        Assert.True(p95 <= 500, $"p95 fuera de presupuesto quotes: {p95}ms");
        Assert.True(payloadBytes <= 200_000, $"payload fuera de presupuesto quotes: {payloadBytes} bytes");
    }

    [Fact]
    public async Task GetPurchasesQuery_StaysWithinPerformanceBudget()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCommerceAccountAsync(db, "owner-perf-purchases@test");

        var supplier = new Supplier { AccountId = fixture.Account.Id, Name = "Proveedor Perf", TaxId = "20123456789", Phone = "123", IsActive = true };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var purchases = Enumerable.Range(1, 90)
            .Select(i => new PurchaseDocument
            {
                AccountId = fixture.Account.Id,
                Number = $"OC-{i:D6}",
                DocumentType = PurchaseDocumentType.PurchaseDocument,
                Status = i % 2 == 0 ? PurchaseDocumentStatus.Issued : PurchaseDocumentStatus.Draft,
                SupplierId = supplier.Id,
                IssuedAtUtc = DateTime.UtcNow.AddDays(-i),
                SupplierDocumentNumber = $"F-{i:D6}",
                Subtotal = 90 + i,
                Total = 90 + i,
                CreatedByUserId = fixture.CurrentUser.UserId
            })
            .ToList();
        db.PurchaseDocuments.AddRange(purchases);
        await db.SaveChangesAsync();

        var handler = new GetPurchasesQueryHandler(db, fixture.Access);
        var samples = new List<long>();
        AppResult<PagedResult<PurchaseListItemDto>>? result = null;

        for (var i = 0; i < 15; i++)
        {
            var sw = Stopwatch.StartNew();
            result = await handler.Handle(new GetPurchasesQuery(Page: 1, PageSize: 20), CancellationToken.None);
            sw.Stop();
            samples.Add(sw.ElapsedMilliseconds);
        }

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);

        var ordered = samples.OrderBy(x => x).ToArray();
        var p95 = ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(result.Data).Length;

        Assert.True(p95 <= 500, $"p95 fuera de presupuesto purchases: {p95}ms");
        Assert.True(payloadBytes <= 200_000, $"payload fuera de presupuesto purchases: {payloadBytes} bytes");
    }

    private sealed record CommerceFixture(Account Account, TestCurrentUser CurrentUser, UserAccessService Access);

    private sealed class TestCurrentUser(string userId, params string[] roles) : ICurrentUser
    {
        private readonly HashSet<string> _roles = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        public string UserId { get; } = userId;
        public string? Email => null;
        public string? FullName => "Test User";
        public bool IsInRole(string role) => _roles.Contains(role);
    }

    private sealed class TestAuditService : IAuditService
    {
        public Task WriteAsync(int accountId, int? propertyId, string entityName, int? entityId, string action, string summary, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class TestIdentityService(string userId) : IIdentityService
    {
        public Task<(bool Success, string? UserId, string? Error)> CreateUserIfNotExistsAsync(string email, string password, CancellationToken ct)
            => Task.FromResult((true, userId, (string?)null));

        public Task<(bool Success, string? UserId, string? Error)> CreateUserIfNotExistsAsync(string email, string password, CancellationToken ct, string firstName, string lastName, bool isActive, int? defaultPropertyId, int defaultAccountId)
            => Task.FromResult((true, userId, (string?)null));

        public Task<(bool Success, string? UserId, string? Error)> FindUserIdByEmailAsync(string email, CancellationToken ct)
            => Task.FromResult((true, userId, (string?)null));
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class InMemoryFiscalCredentialStore : IFiscalCredentialStore
    {
        public int SaveCalls { get; private set; }

        public Task<string> SaveAsync(int accountId, string fileName, byte[] content, bool isPrivateKey, CancellationToken ct)
        {
            SaveCalls += 1;
            return Task.FromResult($"account-{accountId}/{fileName}");
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "GestAI.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
