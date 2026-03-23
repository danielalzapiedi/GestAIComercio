using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GestAI.Application.Commerce;

public sealed record GetPurchaseSeedDataQuery : IRequest<AppResult<PurchaseSeedDataDto>>;
public sealed record GetPurchasesQuery(string? Search = null, PurchaseDocumentStatus? Status = null, int? SupplierId = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<PurchaseListItemDto>>>;
public sealed record GetPurchaseByIdQuery(int Id) : IRequest<AppResult<PurchaseDetailDto>>;
public sealed record CreatePurchaseDocumentCommand(int SupplierId, PurchaseDocumentType DocumentType, PurchaseDocumentStatus Status, DateTime IssuedAtUtc, string? SupplierDocumentNumber, string? Observations, IReadOnlyList<PurchaseLineInput> Items) : IRequest<AppResult<int>>;
public sealed record UpdatePurchaseDocumentCommand(int Id, int SupplierId, PurchaseDocumentType DocumentType, PurchaseDocumentStatus Status, DateTime IssuedAtUtc, string? SupplierDocumentNumber, string? Observations, IReadOnlyList<PurchaseLineInput> Items) : IRequest<AppResult>;
public sealed record CreateGoodsReceiptCommand(int PurchaseDocumentId, int WarehouseId, DateTime ReceivedAtUtc, string? Observations, IReadOnlyList<GoodsReceiptLineInput> Items) : IRequest<AppResult<int>>;
public sealed record GetSupplierAccountsQuery(string? Search = null, bool? OnlyWithBalance = null) : IRequest<AppResult<List<SupplierAccountListItemDto>>>;
public sealed record GetSupplierAccountBySupplierIdQuery(int SupplierId) : IRequest<AppResult<SupplierAccountSummaryDto>>;

public sealed record PurchaseLineInput(int ProductId, int? ProductVariantId, string? Description, decimal Quantity, decimal UnitCost, int SortOrder);
public sealed record GoodsReceiptLineInput(int PurchaseDocumentItemId, decimal QuantityReceived);

internal static class PurchasingSupplyHelpers
{
    public const string CostStrategyKey = "weighted_average";
    public const string CostStrategyDescription = "Promedio ponderado móvil por SKU al confirmar cada ingreso. Si no hay stock previo, el costo recibido pasa a ser el nuevo costo.";

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequirePurchasesAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Purchases, ct);

    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static string BuildSkuLabel(string productName, string? variantName)
        => string.IsNullOrWhiteSpace(variantName) ? productName : $"{productName} · {variantName}";

    public static string BuildPurchaseNumber(int id) => $"OC-{id:D6}";
    public static string BuildReceiptNumber(int id) => $"ING-{id:D6}";

    public static async Task<Dictionary<(int ProductId, int? ProductVariantId), PurchaseSkuSeedData>> LoadSkuSeedDataAsync(IAppDbContext db, int accountId, CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new PurchaseSkuSeedData(x.Id, null, x.Name, null, x.InternalCode, x.Cost, x.IsActive))
            .ToListAsync(ct);

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new PurchaseSkuSeedData(x.ProductId, x.Id, x.Product.Name, x.Name, x.InternalCode, x.Cost, x.IsActive && x.Product.IsActive))
            .ToListAsync(ct);

        return products.Concat(variants).ToDictionary(x => (x.ProductId, x.ProductVariantId));
    }

    public static List<PreparedPurchaseLine> PrepareLines(IReadOnlyList<PurchaseLineInput> items, Dictionary<(int ProductId, int? ProductVariantId), PurchaseSkuSeedData> skuData)
    {
        var prepared = new List<PreparedPurchaseLine>();
        foreach (var item in items.OrderBy(x => x.SortOrder).ThenBy(x => x.ProductId))
        {
            if (!skuData.TryGetValue((item.ProductId, item.ProductVariantId), out var sku))
                throw new InvalidOperationException("sku_not_found");

            var description = string.IsNullOrWhiteSpace(item.Description)
                ? BuildSkuLabel(sku.ProductName, sku.VariantName)
                : item.Description.Trim();
            var unitCost = RoundMoney(item.UnitCost);
            var quantity = item.Quantity;
            prepared.Add(new PreparedPurchaseLine(item.ProductId, item.ProductVariantId, description, sku.InternalCode, quantity, unitCost, RoundMoney(quantity * unitCost), item.SortOrder, sku.CurrentCost));
        }

        return prepared;
    }

    public static void ApplyDocumentLines(PurchaseDocument document, IReadOnlyList<PreparedPurchaseLine> lines, ICurrentUser current)
    {
        document.Items.Clear();
        foreach (var line in lines.OrderBy(x => x.SortOrder))
        {
            document.Items.Add(new PurchaseDocumentItem
            {
                AccountId = document.AccountId,
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                Description = line.Description,
                InternalCode = line.InternalCode,
                QuantityOrdered = line.Quantity,
                QuantityReceived = 0m,
                UnitCost = line.UnitCost,
                LineSubtotal = line.LineSubtotal,
                SortOrder = line.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            });
        }

        document.Subtotal = RoundMoney(lines.Sum(x => x.LineSubtotal));
        document.Total = document.Subtotal;
    }

    public static PurchaseDetailDto MapDetail(PurchaseDocument document, SupplierAccountSummaryDto supplierAccount)
    {
        var ordered = document.Items.Sum(x => x.QuantityOrdered);
        var received = document.Items.Sum(x => x.QuantityReceived);
        return new PurchaseDetailDto(
            document.Id,
            document.Number,
            document.DocumentType,
            document.Status,
            document.SupplierId,
            document.Supplier.Name,
            document.IssuedAtUtc,
            document.SupplierDocumentNumber,
            document.Observations,
            document.Subtotal,
            document.Total,
            ordered,
            received,
            ordered - received,
            document.Items.OrderBy(x => x.SortOrder).Select(x => new PurchaseLineDto(x.Id, x.ProductId, x.ProductVariantId, x.Description, x.InternalCode, x.QuantityOrdered, x.QuantityReceived, x.QuantityOrdered - x.QuantityReceived, x.UnitCost, x.LineSubtotal, x.SortOrder, x.ProductVariantId.HasValue ? x.ProductVariant?.Cost ?? x.Product.Cost : x.Product.Cost)).ToList(),
            document.GoodsReceipts.OrderByDescending(x => x.ReceivedAtUtc).ThenByDescending(x => x.Id).Select(x => new GoodsReceiptListItemDto(x.Id, x.Number, x.WarehouseId, x.Warehouse.Name, x.ReceivedAtUtc, x.TotalQuantity, x.TotalCost, x.Items.Count, x.CreatedByUserId)).ToList(),
            supplierAccount,
            CostStrategyKey,
            CostStrategyDescription,
            document.CreatedByUserId,
            document.CreatedAtUtc,
            document.ModifiedByUserId,
            document.ModifiedAtUtc,
            received == 0m && document.Status != PurchaseDocumentStatus.Cancelled,
            document.Status is PurchaseDocumentStatus.Issued or PurchaseDocumentStatus.PartiallyReceived);
    }

    public static async Task<SupplierAccountSummaryDto> BuildSupplierAccountSummaryAsync(IAppDbContext db, int accountId, int supplierId, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking().Where(x => x.AccountId == accountId && x.Id == supplierId)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(ct);
        if (supplier is null)
            throw new InvalidOperationException("supplier_not_found");

        var movements = await db.SupplierAccountMovements.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.SupplierId == supplierId)
            .OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => new SupplierAccountMovementDto(x.Id, x.MovementType, x.ReferenceNumber, x.Description, x.IssuedAtUtc, x.DebitAmount, x.CreditAmount, x.DebitAmount - x.CreditAmount, x.PurchaseDocumentId, x.Note))
            .ToListAsync(ct);

        var debit = movements.Sum(x => x.DebitAmount);
        var credit = movements.Sum(x => x.CreditAmount);
        return new SupplierAccountSummaryDto(supplier.Id, supplier.Name, debit - credit, debit, credit, movements.Count, movements.FirstOrDefault()?.IssuedAtUtc, movements.Take(10).ToList());
    }

    public static async Task UpsertSupplierMovementAsync(IAppDbContext db, PurchaseDocument document, ICurrentUser current, CancellationToken ct)
    {
        var movement = await db.SupplierAccountMovements.FirstOrDefaultAsync(x => x.AccountId == document.AccountId && x.PurchaseDocumentId == document.Id, ct);
        var debit = FinancialHelpers.PurchaseGeneratesDebt(document.Status) ? document.Total : 0m;
        if (movement is null)
        {
            movement = new SupplierAccountMovement
            {
                AccountId = document.AccountId,
                SupplierId = document.SupplierId,
                PurchaseDocumentId = document.Id,
                MovementType = SupplierAccountMovementType.PurchaseDocument,
                PaymentMethod = PaymentMethod.AccountCredit,
                ReferenceNumber = document.Number,
                IssuedAtUtc = document.IssuedAtUtc,
                DebitAmount = debit,
                CreditAmount = 0m,
                Description = $"Compra {document.Number}",
                Note = document.Status == PurchaseDocumentStatus.Cancelled ? "Comprobante cancelado: impacto en saldo anulado." : document.Observations
            };
            CommerceFeatureHelpers.TouchCreate(movement, current);
            db.SupplierAccountMovements.Add(movement);
            return;
        }

        movement.SupplierId = document.SupplierId;
        movement.PaymentMethod = PaymentMethod.AccountCredit;
        movement.ReferenceNumber = document.Number;
        movement.IssuedAtUtc = document.IssuedAtUtc;
        movement.DebitAmount = debit;
        movement.CreditAmount = 0m;
        movement.Description = $"Compra {document.Number}";
        movement.Note = document.Status == PurchaseDocumentStatus.Cancelled ? "Comprobante cancelado: impacto en saldo anulado." : document.Observations;
        CommerceFeatureHelpers.TouchUpdate(movement, current);
    }

    public static async Task<ProductWarehouseStock> GetOrCreateStockAsync(IAppDbContext db, int accountId, int productId, int? productVariantId, int warehouseId, ICurrentUser current, CancellationToken ct)
    {
        var stock = await db.ProductWarehouseStocks.FirstOrDefaultAsync(x => x.AccountId == accountId && x.ProductId == productId && x.ProductVariantId == productVariantId && x.WarehouseId == warehouseId, ct);
        if (stock is not null) return stock;

        stock = new ProductWarehouseStock
        {
            AccountId = accountId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            WarehouseId = warehouseId,
            QuantityOnHand = 0m
        };
        CommerceFeatureHelpers.TouchCreate(stock, current);
        db.ProductWarehouseStocks.Add(stock);
        return stock;
    }

    public static async Task ApplyWeightedAverageCostAsync(IAppDbContext db, int accountId, int productId, int? productVariantId, decimal receivedQuantity, decimal unitCost, ICurrentUser current, CancellationToken ct)
    {
        var currentQuantity = await db.ProductWarehouseStocks.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.ProductId == productId && x.ProductVariantId == productVariantId)
            .SumAsync(x => (decimal?)x.QuantityOnHand, ct) ?? 0m;

        if (productVariantId.HasValue)
        {
            var variant = await db.ProductVariants.FirstAsync(x => x.AccountId == accountId && x.Id == productVariantId.Value, ct);
            variant.Cost = currentQuantity <= 0m
                ? RoundMoney(unitCost)
                : RoundMoney(((variant.Cost * currentQuantity) + (unitCost * receivedQuantity)) / (currentQuantity + receivedQuantity));
            CommerceFeatureHelpers.TouchUpdate(variant, current);
            return;
        }

        var product = await db.Products.FirstAsync(x => x.AccountId == accountId && x.Id == productId, ct);
        product.Cost = currentQuantity <= 0m
            ? RoundMoney(unitCost)
            : RoundMoney(((product.Cost * currentQuantity) + (unitCost * receivedQuantity)) / (currentQuantity + receivedQuantity));
        CommerceFeatureHelpers.TouchUpdate(product, current);
    }

    public static PurchaseDocumentStatus ResolveStatusAfterReceipt(PurchaseDocument document)
    {
        if (document.Status == PurchaseDocumentStatus.Cancelled)
            return PurchaseDocumentStatus.Cancelled;

        var ordered = document.Items.Sum(x => x.QuantityOrdered);
        var received = document.Items.Sum(x => x.QuantityReceived);
        if (received <= 0m) return PurchaseDocumentStatus.Issued;
        if (received >= ordered) return PurchaseDocumentStatus.Received;
        return PurchaseDocumentStatus.PartiallyReceived;
    }

    public static async Task<PurchaseDocument?> LoadPurchaseAggregateAsync(IAppDbContext db, int accountId, int id, CancellationToken ct)
        => await db.PurchaseDocuments
            .Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Items).ThenInclude(x => x.ProductVariant)
            .Include(x => x.GoodsReceipts).ThenInclude(x => x.Warehouse)
            .Include(x => x.GoodsReceipts).ThenInclude(x => x.Items)
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == id, ct);

    public sealed record PurchaseSkuSeedData(int ProductId, int? ProductVariantId, string ProductName, string? VariantName, string InternalCode, decimal CurrentCost, bool IsActive);
    public sealed record PreparedPurchaseLine(int ProductId, int? ProductVariantId, string Description, string InternalCode, decimal Quantity, decimal UnitCost, decimal LineSubtotal, int SortOrder, decimal CurrentCost);
}

public sealed class GetPurchasesQueryValidator : AbstractValidator<GetPurchasesQuery>
{
    public GetPurchasesQueryValidator() => CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
}

public sealed class CreatePurchaseDocumentCommandValidator : AbstractValidator<CreatePurchaseDocumentCommand>
{
    public CreatePurchaseDocumentCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdatePurchaseDocumentCommandValidator : AbstractValidator<UpdatePurchaseDocumentCommand>
{
    public UpdatePurchaseDocumentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.PurchaseDocumentId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.PurchaseDocumentItemId).GreaterThan(0);
            item.RuleFor(x => x.QuantityReceived).GreaterThan(0);
        });
    }
}

public sealed class GetPurchaseSeedDataQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetPurchaseSeedDataQuery, AppResult<PurchaseSeedDataDto>>
{
    public async Task<AppResult<PurchaseSeedDataDto>> Handle(GetPurchaseSeedDataQuery request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult<PurchaseSeedDataDto>.Fail(scope.ErrorCode, scope.Message);

        var suppliers = await db.Suppliers.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IsActive).OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IsActive).OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        var skuData = await PurchasingSupplyHelpers.LoadSkuSeedDataAsync(db, scope.AccountId, ct);
        var products = skuData.Values.Where(x => x.ProductVariantId is null).OrderBy(x => x.ProductName)
            .Select(x => new PurchaseSkuLookupDto(x.ProductId, x.ProductVariantId, x.ProductName, x.InternalCode, x.CurrentCost, x.IsActive)).ToList();
        var variants = skuData.Values.Where(x => x.ProductVariantId.HasValue).OrderBy(x => x.ProductName).ThenBy(x => x.VariantName)
            .Select(x => new PurchaseSkuLookupDto(x.ProductId, x.ProductVariantId, PurchasingSupplyHelpers.BuildSkuLabel(x.ProductName, x.VariantName), x.InternalCode, x.CurrentCost, x.IsActive)).ToList();

        return AppResult<PurchaseSeedDataDto>.Ok(new PurchaseSeedDataDto(suppliers, warehouses, products, variants, PurchasingSupplyHelpers.CostStrategyKey, PurchasingSupplyHelpers.CostStrategyDescription));
    }
}

public sealed class GetPurchasesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetPurchasesQuery, AppResult<PagedResult<PurchaseListItemDto>>>
{
    public async Task<AppResult<PagedResult<PurchaseListItemDto>>> Handle(GetPurchasesQuery request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult<PagedResult<PurchaseListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var query = db.PurchaseDocuments.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Number.Contains(search) || x.Supplier.Name.Contains(search) || (x.SupplierDocumentNumber != null && x.SupplierDocumentNumber.Contains(search)));
        }
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        if (request.SupplierId.HasValue) query = query.Where(x => x.SupplierId == request.SupplierId.Value);

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, CommerceFeatureHelpers.MaxPageSize);
        var items = await query.OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseListItemDto(
                x.Id,
                x.Number,
                x.DocumentType,
                x.Status,
                x.SupplierId,
                x.Supplier.Name,
                x.IssuedAtUtc,
                x.SupplierDocumentNumber,
                x.Subtotal,
                x.Total,
                x.Items.Sum(i => i.QuantityOrdered),
                x.Items.Sum(i => i.QuantityReceived),
                x.Items.Count,
                x.GoodsReceipts.OrderByDescending(r => r.ReceivedAtUtc).Select(r => (DateTime?)r.ReceivedAtUtc).FirstOrDefault()))
            .ToListAsync(ct);

        return AppResult<PagedResult<PurchaseListItemDto>>.Ok(new PagedResult<PurchaseListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetPurchaseByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetPurchaseByIdQuery, AppResult<PurchaseDetailDto>>
{
    public async Task<AppResult<PurchaseDetailDto>> Handle(GetPurchaseByIdQuery request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult<PurchaseDetailDto>.Fail(scope.ErrorCode, scope.Message);

        var document = await PurchasingSupplyHelpers.LoadPurchaseAggregateAsync(db, scope.AccountId, request.Id, ct);
        if (document is null) return AppResult<PurchaseDetailDto>.Fail("not_found", "Compra no encontrada.");

        var supplierAccount = await PurchasingSupplyHelpers.BuildSupplierAccountSummaryAsync(db, scope.AccountId, document.SupplierId, ct);
        return AppResult<PurchaseDetailDto>.Ok(PurchasingSupplyHelpers.MapDetail(document, supplierAccount));
    }
}

public sealed class CreatePurchaseDocumentCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreatePurchaseDocumentCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreatePurchaseDocumentCommand request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        var supplier = await db.Suppliers.FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.SupplierId, ct);
        if (supplier is null) return AppResult<int>.Fail("supplier_not_found", "Proveedor no encontrado.");

        var skuData = await PurchasingSupplyHelpers.LoadSkuSeedDataAsync(db, scope.AccountId, ct);
        List<PurchasingSupplyHelpers.PreparedPurchaseLine> lines;
        try { lines = PurchasingSupplyHelpers.PrepareLines(request.Items, skuData); }
        catch (InvalidOperationException ex) when (ex.Message == "sku_not_found") { return AppResult<int>.Fail("sku_not_found", "Alguno de los ítems de compra ya no existe."); }

        var document = new PurchaseDocument
        {
            AccountId = scope.AccountId,
            SupplierId = request.SupplierId,
            DocumentType = request.DocumentType,
            Status = request.Status,
            IssuedAtUtc = request.IssuedAtUtc,
            SupplierDocumentNumber = string.IsNullOrWhiteSpace(request.SupplierDocumentNumber) ? null : request.SupplierDocumentNumber.Trim(),
            Observations = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim(),
            Number = "TEMP"
        };
        CommerceFeatureHelpers.TouchCreate(document, current);
        PurchasingSupplyHelpers.ApplyDocumentLines(document, lines, current);
        db.PurchaseDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        document.Number = PurchasingSupplyHelpers.BuildPurchaseNumber(document.Id);
        await PurchasingSupplyHelpers.UpsertSupplierMovementAsync(db, document, current, ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(PurchaseDocument), document.Id, "created", $"Compra {document.Number} creada para {supplier.Name} por {document.Total:C}.", ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, nameof(PurchaseDocument), document.Id, document.Number, "created", $"Compra {document.Number} creada.", new { document.SupplierId, document.Status, document.Total }, null, ct);
        return AppResult<int>.Ok(document.Id);
    }
}

public sealed class UpdatePurchaseDocumentCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdatePurchaseDocumentCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdatePurchaseDocumentCommand request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);

        var document = await db.PurchaseDocuments.Include(x => x.Items).FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);
        if (document is null) return AppResult.Fail("not_found", "Compra no encontrada.");
        if (document.Items.Any(x => x.QuantityReceived > 0m)) return AppResult.Fail("received_lock", "La compra ya tiene ingresos registrados y no puede reeditarse.");

        var supplier = await db.Suppliers.FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.SupplierId, ct);
        if (supplier is null) return AppResult.Fail("supplier_not_found", "Proveedor no encontrado.");

        var skuData = await PurchasingSupplyHelpers.LoadSkuSeedDataAsync(db, scope.AccountId, ct);
        List<PurchasingSupplyHelpers.PreparedPurchaseLine> lines;
        try { lines = PurchasingSupplyHelpers.PrepareLines(request.Items, skuData); }
        catch (InvalidOperationException ex) when (ex.Message == "sku_not_found") { return AppResult.Fail("sku_not_found", "Alguno de los ítems de compra ya no existe."); }

        document.SupplierId = request.SupplierId;
        document.DocumentType = request.DocumentType;
        document.Status = request.Status;
        document.IssuedAtUtc = request.IssuedAtUtc;
        document.SupplierDocumentNumber = string.IsNullOrWhiteSpace(request.SupplierDocumentNumber) ? null : request.SupplierDocumentNumber.Trim();
        document.Observations = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim();
        PurchasingSupplyHelpers.ApplyDocumentLines(document, lines, current);
        CommerceFeatureHelpers.TouchUpdate(document, current);
        await PurchasingSupplyHelpers.UpsertSupplierMovementAsync(db, document, current, ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(PurchaseDocument), document.Id, "updated", $"Compra {document.Number} actualizada.", ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, nameof(PurchaseDocument), document.Id, document.Number, "updated", $"Compra {document.Number} actualizada.", new { document.SupplierId, document.Status, document.Total, document.SupplierDocumentNumber }, null, ct);
        return AppResult.Ok();
    }
}

public sealed class CreateGoodsReceiptCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateGoodsReceiptCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateGoodsReceiptCommand request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await db.PurchaseDocuments
            .Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Items).ThenInclude(x => x.ProductVariant)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.PurchaseDocumentId, ct);
        if (document is null) return AppResult<int>.Fail("not_found", "Compra no encontrada.");
        if (document.Status == PurchaseDocumentStatus.Cancelled) return AppResult<int>.Fail("invalid_status", "No se puede ingresar mercadería para una compra cancelada.");

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.WarehouseId && x.IsActive, ct);
        if (warehouse is null) return AppResult<int>.Fail("warehouse_not_found", "Depósito destino no encontrado.");

        var requestedIds = request.Items.Select(x => x.PurchaseDocumentItemId).ToHashSet();
        var items = document.Items.Where(x => requestedIds.Contains(x.Id)).ToDictionary(x => x.Id);
        if (items.Count != requestedIds.Count) return AppResult<int>.Fail("item_not_found", "Alguno de los ítems a recibir no pertenece a la compra.");

        var receipt = new GoodsReceipt
        {
            AccountId = scope.AccountId,
            PurchaseDocumentId = document.Id,
            WarehouseId = warehouse.Id,
            ReceivedAtUtc = request.ReceivedAtUtc,
            Observations = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim(),
            Number = "TEMP"
        };
        CommerceFeatureHelpers.TouchCreate(receipt, current);

        foreach (var input in request.Items.OrderBy(x => x.PurchaseDocumentItemId))
        {
            var item = items[input.PurchaseDocumentItemId];
            var pending = item.QuantityOrdered - item.QuantityReceived;
            if (pending <= 0m) return AppResult<int>.Fail("no_pending_quantity", $"El ítem {item.Description} ya fue recibido completamente.");
            if (input.QuantityReceived > pending) return AppResult<int>.Fail("quantity_exceeded", $"La cantidad recibida para {item.Description} supera lo pendiente.");

            var lineSubtotal = PurchasingSupplyHelpers.RoundMoney(input.QuantityReceived * item.UnitCost);
            receipt.Items.Add(new GoodsReceiptItem
            {
                AccountId = scope.AccountId,
                PurchaseDocumentItemId = item.Id,
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                Description = item.Description,
                InternalCode = item.InternalCode,
                QuantityReceived = input.QuantityReceived,
                UnitCost = item.UnitCost,
                LineSubtotal = lineSubtotal,
                SortOrder = item.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            });

            item.QuantityReceived += input.QuantityReceived;
            CommerceFeatureHelpers.TouchUpdate(item, current);

            await PurchasingSupplyHelpers.ApplyWeightedAverageCostAsync(db, scope.AccountId, item.ProductId, item.ProductVariantId, input.QuantityReceived, item.UnitCost, current, ct);
            var stock = await PurchasingSupplyHelpers.GetOrCreateStockAsync(db, scope.AccountId, item.ProductId, item.ProductVariantId, warehouse.Id, current, ct);
            stock.QuantityOnHand += input.QuantityReceived;
            stock.LastMovementAtUtc = request.ReceivedAtUtc;
            CommerceFeatureHelpers.TouchUpdate(stock, current);

            var movement = new StockMovement
            {
                AccountId = scope.AccountId,
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                WarehouseId = warehouse.Id,
                MovementType = StockMovementType.Inbound,
                QuantityDelta = input.QuantityReceived,
                Reason = "Ingreso por compra",
                Note = $"{document.Number} / {receipt.Number}",
                ReferenceGroup = $"purchase:{document.Id}",
                OccurredAtUtc = request.ReceivedAtUtc
            };
            CommerceFeatureHelpers.TouchCreate(movement, current);
            db.StockMovements.Add(movement);
        }

        receipt.TotalQuantity = PurchasingSupplyHelpers.RoundMoney(receipt.Items.Sum(x => x.QuantityReceived));
        receipt.TotalCost = PurchasingSupplyHelpers.RoundMoney(receipt.Items.Sum(x => x.LineSubtotal));
        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);

        receipt.Number = PurchasingSupplyHelpers.BuildReceiptNumber(receipt.Id);
        foreach (var movement in db.StockMovements.Local.Where(x => x.ReferenceGroup == $"purchase:{document.Id}" && x.Note != null && x.Note.EndsWith("/ TEMP")))
        {
            movement.Note = movement.Note!.Replace("/ TEMP", $"/ {receipt.Number}");
        }

        document.Status = PurchasingSupplyHelpers.ResolveStatusAfterReceipt(document);
        CommerceFeatureHelpers.TouchUpdate(document, current);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(GoodsReceipt), receipt.Id, "created", $"Ingreso {receipt.Number} para compra {document.Number} en depósito {warehouse.Name}.", ct);
        return AppResult<int>.Ok(receipt.Id);
    }
}

public sealed class GetSupplierAccountsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSupplierAccountsQuery, AppResult<List<SupplierAccountListItemDto>>>
{
    public async Task<AppResult<List<SupplierAccountListItemDto>>> Handle(GetSupplierAccountsQuery request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult<List<SupplierAccountListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var suppliers = db.Suppliers.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            suppliers = suppliers.Where(x => x.Name.Contains(search) || x.TaxId.Contains(search));
        }

        var supplierRows = await suppliers
            .Select(x => new { x.Id, x.Name, x.TaxId, x.IsActive })
            .ToListAsync(ct);

        var supplierIds = supplierRows.Select(x => x.Id).ToArray();

        var movementStats = await db.SupplierAccountMovements.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId && supplierIds.Contains(x.SupplierId))
            .GroupBy(x => x.SupplierId)
            .Select(x => new
            {
                SupplierId = x.Key,
                Debit = x.Sum(m => m.DebitAmount),
                Credit = x.Sum(m => m.CreditAmount),
                LastMovementAtUtc = x.Max(m => (DateTime?)m.IssuedAtUtc)
            })
            .ToDictionaryAsync(x => x.SupplierId, ct);

        var purchaseCounts = await db.PurchaseDocuments.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId && supplierIds.Contains(x.SupplierId))
            .GroupBy(x => x.SupplierId)
            .Select(x => new { SupplierId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Count, ct);

        var items = supplierRows.Select(x =>
        {
            movementStats.TryGetValue(x.Id, out var movement);
            purchaseCounts.TryGetValue(x.Id, out var documentsCount);
            var debit = movement?.Debit ?? 0m;
            var credit = movement?.Credit ?? 0m;
            return new SupplierAccountListItemDto(
                x.Id,
                x.Name,
                x.TaxId,
                x.IsActive,
                debit - credit,
                debit,
                credit,
                documentsCount,
                movement?.LastMovementAtUtc);
        });

        if (request.OnlyWithBalance == true)
            items = items.Where(x => x.Balance != 0m);

        return AppResult<List<SupplierAccountListItemDto>>.Ok(items.OrderByDescending(x => x.Balance).ThenBy(x => x.SupplierName).ToList());
    }
}

public sealed class GetSupplierAccountBySupplierIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSupplierAccountBySupplierIdQuery, AppResult<SupplierAccountSummaryDto>>
{
    public async Task<AppResult<SupplierAccountSummaryDto>> Handle(GetSupplierAccountBySupplierIdQuery request, CancellationToken ct)
    {
        var scope = await PurchasingSupplyHelpers.RequirePurchasesAsync(access, ct);
        if (!scope.Success) return AppResult<SupplierAccountSummaryDto>.Fail(scope.ErrorCode, scope.Message);

        var exists = await db.Suppliers.AsNoTracking().AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.SupplierId, ct);
        if (!exists) return AppResult<SupplierAccountSummaryDto>.Fail("not_found", "Proveedor no encontrado.");
        return AppResult<SupplierAccountSummaryDto>.Ok(await PurchasingSupplyHelpers.BuildSupplierAccountSummaryAsync(db, scope.AccountId, request.SupplierId, ct));
    }
}
