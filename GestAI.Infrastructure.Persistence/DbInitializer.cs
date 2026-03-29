using GestAI.Domain.Common;
using GestAI.Domain.Entities;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestAI.Infrastructure.Persistence;

public static class DbInitializer
{
    public sealed record SeedOptions(
        string AdminEmail,
        string AdminPassword,
        string PropertyName,
        string[] UnitNames,
        string DemoOwnerEmail,
        string DemoOwnerPassword);

    public static async Task MigrateAndSeedAsync(AppDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger logger, SeedOptions options, CancellationToken ct = default)
    {
        var hasMigrations = db.Database.GetMigrations().Any();
        if (hasMigrations)
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);

        await SeedAsync(db, userManager, roleManager, logger, options, ct);
    }

    public static async Task SeedAsync(AppDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger logger, SeedOptions options, CancellationToken ct = default)
    {
        var hasMigrations = db.Database.GetMigrations().Any();

        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

        var admin = await EnsureUserAsync(userManager, options.AdminEmail, options.AdminPassword, "Admin", "GestAI");
        if (!await userManager.IsInRoleAsync(admin, "SuperAdmin"))
            await userManager.AddToRoleAsync(admin, "SuperAdmin");

        var adminAccount = await EnsureAccountAsync(db, admin, "Comercio Demo", ct);
        await EnsureProPlanAsync(db, adminAccount.Id, ct);
        await EnsureMembershipAsync(db, adminAccount.Id, admin.Id, InternalUserRole.Owner, canManageConfiguration: true, ct);
        await EnsureDefaultAccountAsync(db, admin.Id, adminAccount.Id, ct);

        var demoOwner = await EnsureUserAsync(userManager, options.DemoOwnerEmail, options.DemoOwnerPassword, "Daniel", "Demo");
        var demoAccount = await EnsureAccountAsync(db, demoOwner, "Daniel Demo Store", ct);
        await EnsureProPlanAsync(db, demoAccount.Id, ct);
        await EnsureMembershipAsync(db, demoAccount.Id, demoOwner.Id, InternalUserRole.Owner, canManageConfiguration: true, ct);
        await EnsureDefaultAccountAsync(db, demoOwner.Id, demoAccount.Id, ct);
        await SeedCommerceDemoDataAsync(db, demoAccount, demoOwner, ct);

        logger.LogInformation(
            "DB init + seed OK. SuperAdmin: {AdminEmail}. DemoOwner: {DemoEmail}. AdminAccountId: {AdminAccountId}. DemoAccountId: {DemoAccountId}. UsedMigrations: {UsedMigrations}",
            options.AdminEmail,
            options.DemoOwnerEmail,
            adminAccount.Id,
            demoAccount.Id,
            hasMigrations);
    }

    private static async Task<User> EnsureUserAsync(UserManager<User> userManager, string email, string password, string firstName, string lastName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nombre = firstName,
                Apellido = lastName,
                IsActive = true
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        return user;
    }

    private static async Task<Account> EnsureAccountAsync(AppDbContext db, User owner, string accountName, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.OwnerUserId == owner.Id, ct);
        if (account is not null)
            return account;

        account = new Account
        {
            OwnerUserId = owner.Id,
            Name = accountName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    private static async Task EnsureProPlanAsync(AppDbContext db, int accountId, CancellationToken ct)
    {
        if (!await db.SaasPlanDefinitions.AnyAsync(ct))
        {
            db.SaasPlanDefinitions.AddRange(
                new SaasPlanDefinition { Code = SaasPlanCode.Starter, Name = "Starter", MaxProperties = 1, MaxUnits = 5, MaxUsers = 2, IncludesOperations = false, IncludesPublicPortal = false, IncludesReports = false },
                new SaasPlanDefinition { Code = SaasPlanCode.Pro, Name = "Pro", MaxProperties = 3, MaxUnits = 20, MaxUsers = 5, IncludesOperations = true, IncludesPublicPortal = true, IncludesReports = true },
                new SaasPlanDefinition { Code = SaasPlanCode.Manager, Name = "Manager", MaxProperties = 10, MaxUnits = 100, MaxUsers = 20, IncludesOperations = true, IncludesPublicPortal = true, IncludesReports = true });
            await db.SaveChangesAsync(ct);
        }

        if (await db.AccountSubscriptionPlans.AnyAsync(x => x.AccountId == accountId && x.IsActive, ct))
            return;

        var proPlan = await db.SaasPlanDefinitions.FirstAsync(x => x.Code == SaasPlanCode.Pro, ct);
        db.AccountSubscriptionPlans.Add(new AccountSubscriptionPlan { AccountId = accountId, PlanDefinitionId = proPlan.Id, IsActive = true });
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureMembershipAsync(AppDbContext db, int accountId, string userId, InternalUserRole role, bool canManageConfiguration, CancellationToken ct)
    {
        if (await db.AccountUsers.AnyAsync(x => x.AccountId == accountId && x.UserId == userId, ct))
            return;

        db.AccountUsers.Add(new AccountUser
        {
            AccountId = accountId,
            UserId = userId,
            Role = role,
            IsActive = true,
            CanManageConfiguration = canManageConfiguration,
            InvitedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureDefaultAccountAsync(AppDbContext db, string userId, int accountId, CancellationToken ct)
    {
        var trackedUser = await db.Users.FirstAsync(u => u.Id == userId, ct);
        if (trackedUser.DefaultAccountId == accountId)
            return;

        trackedUser.DefaultAccountId = accountId;
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCommerceDemoDataAsync(AppDbContext db, Account account, User owner, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var ownerId = owner.Id;

        var branch = await db.Branches.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.Code == "CC", ct);
        if (branch is null)
        {
            branch = CreateAudited(new Branch
            {
                AccountId = account.Id,
                Name = "Casa Central",
                Code = "CC",
                IsActive = true
            }, ownerId, now);
            db.Branches.Add(branch);
            await db.SaveChangesAsync(ct);
        }

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.BranchId == branch.Id && x.Name == "Depósito Principal", ct);
        if (warehouse is null)
        {
            warehouse = CreateAudited(new Warehouse
            {
                AccountId = account.Id,
                BranchId = branch.Id,
                Name = "Depósito Principal",
                IsMain = true,
                IsActive = true
            }, ownerId, now);
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync(ct);
        }

        var secondaryWarehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.BranchId == branch.Id && x.Name == "Depósito Secundario", ct);
        if (secondaryWarehouse is null)
        {
            secondaryWarehouse = CreateAudited(new Warehouse
            {
                AccountId = account.Id,
                BranchId = branch.Id,
                Name = "Depósito Secundario",
                IsMain = false,
                IsActive = true
            }, ownerId, now);
            db.Warehouses.Add(secondaryWarehouse);
            await db.SaveChangesAsync(ct);
        }

        if (!await db.CashRegisters.AnyAsync(x => x.AccountId == account.Id, ct))
        {
            db.CashRegisters.Add(CreateAudited(new CashRegister
            {
                AccountId = account.Id,
                BranchId = branch.Id,
                Name = "Caja Mostrador",
                Code = "CAJA-1",
                IsDefault = true,
                IsActive = true
            }, ownerId, now));
            await db.SaveChangesAsync(ct);
        }

        var beverages = await EnsureCategoryAsync(db, account.Id, ownerId, "Bebidas", null, ct);
        var snacks = await EnsureCategoryAsync(db, account.Id, ownerId, "Snacks", null, ct);
        var cleaning = await EnsureCategoryAsync(db, account.Id, ownerId, "Limpieza", null, ct);

        var cola = await EnsureProductAsync(db, account.Id, ownerId, beverages.Id, "Gaseosa Cola 2.25L", "BEB-001", "779000000001", "Bebida cola retornable 2.25 litros", "Demo Drinks", UnitOfMeasure.Unit, 900m, 1500m, 10m, ct);
        var water = await EnsureProductAsync(db, account.Id, ownerId, beverages.Id, "Agua Mineral 1.5L", "BEB-002", "779000000002", "Agua mineral sin gas 1.5 litros", "Demo Drinks", UnitOfMeasure.Unit, 650m, 1200m, 12m, ct);
        var chips = await EnsureProductAsync(db, account.Id, ownerId, snacks.Id, "Papas Fritas 150g", "SNK-001", "779000000003", "Snack salado en paquete de 150g", "SnackCo", UnitOfMeasure.Unit, 500m, 980m, 15m, ct);
        var detergent = await EnsureProductAsync(db, account.Id, ownerId, cleaning.Id, "Detergente Limón 750ml", "LMP-001", "779000000004", "Detergente líquido concentrado", "CleanCo", UnitOfMeasure.Unit, 780m, 1390m, 8m, ct);

        await EnsureStockAsync(db, account.Id, warehouse.Id, cola.Id, 48m, ownerId, now, ct);
        await EnsureStockAsync(db, account.Id, warehouse.Id, water.Id, 60m, ownerId, now, ct);
        await EnsureStockAsync(db, account.Id, warehouse.Id, chips.Id, 35m, ownerId, now, ct);
        await EnsureStockAsync(db, account.Id, warehouse.Id, detergent.Id, 22m, ownerId, now, ct);

        await EnsureStockMovementAsync(db, account.Id, warehouse.Id, cola.Id, StockMovementType.Inbound, 48m, "Stock inicial de demo", ownerId, now, ct);
        await EnsureStockMovementAsync(db, account.Id, warehouse.Id, water.Id, StockMovementType.Inbound, 60m, "Stock inicial de demo", ownerId, now, ct);
        await EnsureStockMovementAsync(db, account.Id, warehouse.Id, chips.Id, StockMovementType.Inbound, 35m, "Stock inicial de demo", ownerId, now, ct);
        await EnsureStockMovementAsync(db, account.Id, warehouse.Id, detergent.Id, StockMovementType.Inbound, 22m, "Stock inicial de demo", ownerId, now, ct);

        var customerRetail = await EnsureCustomerAsync(db, account.Id, ownerId, "Consumidor Final Mostrador", "0", "11-4000-1000", "Av. Demo 123", "Buenos Aires", CustomerType.Consumer, ct);
        var customerCompany = await EnsureCustomerAsync(db, account.Id, ownerId, "Daniel Demo SRL", "30712345678", "11-4000-2000", "Av. Empresas 456", "Buenos Aires", CustomerType.Company, ct);
        await EnsureCustomerAsync(db, account.Id, ownerId, "Kiosco Centro", "20123456789", "11-4000-3000", "Calle Comercial 789", "Rosario", CustomerType.Mixed, ct);

        var supplierDrinks = await EnsureSupplierAsync(db, account.Id, ownerId, "Distribuidora Bebidas SA", "30711111111", "11-5000-1000", ct);
        await EnsureSupplierAsync(db, account.Id, ownerId, "Limpieza Mayorista SRL", "30722222222", "11-5000-2000", ct);

        var priceList = await db.PriceLists.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.Name == "Lista Mostrador", ct);
        if (priceList is null)
        {
            priceList = CreateAudited(new PriceList
            {
                AccountId = account.Id,
                Name = "Lista Mostrador",
                BaseMode = PriceListBaseMode.Manual,
                TargetType = PriceListTargetType.Product,
                IsActive = true
            }, ownerId, now);
            db.PriceLists.Add(priceList);
            await db.SaveChangesAsync(ct);
        }

        await EnsurePriceListItemAsync(db, account.Id, priceList.Id, cola.Id, 1500m, ownerId, now, ct);
        await EnsurePriceListItemAsync(db, account.Id, priceList.Id, water.Id, 1200m, ownerId, now, ct);
        await EnsurePriceListItemAsync(db, account.Id, priceList.Id, chips.Id, 980m, ownerId, now, ct);
        await EnsurePriceListItemAsync(db, account.Id, priceList.Id, detergent.Id, 1390m, ownerId, now, ct);

        if (!await db.FiscalConfigurations.AnyAsync(x => x.AccountId == account.Id && x.IsActive, ct))
        {
            db.FiscalConfigurations.Add(CreateAudited(new FiscalConfiguration
            {
                AccountId = account.Id,
                LegalName = "Daniel Demo SRL",
                TaxIdentifier = "30712345678",
                GrossIncomeTaxId = "902-123456-7",
                PointOfSale = 3,
                DefaultInvoiceType = InvoiceType.InvoiceB,
                IntegrationMode = FiscalIntegrationMode.Mock,
                UseSandbox = true,
                IsActive = true,
                Observations = "Configuración demo lista para pruebas; cambiar a ARCA WSFE y subir certificado/clave para homologación real."
            }, ownerId, now));
            await db.SaveChangesAsync(ct);
        }

        var quote = await db.Quotes.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.Number == "PRES-0001", ct);
        if (quote is null)
        {
            quote = CreateAudited(new Quote
            {
                AccountId = account.Id,
                Number = "PRES-0001",
                Status = QuoteStatus.Sent,
                CustomerId = customerCompany.Id,
                IssuedAtUtc = now.AddDays(-3),
                ValidUntilUtc = now.AddDays(7),
                Observations = "Presupuesto demo para pruebas comerciales.",
                Subtotal = 3960m,
                Total = 3960m
            }, ownerId, now.AddDays(-3));
            db.Quotes.Add(quote);
            await db.SaveChangesAsync(ct);

            db.QuoteItems.AddRange(
                CreateAudited(new QuoteItem { AccountId = account.Id, QuoteId = quote.Id, ProductId = cola.Id, Description = cola.Name, InternalCode = cola.InternalCode, Quantity = 2m, UnitPrice = 1500m, LineSubtotal = 3000m, SortOrder = 1 }, ownerId, now.AddDays(-3)),
                CreateAudited(new QuoteItem { AccountId = account.Id, QuoteId = quote.Id, ProductId = chips.Id, Description = chips.Name, InternalCode = chips.InternalCode, Quantity = 1m, UnitPrice = 960m, LineSubtotal = 960m, SortOrder = 2 }, ownerId, now.AddDays(-3)));
            await db.SaveChangesAsync(ct);
        }

        var sale = await db.Sales.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.Number == "VTA-0001", ct);
        if (sale is null)
        {
            sale = CreateAudited(new Sale
            {
                AccountId = account.Id,
                Number = "VTA-0001",
                Status = SaleStatus.Confirmed,
                CustomerId = customerRetail.Id,
                IssuedAtUtc = now.AddDays(-1),
                Observations = "Venta demo confirmada para pruebas de factura/remito.",
                Subtotal = 2700m,
                Total = 2700m
            }, ownerId, now.AddDays(-1));
            db.Sales.Add(sale);
            await db.SaveChangesAsync(ct);

            db.SaleItems.AddRange(
                CreateAudited(new SaleItem { AccountId = account.Id, SaleId = sale.Id, ProductId = cola.Id, Description = cola.Name, InternalCode = cola.InternalCode, Quantity = 1m, UnitPrice = 1500m, LineSubtotal = 1500m, SortOrder = 1 }, ownerId, now.AddDays(-1)),
                CreateAudited(new SaleItem { AccountId = account.Id, SaleId = sale.Id, ProductId = chips.Id, Description = chips.Name, InternalCode = chips.InternalCode, Quantity = 1m, UnitPrice = 1200m, LineSubtotal = 1200m, SortOrder = 2 }, ownerId, now.AddDays(-1)));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.CustomerAccountMovements.AnyAsync(x => x.AccountId == account.Id && x.SaleId == sale.Id, ct))
        {
            db.CustomerAccountMovements.Add(CreateAudited(new CustomerAccountMovement
            {
                AccountId = account.Id,
                CustomerId = sale.CustomerId,
                MovementType = CustomerAccountMovementType.SaleDocument,
                PaymentMethod = PaymentMethod.AccountCredit,
                SaleId = sale.Id,
                ReferenceNumber = sale.Number,
                IssuedAtUtc = sale.IssuedAtUtc,
                DebitAmount = sale.Total,
                CreditAmount = 0m,
                Description = $"Venta {sale.Number} registrada para pruebas demo.",
                Note = "Saldo pendiente demo"
            }, ownerId, now.AddDays(-1)));
            await db.SaveChangesAsync(ct);
        }

        var purchase = await db.PurchaseDocuments.FirstOrDefaultAsync(x => x.AccountId == account.Id && x.Number == "COMP-0001", ct);
        if (purchase is null)
        {
            purchase = CreateAudited(new PurchaseDocument
            {
                AccountId = account.Id,
                Number = "COMP-0001",
                DocumentType = PurchaseDocumentType.PurchaseDocument,
                Status = PurchaseDocumentStatus.Issued,
                SupplierId = supplierDrinks.Id,
                IssuedAtUtc = now.AddDays(-2),
                SupplierDocumentNumber = "FAC-A-0001",
                Observations = "Compra demo confirmada para pruebas de abastecimiento.",
                Subtotal = 21600m,
                Total = 21600m
            }, ownerId, now.AddDays(-2));
            db.PurchaseDocuments.Add(purchase);
            await db.SaveChangesAsync(ct);

            db.PurchaseDocumentItems.Add(
                CreateAudited(new PurchaseDocumentItem
                {
                    AccountId = account.Id,
                    PurchaseDocumentId = purchase.Id,
                    ProductId = cola.Id,
                    Description = cola.Name,
                    InternalCode = cola.InternalCode,
                    QuantityOrdered = 24m,
                    QuantityReceived = 24m,
                    UnitCost = 900m,
                    LineSubtotal = 21600m,
                    SortOrder = 1
                }, ownerId, now.AddDays(-2)));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.SupplierAccountMovements.AnyAsync(x => x.AccountId == account.Id && x.PurchaseDocumentId == purchase.Id, ct))
        {
            db.SupplierAccountMovements.Add(CreateAudited(new SupplierAccountMovement
            {
                AccountId = account.Id,
                SupplierId = purchase.SupplierId,
                MovementType = SupplierAccountMovementType.PurchaseDocument,
                PaymentMethod = PaymentMethod.AccountCredit,
                PurchaseDocumentId = purchase.Id,
                ReferenceNumber = purchase.Number,
                IssuedAtUtc = purchase.IssuedAtUtc,
                DebitAmount = purchase.Total,
                CreditAmount = 0m,
                Description = $"Compra {purchase.Number} registrada para pruebas demo.",
                Note = "Saldo proveedor demo"
            }, ownerId, now.AddDays(-2)));
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task<ProductCategory> EnsureCategoryAsync(AppDbContext db, int accountId, string userId, string name, int? parentCategoryId, CancellationToken ct)
    {
        var category = await db.ProductCategories.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Name == name, ct);
        if (category is not null)
            return category;

        category = CreateAudited(new ProductCategory
        {
            AccountId = accountId,
            Name = name,
            ParentCategoryId = parentCategoryId,
            IsActive = true
        }, userId, DateTime.UtcNow);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }

    private static async Task<Product> EnsureProductAsync(AppDbContext db, int accountId, string userId, int categoryId, string name, string internalCode, string? barcode, string description, string brand, UnitOfMeasure unit, decimal cost, decimal salePrice, decimal minimumStock, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(x => x.AccountId == accountId && x.InternalCode == internalCode, ct);
        if (product is not null)
            return product;

        product = CreateAudited(new Product
        {
            AccountId = accountId,
            Name = name,
            InternalCode = internalCode,
            Barcode = barcode,
            Description = description,
            CategoryId = categoryId,
            Brand = brand,
            UnitOfMeasure = unit,
            Cost = cost,
            SalePrice = salePrice,
            MinimumStock = minimumStock,
            IsActive = true
        }, userId, DateTime.UtcNow);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product;
    }

    private static async Task EnsureStockAsync(AppDbContext db, int accountId, int warehouseId, int productId, decimal quantity, string userId, DateTime occurredAtUtc, CancellationToken ct)
    {
        if (await db.ProductWarehouseStocks.AnyAsync(x => x.AccountId == accountId && x.WarehouseId == warehouseId && x.ProductId == productId, ct))
            return;

        db.ProductWarehouseStocks.Add(CreateAudited(new ProductWarehouseStock
        {
            AccountId = accountId,
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityOnHand = quantity,
            LastMovementAtUtc = occurredAtUtc
        }, userId, occurredAtUtc));
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureStockMovementAsync(AppDbContext db, int accountId, int warehouseId, int productId, StockMovementType movementType, decimal quantityDelta, string reason, string userId, DateTime occurredAtUtc, CancellationToken ct)
    {
        if (await db.StockMovements.AnyAsync(x => x.AccountId == accountId && x.WarehouseId == warehouseId && x.ProductId == productId && x.Reason == reason, ct))
            return;

        db.StockMovements.Add(CreateAudited(new StockMovement
        {
            AccountId = accountId,
            WarehouseId = warehouseId,
            ProductId = productId,
            MovementType = movementType,
            QuantityDelta = quantityDelta,
            Reason = reason,
            OccurredAtUtc = occurredAtUtc,
            ReferenceGroup = "DEMO-STOCK"
        }, userId, occurredAtUtc));
        await db.SaveChangesAsync(ct);
    }

    private static async Task<Customer> EnsureCustomerAsync(AppDbContext db, int accountId, string userId, string name, string? documentNumber, string phone, string address, string city, CustomerType customerType, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Name == name, ct);
        if (customer is not null)
            return customer;

        customer = CreateAudited(new Customer
        {
            AccountId = accountId,
            Name = name,
            DocumentNumber = documentNumber,
            Phone = phone,
            Address = address,
            City = city,
            CustomerType = customerType,
            IsActive = true
        }, userId, DateTime.UtcNow);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    private static async Task<Supplier> EnsureSupplierAsync(AppDbContext db, int accountId, string userId, string name, string taxId, string phone, CancellationToken ct)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(x => x.AccountId == accountId && x.TaxId == taxId, ct);
        if (supplier is not null)
            return supplier;

        supplier = CreateAudited(new Supplier
        {
            AccountId = accountId,
            Name = name,
            TaxId = taxId,
            Phone = phone,
            IsActive = true
        }, userId, DateTime.UtcNow);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return supplier;
    }

    private static async Task EnsurePriceListItemAsync(AppDbContext db, int accountId, int priceListId, int productId, decimal price, string userId, DateTime createdAtUtc, CancellationToken ct)
    {
        if (await db.PriceListItems.AnyAsync(x => x.AccountId == accountId && x.PriceListId == priceListId && x.ProductId == productId && x.ProductVariantId == null, ct))
            return;

        db.PriceListItems.Add(CreateAudited(new PriceListItem
        {
            AccountId = accountId,
            PriceListId = priceListId,
            ProductId = productId,
            ProductVariantId = null,
            Price = price,
            IsActive = true
        }, userId, createdAtUtc));
        await db.SaveChangesAsync(ct);
    }

    private static T CreateAudited<T>(T entity, string userId, DateTime createdAtUtc) where T : AuditableEntity
    {
        entity.CreatedByUserId = userId;
        entity.CreatedAtUtc = createdAtUtc;
        return entity;
    }
}
