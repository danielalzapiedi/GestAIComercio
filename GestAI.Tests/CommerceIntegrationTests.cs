using GestAI.Application.Abstractions;
using GestAI.Application.Commerce;
using GestAI.Application.Saas;
using GestAI.Domain.Entities;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using GestAI.Infrastructure.Persistence;
using GestAI.Infrastructure.Saas;
using Microsoft.EntityFrameworkCore;
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
}
