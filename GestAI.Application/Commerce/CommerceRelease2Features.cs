using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text;

namespace GestAI.Application.Commerce;

public sealed record GetInventorySeedDataQuery : IRequest<AppResult<InventorySeedDataDto>>;
public sealed record GetInventoryOverviewQuery(int? WarehouseId = null, int? ProductId = null, int? ProductVariantId = null, string? Search = null) : IRequest<AppResult<InventoryOverviewDto>>;
public sealed record GetStockMovementsQuery(int? WarehouseId = null, int? ProductId = null, int? ProductVariantId = null, StockMovementType? MovementType = null, int Take = 30) : IRequest<AppResult<List<StockMovementListItemDto>>>;
public sealed record RecordStockMovementCommand(int ProductId, int? ProductVariantId, int WarehouseId, int? CounterpartWarehouseId, StockMovementType MovementType, decimal Quantity, string Reason, string? Note, DateTime? OccurredAtUtc) : IRequest<AppResult<int>>;

public sealed record GetPriceListSeedDataQuery : IRequest<AppResult<PriceListSeedDataDto>>;
public sealed record GetPriceListsQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<PriceListListItemDto>>>;
public sealed record GetPriceListByIdQuery(int Id) : IRequest<AppResult<PriceListDetailDto>>;
public sealed record GetPriceListItemsQuery(int PriceListId, string? Search = null) : IRequest<AppResult<List<PriceListItemDto>>>;
public sealed record CreatePriceListCommand(string Name, PriceListBaseMode BaseMode, PriceListTargetType TargetType, bool IsActive) : IRequest<AppResult<int>>;
public sealed record UpdatePriceListCommand(int Id, string Name, PriceListBaseMode BaseMode, PriceListTargetType TargetType, bool IsActive) : IRequest<AppResult>;
public sealed record SetPriceListItemCommand(int PriceListId, int ProductId, int? ProductVariantId, decimal Price, bool IsActive) : IRequest<AppResult<int>>;
public sealed record ApplyPriceListAdjustmentCommand(int PriceListId, BulkPriceAdjustmentType AdjustmentType, decimal Value, bool IncludeInactiveProducts, int? CategoryId) : IRequest<AppResult<BulkPriceUpdateResultDto>>;

public sealed record PreviewProductImportCommand(string CsvContent, bool UpsertExisting) : IRequest<AppResult<ProductImportPreviewDto>>;
public sealed record ApplyProductImportCommand(string CsvContent, bool UpsertExisting) : IRequest<AppResult<ProductImportResultDto>>;

file static class CommerceRelease2Helpers
{
    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireProductsAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);

    public static async Task<(bool Success, PriceList PriceList, string ErrorCode, string Message)> GetPriceListAsync(IAppDbContext db, int accountId, int priceListId, CancellationToken ct)
    {
        var priceList = await db.PriceLists.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == priceListId, ct);
        if (priceList is null)
            return (false, null!, "not_found", "Lista de precios no encontrada.");

        return (true, priceList, string.Empty, string.Empty);
    }

    public static string BuildSkuName(string productName, string? variantName)
        => string.IsNullOrWhiteSpace(variantName) ? productName : $"{productName} · {variantName}";

    public static decimal ResolveBasePrice(PriceListBaseMode baseMode, Product product, ProductVariant? variant)
        => baseMode switch
        {
            PriceListBaseMode.Cost => variant?.Cost ?? product.Cost,
            PriceListBaseMode.Manual => 0m,
            _ => variant?.SalePrice ?? product.SalePrice
        };

    public static decimal ApplyAdjustment(BulkPriceAdjustmentType type, decimal basePrice, decimal value)
        => type switch
        {
            BulkPriceAdjustmentType.FixedAmount => Math.Max(0, basePrice + value),
            _ => Math.Max(0, Math.Round(basePrice * (1 + (value / 100m)), 2, MidpointRounding.AwayFromZero))
        };

    public static async Task<ProductWarehouseStock> GetOrCreateStockAsync(IAppDbContext db, int accountId, int productId, int? productVariantId, int warehouseId, ICurrentUser current, CancellationToken ct)
    {
        var stock = await db.ProductWarehouseStocks.FirstOrDefaultAsync(x =>
            x.AccountId == accountId &&
            x.ProductId == productId &&
            x.ProductVariantId == productVariantId &&
            x.WarehouseId == warehouseId, ct);

        if (stock is not null)
            return stock;

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

    public static async Task<ProductImportParseResult> ParseImportAsync(IAppDbContext db, int accountId, string csvContent, bool upsertExisting, CancellationToken ct)
    {
        var rows = ParseCsv(csvContent);
        var resultRows = new List<ProductImportPreviewRowDto>();
        var parsedRows = new List<ProductImportRow>();
        var categories = await db.ProductCategories.AsNoTracking().Where(x => x.AccountId == accountId).ToDictionaryAsync(x => x.Name.ToLower(), x => x.Id, ct);
        var products = await db.Products.AsNoTracking().Where(x => x.AccountId == accountId).ToListAsync(ct);
        var variants = await db.ProductVariants.AsNoTracking().Where(x => x.AccountId == accountId).ToListAsync(ct);
        var productCodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var variantCodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.InternalCode) || string.IsNullOrWhiteSpace(row.Name))
            {
                resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, false, false, false, false, "Cada fila debe incluir Name e InternalCode."));
                continue;
            }

            if (!categories.TryGetValue((row.CategoryName ?? string.Empty).Trim().ToLower(), out var categoryId))
            {
                resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, false, false, false, false, $"La categoría '{row.CategoryName}' no existe."));
                continue;
            }

            if (!Enum.TryParse<UnitOfMeasure>(row.UnitOfMeasure, true, out var unit))
            {
                resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, false, false, false, false, $"La unidad '{row.UnitOfMeasure}' no es válida."));
                continue;
            }

            if (!decimal.TryParse(row.Cost, out var cost) || !decimal.TryParse(row.SalePrice, out var salePrice) || !decimal.TryParse(row.MinimumStock, out var minimumStock))
            {
                resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, false, false, false, false, "Costo, precio o stock mínimo inválidos."));
                continue;
            }

            var normalizedProductCode = row.InternalCode.Trim();
            if (!productCodesInFile.Add(normalizedProductCode))
            {
                resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, false, false, false, false, "Código de producto duplicado dentro del archivo."));
                continue;
            }

            var existingProduct = products.FirstOrDefault(x => x.InternalCode == normalizedProductCode);
            var willCreateProduct = existingProduct is null;
            var willUpdateProduct = existingProduct is not null && upsertExisting;
            if (existingProduct is not null && !upsertExisting)
            {
                resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, false, false, false, false, "El producto ya existe y la opción de actualizar está desactivada."));
                continue;
            }

            var hasVariant = !string.IsNullOrWhiteSpace(row.VariantInternalCode) || !string.IsNullOrWhiteSpace(row.VariantName);
            var willCreateVariant = false;
            var willUpdateVariant = false;
            ProductVariant? existingVariant = null;
            if (hasVariant)
            {
                if (string.IsNullOrWhiteSpace(row.VariantInternalCode) || string.IsNullOrWhiteSpace(row.VariantName) || string.IsNullOrWhiteSpace(row.VariantAttributes))
                {
                    resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, willCreateProduct, willUpdateProduct, false, false, "Si se informa variante, también se requieren VariantName, VariantInternalCode y VariantAttributes."));
                    continue;
                }

                var variantCode = row.VariantInternalCode.Trim();
                if (!variantCodesInFile.Add(variantCode))
                {
                    resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, willCreateProduct, willUpdateProduct, false, false, "Código de variante duplicado dentro del archivo."));
                    continue;
                }

                existingVariant = variants.FirstOrDefault(x => x.InternalCode == variantCode);
                willCreateVariant = existingVariant is null;
                willUpdateVariant = existingVariant is not null && upsertExisting;
                if (existingVariant is not null && !upsertExisting)
                {
                    resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, false, willCreateProduct, willUpdateProduct, false, false, "La variante ya existe y la opción de actualizar está desactivada."));
                    continue;
                }
            }

            parsedRows.Add(new ProductImportRow(row.RowNumber, row.Name.Trim(), normalizedProductCode, string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode.Trim(), row.Description.Trim(), categoryId, row.Brand.Trim(), unit, cost, salePrice, minimumStock, ParseBool(row.IsActive), row.VariantName?.Trim(), row.VariantInternalCode?.Trim(), string.IsNullOrWhiteSpace(row.VariantBarcode) ? null : row.VariantBarcode.Trim(), row.VariantAttributes?.Trim(), ParseNullableDecimal(row.VariantCost) ?? cost, ParseNullableDecimal(row.VariantSalePrice) ?? salePrice, existingProduct, existingVariant));
            resultRows.Add(new ProductImportPreviewRowDto(row.RowNumber, row.InternalCode, row.Name, true, willCreateProduct, willUpdateProduct, willCreateVariant, willUpdateVariant, hasVariant ? "Producto y variante listos para procesar." : "Producto listo para procesar."));
        }

        return new ProductImportParseResult(parsedRows, new ProductImportPreviewDto(resultRows.Count, resultRows.Count(x => x.IsValid), resultRows.Count(x => !x.IsValid), resultRows));
    }

    private static bool ParseBool(string? raw)
        => string.IsNullOrWhiteSpace(raw) || bool.TryParse(raw, out var value) && value;

    private static decimal? ParseNullableDecimal(string? raw)
        => decimal.TryParse(raw, out var value) ? value : null;

    private static List<ProductImportCsvRow> ParseCsv(string csvContent)
    {
        var rows = new List<ProductImportCsvRow>();
        if (string.IsNullOrWhiteSpace(csvContent))
            return rows;

        var lines = csvContent.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
            return rows;

        var headers = SplitCsvLine(lines[0]);
        var map = headers
            .Select((value, index) => new { Key = value.Trim(), Index = index })
            .ToDictionary(x => x.Key, x => x.Index, StringComparer.OrdinalIgnoreCase);

        string Get(List<string> fields, string key)
            => map.TryGetValue(key, out var index) && index < fields.Count ? fields[index] : string.Empty;

        for (var i = 1; i < lines.Length; i++)
        {
            var fields = SplitCsvLine(lines[i]);
            rows.Add(new ProductImportCsvRow(
                i + 1,
                Get(fields, "Name"),
                Get(fields, "InternalCode"),
                Get(fields, "Barcode"),
                Get(fields, "Description"),
                Get(fields, "CategoryName"),
                Get(fields, "Brand"),
                Get(fields, "UnitOfMeasure"),
                Get(fields, "Cost"),
                Get(fields, "SalePrice"),
                Get(fields, "MinimumStock"),
                Get(fields, "IsActive"),
                Get(fields, "VariantName"),
                Get(fields, "VariantInternalCode"),
                Get(fields, "VariantBarcode"),
                Get(fields, "VariantAttributes"),
                Get(fields, "VariantCost"),
                Get(fields, "VariantSalePrice")));
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    public sealed record ProductImportParseResult(IReadOnlyList<ProductImportRow> Rows, ProductImportPreviewDto Preview);
    public sealed record ProductImportCsvRow(int RowNumber, string Name, string InternalCode, string Barcode, string Description, string CategoryName, string Brand, string UnitOfMeasure, string Cost, string SalePrice, string MinimumStock, string IsActive, string VariantName, string VariantInternalCode, string VariantBarcode, string VariantAttributes, string VariantCost, string VariantSalePrice);
    public sealed record ProductImportRow(int RowNumber, string Name, string InternalCode, string? Barcode, string Description, int CategoryId, string Brand, UnitOfMeasure UnitOfMeasure, decimal Cost, decimal SalePrice, decimal MinimumStock, bool IsActive, string? VariantName, string? VariantInternalCode, string? VariantBarcode, string? VariantAttributes, decimal VariantCost, decimal VariantSalePrice, Product? ExistingProduct, ProductVariant? ExistingVariant);
}

public sealed class GetInventoryOverviewQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetInventoryOverviewQuery, AppResult<InventoryOverviewDto>>
{
    public async Task<AppResult<InventoryOverviewDto>> Handle(GetInventoryOverviewQuery request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<InventoryOverviewDto>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var stockQuery = db.ProductWarehouseStocks.AsNoTracking()
            .Where(x => x.AccountId == accountId);

        if (request.WarehouseId.HasValue)
            stockQuery = stockQuery.Where(x => x.WarehouseId == request.WarehouseId.Value);
        if (request.ProductId.HasValue)
            stockQuery = stockQuery.Where(x => x.ProductId == request.ProductId.Value);
        if (request.ProductVariantId.HasValue)
            stockQuery = stockQuery.Where(x => x.ProductVariantId == request.ProductVariantId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            stockQuery = stockQuery.Where(x =>
                x.Product.Name.Contains(search) ||
                x.Product.InternalCode.Contains(search) ||
                (x.ProductVariant != null && (x.ProductVariant.Name.Contains(search) || x.ProductVariant.InternalCode.Contains(search))));
        }

        var items = await stockQuery
            .OrderBy(x => x.Product.Name)
            .ThenBy(x => x.ProductVariantId.HasValue ? x.ProductVariant!.Name : x.Product.Name)
            .ThenBy(x => x.Warehouse.Name)
            .Select(x => new
            {
                x.ProductId,
                x.ProductVariantId,
                ProductName = x.Product.Name,
                VariantName = x.ProductVariant != null ? x.ProductVariant.Name : null,
                InternalCode = x.ProductVariant != null ? x.ProductVariant.InternalCode : x.Product.InternalCode,
                WarehouseName = x.Warehouse.Name,
                x.WarehouseId,
                x.QuantityOnHand,
                x.LastMovementAtUtc,
                x.Product.MinimumStock,
                TotalQuantity = db.ProductWarehouseStocks.Where(s => s.AccountId == accountId && s.ProductId == x.ProductId && s.ProductVariantId == x.ProductVariantId).Sum(s => (decimal?)s.QuantityOnHand) ?? 0m
            })
            .ToListAsync(ct);

        var dtoItems = items
            .Select(x => new InventoryStockItemDto(
                x.ProductId,
                x.ProductVariantId,
                x.ProductName,
                CommerceRelease2Helpers.BuildSkuName(x.ProductName, x.VariantName),
                x.InternalCode,
                x.WarehouseName,
                x.WarehouseId,
                x.QuantityOnHand,
                x.TotalQuantity,
                x.MinimumStock,
                x.TotalQuantity < x.MinimumStock,
                x.LastMovementAtUtc))
            .ToList();

        var overview = new InventoryOverviewDto(
            dtoItems,
            dtoItems.Sum(x => x.QuantityOnHand),
            dtoItems.Select(x => x.WarehouseId).Distinct().Count(),
            dtoItems.Count(x => x.IsLowStock),
            dtoItems.Select(x => (x.ProductId, x.ProductVariantId)).Distinct().Count());

        return AppResult<InventoryOverviewDto>.Ok(overview);
    }
}

public sealed class GetInventorySeedDataQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetInventorySeedDataQuery, AppResult<InventorySeedDataDto>>
{
    public async Task<AppResult<InventorySeedDataDto>> Handle(GetInventorySeedDataQuery request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<InventorySeedDataDto>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        var categories = await db.ProductCategories.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        var products = await db.Products.AsNoTracking().Where(x => x.AccountId == accountId).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.InternalCode, HasVariants = x.Variants.Any() }).ToListAsync(ct);
        var variants = await db.ProductVariants.AsNoTracking().Where(x => x.AccountId == accountId).OrderBy(x => x.Product.Name).ThenBy(x => x.Name).Select(x => new { x.ProductId, x.Id, ProductName = x.Product.Name, x.Name, x.InternalCode }).ToListAsync(ct);

        var skus = new List<InventorySkuLookupDto>();
        skus.AddRange(products.Select(x => new InventorySkuLookupDto(x.Id, null, x.Name, x.InternalCode, x.HasVariants)));
        skus.AddRange(variants.Select(x => new InventorySkuLookupDto(x.ProductId, x.Id, $"{x.ProductName} · {x.Name}", x.InternalCode, true)));
        return AppResult<InventorySeedDataDto>.Ok(new InventorySeedDataDto(warehouses, skus, categories));
    }
}

public sealed class GetStockMovementsQueryValidator : AbstractValidator<GetStockMovementsQuery>
{
    public GetStockMovementsQueryValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}

public sealed class GetStockMovementsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetStockMovementsQuery, AppResult<List<StockMovementListItemDto>>>
{
    public async Task<AppResult<List<StockMovementListItemDto>>> Handle(GetStockMovementsQuery request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<List<StockMovementListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var query = db.StockMovements.AsNoTracking().Where(x => x.AccountId == accountId);
        if (request.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == request.WarehouseId.Value);
        if (request.ProductId.HasValue) query = query.Where(x => x.ProductId == request.ProductId.Value);
        if (request.ProductVariantId.HasValue) query = query.Where(x => x.ProductVariantId == request.ProductVariantId.Value);
        if (request.MovementType.HasValue) query = query.Where(x => x.MovementType == request.MovementType.Value);

        var rawItems = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(request.Take)
            .Select(x => new
            {
                x.Id,
                x.ProductId,
                x.ProductVariantId,
                ProductName = x.Product.Name,
                VariantName = x.ProductVariant != null ? x.ProductVariant.Name : null,
                InternalCode = x.ProductVariant != null ? x.ProductVariant.InternalCode : x.Product.InternalCode,
                WarehouseName = x.Warehouse.Name,
                x.WarehouseId,
                CounterpartWarehouseName = x.CounterpartWarehouse != null ? x.CounterpartWarehouse.Name : null,
                x.MovementType,
                x.QuantityDelta,
                x.Reason,
                x.Note,
                x.CreatedByUserId,
                x.OccurredAtUtc
            })
            .ToListAsync(ct);

        var items = rawItems
            .Select(x => new StockMovementListItemDto(
                x.Id,
                x.ProductId,
                x.ProductVariantId,
                CommerceRelease2Helpers.BuildSkuName(x.ProductName, x.VariantName),
                x.InternalCode,
                x.WarehouseName,
                x.WarehouseId,
                x.CounterpartWarehouseName,
                x.MovementType,
                x.QuantityDelta,
                x.Reason,
                x.Note,
                x.CreatedByUserId,
                x.OccurredAtUtc))
            .ToList();

        return AppResult<List<StockMovementListItemDto>>.Ok(items);
    }
}

public sealed class RecordStockMovementCommandValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        When(x => x.MovementType == StockMovementType.TransferOut, () =>
        {
            RuleFor(x => x.CounterpartWarehouseId).NotNull().GreaterThan(0);
        });
    }
}

public sealed class RecordStockMovementCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<RecordStockMovementCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(RecordStockMovementCommand request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var product = await db.Products.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.ProductId, ct);
        if (product is null) return AppResult<int>.Fail("not_found", "Producto no encontrado.");

        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = await db.ProductVariants.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.ProductVariantId.Value && x.ProductId == request.ProductId, ct);
            if (variant is null) return AppResult<int>.Fail("variant_not_found", "La variante seleccionada no pertenece al producto.");
        }

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.WarehouseId, ct);
        if (warehouse is null) return AppResult<int>.Fail("warehouse_not_found", "Depósito no encontrado.");

        Warehouse? counterpartWarehouse = null;
        if (request.CounterpartWarehouseId.HasValue)
        {
            counterpartWarehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.CounterpartWarehouseId.Value, ct);
            if (counterpartWarehouse is null) return AppResult<int>.Fail("warehouse_not_found", "El depósito destino no existe.");
            if (counterpartWarehouse.Id == warehouse.Id) return AppResult<int>.Fail("invalid_transfer", "El depósito origen y destino no pueden ser el mismo.");
        }

        await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var movementId = 0;
            var referenceGroup = request.MovementType == StockMovementType.TransferOut ? Guid.NewGuid().ToString("N") : null;

            var sourceStock = await CommerceRelease2Helpers.GetOrCreateStockAsync(db, accountId, request.ProductId, request.ProductVariantId, request.WarehouseId, current, ct);
            var occurredAt = request.OccurredAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            var delta = request.MovementType switch
            {
                StockMovementType.Outbound or StockMovementType.TransferOut => -request.Quantity,
                _ => request.Quantity
            };

            if (request.MovementType is StockMovementType.Outbound or StockMovementType.TransferOut && sourceStock.QuantityOnHand + delta < 0)
                return AppResult<int>.Fail("insufficient_stock", "No hay stock suficiente para registrar el egreso.");

            sourceStock.QuantityOnHand += delta;
            sourceStock.LastMovementAtUtc = occurredAt;
            CommerceFeatureHelpers.TouchUpdate(sourceStock, current);

            var movement = new StockMovement
            {
                AccountId = accountId,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                WarehouseId = request.WarehouseId,
                CounterpartWarehouseId = counterpartWarehouse?.Id,
                MovementType = request.MovementType,
                QuantityDelta = delta,
                Reason = request.Reason.Trim(),
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                OccurredAtUtc = occurredAt,
                ReferenceGroup = referenceGroup
            };
            CommerceFeatureHelpers.TouchCreate(movement, current);
            db.StockMovements.Add(movement);

            if (request.MovementType == StockMovementType.TransferOut)
            {
                var targetStock = await CommerceRelease2Helpers.GetOrCreateStockAsync(db, accountId, request.ProductId, request.ProductVariantId, counterpartWarehouse!.Id, current, ct);
                targetStock.QuantityOnHand += request.Quantity;
                targetStock.LastMovementAtUtc = occurredAt;
                CommerceFeatureHelpers.TouchUpdate(targetStock, current);

                var inbound = new StockMovement
                {
                    AccountId = accountId,
                    ProductId = request.ProductId,
                    ProductVariantId = request.ProductVariantId,
                    WarehouseId = counterpartWarehouse.Id,
                    CounterpartWarehouseId = request.WarehouseId,
                    MovementType = StockMovementType.TransferIn,
                    QuantityDelta = request.Quantity,
                    Reason = request.Reason.Trim(),
                    Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                    OccurredAtUtc = occurredAt,
                    ReferenceGroup = referenceGroup
                };
                CommerceFeatureHelpers.TouchCreate(inbound, current);
                db.StockMovements.Add(inbound);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            movementId = movement.Id;

            await audit.WriteAsync(accountId, null, "StockMovement", movementId, "created", $"Movimiento {request.MovementType}: {CommerceRelease2Helpers.BuildSkuName(product.Name, variant?.Name)}", ct);
            return AppResult<int>.Ok(movementId);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}

public sealed class GetPriceListSeedDataQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetPriceListSeedDataQuery, AppResult<PriceListSeedDataDto>>
{
    public async Task<AppResult<PriceListSeedDataDto>> Handle(GetPriceListSeedDataQuery request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<PriceListSeedDataDto>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var categories = await db.ProductCategories.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        var products = await db.Products.AsNoTracking().Where(x => x.AccountId == accountId).OrderBy(x => x.Name).Select(x => new InventorySkuLookupDto(x.Id, null, x.Name, x.InternalCode, x.Variants.Any())).ToListAsync(ct);
        var variants = await db.ProductVariants.AsNoTracking().Where(x => x.AccountId == accountId).OrderBy(x => x.Product.Name).ThenBy(x => x.Name).Select(x => new InventorySkuLookupDto(x.ProductId, x.Id, x.Product.Name + " · " + x.Name, x.InternalCode, true)).ToListAsync(ct);
        return AppResult<PriceListSeedDataDto>.Ok(new PriceListSeedDataDto(categories, products, variants));
    }
}

public sealed class GetPriceListsQueryValidator : AbstractValidator<GetPriceListsQuery>
{
    public GetPriceListsQueryValidator() => CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
}

public sealed class GetPriceListsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetPriceListsQuery, AppResult<PagedResult<PriceListListItemDto>>>
{
    public async Task<AppResult<PagedResult<PriceListListItemDto>>> Handle(GetPriceListsQuery request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<PagedResult<PriceListListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var query = db.PriceLists.AsNoTracking().Where(x => x.AccountId == accountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search));
        }
        if (request.IsActive.HasValue) query = query.Where(x => x.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, CommerceFeatureHelpers.MaxPageSize);
        var items = await query.OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PriceListListItemDto(x.Id, x.Name, x.BaseMode, x.TargetType, x.IsActive, x.Items.Count(i => i.IsActive), x.CreatedAtUtc, x.ModifiedAtUtc))
            .ToListAsync(ct);

        return AppResult<PagedResult<PriceListListItemDto>>.Ok(new PagedResult<PriceListListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetPriceListByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetPriceListByIdQuery, AppResult<PriceListDetailDto>>
{
    public async Task<AppResult<PriceListDetailDto>> Handle(GetPriceListByIdQuery request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<PriceListDetailDto>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var item = await db.PriceLists.AsNoTracking().Where(x => x.AccountId == accountId && x.Id == request.Id)
            .Select(x => new PriceListDetailDto(x.Id, x.Name, x.BaseMode, x.TargetType, x.IsActive, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);

        return item is null ? AppResult<PriceListDetailDto>.Fail("not_found", "Lista de precios no encontrada.") : AppResult<PriceListDetailDto>.Ok(item);
    }
}

public sealed class GetPriceListItemsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetPriceListItemsQuery, AppResult<List<PriceListItemDto>>>
{
    public async Task<AppResult<List<PriceListItemDto>>> Handle(GetPriceListItemsQuery request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<List<PriceListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var priceList = await db.PriceLists.AsNoTracking().FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.PriceListId, ct);
        if (priceList is null) return AppResult<List<PriceListItemDto>>.Fail("not_found", "Lista no encontrada.");

        var query = db.PriceListItems.AsNoTracking().Where(x => x.AccountId == accountId && x.PriceListId == request.PriceListId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Product.Name.Contains(search) || x.Product.InternalCode.Contains(search) || (x.ProductVariant != null && (x.ProductVariant.Name.Contains(search) || x.ProductVariant.InternalCode.Contains(search))));
        }

        var rawItems = await query.OrderBy(x => x.Product.Name).ThenBy(x => x.ProductVariantId.HasValue ? x.ProductVariant!.Name : x.Product.Name)
            .Select(x => new
            {
                x.Id,
                x.PriceListId,
                x.ProductId,
                x.ProductVariantId,
                ProductName = x.Product.Name,
                VariantName = x.ProductVariant != null ? x.ProductVariant.Name : null,
                InternalCode = x.ProductVariant != null ? x.ProductVariant.InternalCode : x.Product.InternalCode,
                ProductSalePrice = x.Product.SalePrice,
                ProductCost = x.Product.Cost,
                VariantSalePrice = x.ProductVariant != null ? x.ProductVariant.SalePrice : (decimal?)null,
                VariantCost = x.ProductVariant != null ? x.ProductVariant.Cost : (decimal?)null,
                x.Price,
                x.IsActive,
                x.CreatedAtUtc,
                x.ModifiedAtUtc
            })
            .ToListAsync(ct);

        var items = rawItems
            .Select(x => new PriceListItemDto(
                x.Id,
                x.PriceListId,
                x.ProductId,
                x.ProductVariantId,
                CommerceRelease2Helpers.BuildSkuName(x.ProductName, x.VariantName),
                x.InternalCode,
                priceList.BaseMode switch
                {
                    PriceListBaseMode.Cost => x.VariantCost ?? x.ProductCost,
                    PriceListBaseMode.Manual => 0m,
                    _ => x.VariantSalePrice ?? x.ProductSalePrice
                },
                x.Price,
                x.IsActive,
                x.CreatedAtUtc,
                x.ModifiedAtUtc))
            .ToList();

        return AppResult<List<PriceListItemDto>>.Ok(items);
    }
}

public sealed class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class UpdatePriceListCommandValidator : AbstractValidator<UpdatePriceListCommand>
{
    public UpdatePriceListCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class CreatePriceListCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreatePriceListCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreatePriceListCommand request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        if (await db.PriceLists.AnyAsync(x => x.AccountId == accountId && x.Name == request.Name.Trim(), ct))
            return AppResult<int>.Fail("duplicate_name", "Ya existe una lista con ese nombre.");

        var entity = new PriceList
        {
            AccountId = accountId,
            Name = request.Name.Trim(),
            BaseMode = request.BaseMode,
            TargetType = request.TargetType,
            IsActive = request.IsActive
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        db.PriceLists.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "PriceList", entity.Id, "created", $"Lista creada: {entity.Name}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdatePriceListCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdatePriceListCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdatePriceListCommand request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var entity = await db.PriceLists.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.Id, ct);
        if (entity is null) return AppResult.Fail("not_found", "Lista de precios no encontrada.");
        if (await db.PriceLists.AnyAsync(x => x.AccountId == accountId && x.Name == request.Name.Trim() && x.Id != request.Id, ct))
            return AppResult.Fail("duplicate_name", "Ya existe otra lista con ese nombre.");

        entity.Name = request.Name.Trim();
        entity.BaseMode = request.BaseMode;
        entity.TargetType = request.TargetType;
        entity.IsActive = request.IsActive;
        CommerceFeatureHelpers.TouchUpdate(entity, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "PriceList", entity.Id, "updated", $"Lista actualizada: {entity.Name}", ct);
        return AppResult.Ok();
    }
}

public sealed class SetPriceListItemCommandValidator : AbstractValidator<SetPriceListItemCommand>
{
    public SetPriceListItemCommandValidator()
    {
        RuleFor(x => x.PriceListId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public sealed class SetPriceListItemCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<SetPriceListItemCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(SetPriceListItemCommand request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;

        var priceListResult = await CommerceRelease2Helpers.GetPriceListAsync(db, accountId, request.PriceListId, ct);
        if (!priceListResult.Success) return AppResult<int>.Fail(priceListResult.ErrorCode, priceListResult.Message);
        var priceList = priceListResult.PriceList;

        var product = await db.Products.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.ProductId, ct);
        if (product is null) return AppResult<int>.Fail("product_not_found", "Producto no encontrado.");

        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = await db.ProductVariants.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Id == request.ProductVariantId.Value && x.ProductId == request.ProductId, ct);
            if (variant is null) return AppResult<int>.Fail("variant_not_found", "La variante no corresponde al producto.");
        }

        if (priceList.TargetType == PriceListTargetType.Product && request.ProductVariantId.HasValue)
            return AppResult<int>.Fail("invalid_target", "La lista está configurada a nivel producto.");
        if (priceList.TargetType == PriceListTargetType.Variant && !request.ProductVariantId.HasValue)
            return AppResult<int>.Fail("invalid_target", "La lista está configurada a nivel variante.");

        var entity = await db.PriceListItems.FirstOrDefaultAsync(x => x.AccountId == accountId && x.PriceListId == request.PriceListId && x.ProductId == request.ProductId && x.ProductVariantId == request.ProductVariantId, ct);
        var isNew = entity is null;
        if (entity is null)
        {
            entity = new PriceListItem
            {
                AccountId = accountId,
                PriceListId = request.PriceListId,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                Price = request.Price,
                IsActive = request.IsActive
            };
            CommerceFeatureHelpers.TouchCreate(entity, current);
            db.PriceListItems.Add(entity);
        }
        else
        {
            entity.Price = request.Price;
            entity.IsActive = request.IsActive;
            CommerceFeatureHelpers.TouchUpdate(entity, current);
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "PriceListItem", entity.Id, isNew ? "created" : "updated", $"Precio actualizado para {CommerceRelease2Helpers.BuildSkuName(product.Name, variant?.Name)}", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class ApplyPriceListAdjustmentCommandValidator : AbstractValidator<ApplyPriceListAdjustmentCommand>
{
    public ApplyPriceListAdjustmentCommandValidator()
    {
        RuleFor(x => x.PriceListId).GreaterThan(0);
        RuleFor(x => x.Value).NotEqual(0m);
    }
}

public sealed class ApplyPriceListAdjustmentCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ApplyPriceListAdjustmentCommand, AppResult<BulkPriceUpdateResultDto>>
{
    public async Task<AppResult<BulkPriceUpdateResultDto>> Handle(ApplyPriceListAdjustmentCommand request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<BulkPriceUpdateResultDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var priceListResult = await CommerceRelease2Helpers.GetPriceListAsync(db, accountId, request.PriceListId, ct);
        if (!priceListResult.Success) return AppResult<BulkPriceUpdateResultDto>.Fail(priceListResult.ErrorCode, priceListResult.Message);
        var priceList = priceListResult.PriceList;

        var productsQuery = db.Products.Where(x => x.AccountId == accountId);
        if (!request.IncludeInactiveProducts) productsQuery = productsQuery.Where(x => x.IsActive);
        if (request.CategoryId.HasValue) productsQuery = productsQuery.Where(x => x.CategoryId == request.CategoryId.Value);

        var products = await productsQuery.Include(x => x.Variants).ToListAsync(ct);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var product in products)
        {
            if (priceList.TargetType == PriceListTargetType.Product)
            {
                var basePrice = CommerceRelease2Helpers.ResolveBasePrice(priceList.BaseMode, product, null);
                if (priceList.BaseMode == PriceListBaseMode.Manual && basePrice == 0m)
                {
                    skipped++;
                    continue;
                }

                var entity = await db.PriceListItems.FirstOrDefaultAsync(x => x.AccountId == accountId && x.PriceListId == priceList.Id && x.ProductId == product.Id && x.ProductVariantId == null, ct);
                var newPrice = CommerceRelease2Helpers.ApplyAdjustment(request.AdjustmentType, basePrice, request.Value);
                if (entity is null)
                {
                    entity = new PriceListItem { AccountId = accountId, PriceListId = priceList.Id, ProductId = product.Id, Price = newPrice, IsActive = true };
                    CommerceFeatureHelpers.TouchCreate(entity, current);
                    db.PriceListItems.Add(entity);
                    created++;
                }
                else
                {
                    entity.Price = newPrice;
                    entity.IsActive = true;
                    CommerceFeatureHelpers.TouchUpdate(entity, current);
                    updated++;
                }
            }
            else
            {
                foreach (var variant in product.Variants.Where(x => request.IncludeInactiveProducts || x.IsActive))
                {
                    var basePrice = CommerceRelease2Helpers.ResolveBasePrice(priceList.BaseMode, product, variant);
                    if (priceList.BaseMode == PriceListBaseMode.Manual && basePrice == 0m)
                    {
                        skipped++;
                        continue;
                    }

                    var entity = await db.PriceListItems.FirstOrDefaultAsync(x => x.AccountId == accountId && x.PriceListId == priceList.Id && x.ProductVariantId == variant.Id, ct);
                    var newPrice = CommerceRelease2Helpers.ApplyAdjustment(request.AdjustmentType, basePrice, request.Value);
                    if (entity is null)
                    {
                        entity = new PriceListItem { AccountId = accountId, PriceListId = priceList.Id, ProductId = product.Id, ProductVariantId = variant.Id, Price = newPrice, IsActive = true };
                        CommerceFeatureHelpers.TouchCreate(entity, current);
                        db.PriceListItems.Add(entity);
                        created++;
                    }
                    else
                    {
                        entity.Price = newPrice;
                        entity.IsActive = true;
                        CommerceFeatureHelpers.TouchUpdate(entity, current);
                        updated++;
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "PriceList", priceList.Id, "bulk_updated", $"Actualización masiva sobre lista {priceList.Name}", ct);
        var summary = new BulkPriceUpdateResultDto(updated, created, skipped, $"Se actualizaron {updated} ítems, se crearon {created} y se omitieron {skipped}.");
        return AppResult<BulkPriceUpdateResultDto>.Ok(summary);
    }
}

public sealed class PreviewProductImportCommandHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<PreviewProductImportCommand, AppResult<ProductImportPreviewDto>>
{
    public async Task<AppResult<ProductImportPreviewDto>> Handle(PreviewProductImportCommand request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<ProductImportPreviewDto>.Fail(scope.ErrorCode, scope.Message);
        var parsed = await CommerceRelease2Helpers.ParseImportAsync(db, scope.AccountId, request.CsvContent, request.UpsertExisting, ct);
        return AppResult<ProductImportPreviewDto>.Ok(parsed.Preview);
    }
}

public sealed class ApplyProductImportCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ApplyProductImportCommand, AppResult<ProductImportResultDto>>
{
    public async Task<AppResult<ProductImportResultDto>> Handle(ApplyProductImportCommand request, CancellationToken ct)
    {
        var scope = await CommerceRelease2Helpers.RequireProductsAsync(access, ct);
        if (!scope.Success) return AppResult<ProductImportResultDto>.Fail(scope.ErrorCode, scope.Message);
        var accountId = scope.AccountId;
        var parsed = await CommerceRelease2Helpers.ParseImportAsync(db, accountId, request.CsvContent, request.UpsertExisting, ct);

        var createdProducts = 0;
        var updatedProducts = 0;
        var createdVariants = 0;
        var updatedVariants = 0;
        var messages = new List<string>();

        foreach (var row in parsed.Rows)
        {
            Product product;
            if (row.ExistingProduct is null)
            {
                product = new Product
                {
                    AccountId = accountId,
                    Name = row.Name,
                    InternalCode = row.InternalCode,
                    Barcode = row.Barcode,
                    Description = row.Description,
                    CategoryId = row.CategoryId,
                    Brand = row.Brand,
                    UnitOfMeasure = row.UnitOfMeasure,
                    Cost = row.Cost,
                    SalePrice = row.SalePrice,
                    MinimumStock = row.MinimumStock,
                    IsActive = row.IsActive
                };
                CommerceFeatureHelpers.TouchCreate(product, current);
                db.Products.Add(product);
                createdProducts++;
            }
            else
            {
                product = await db.Products.FirstAsync(x => x.Id == row.ExistingProduct.Id, ct);
                product.Name = row.Name;
                product.Barcode = row.Barcode;
                product.Description = row.Description;
                product.CategoryId = row.CategoryId;
                product.Brand = row.Brand;
                product.UnitOfMeasure = row.UnitOfMeasure;
                product.Cost = row.Cost;
                product.SalePrice = row.SalePrice;
                product.MinimumStock = row.MinimumStock;
                product.IsActive = row.IsActive;
                CommerceFeatureHelpers.TouchUpdate(product, current);
                updatedProducts++;
            }

            await db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(row.VariantInternalCode) && !string.IsNullOrWhiteSpace(row.VariantName) && !string.IsNullOrWhiteSpace(row.VariantAttributes))
            {
                if (row.ExistingVariant is null)
                {
                    var variant = new ProductVariant
                    {
                        AccountId = accountId,
                        ProductId = product.Id,
                        Name = row.VariantName,
                        InternalCode = row.VariantInternalCode,
                        Barcode = row.VariantBarcode,
                        AttributesSummary = row.VariantAttributes,
                        Cost = row.VariantCost,
                        SalePrice = row.VariantSalePrice,
                        IsActive = row.IsActive
                    };
                    CommerceFeatureHelpers.TouchCreate(variant, current);
                    db.ProductVariants.Add(variant);
                    createdVariants++;
                }
                else
                {
                    var variant = await db.ProductVariants.FirstAsync(x => x.Id == row.ExistingVariant.Id, ct);
                    variant.ProductId = product.Id;
                    variant.Name = row.VariantName;
                    variant.Barcode = row.VariantBarcode;
                    variant.AttributesSummary = row.VariantAttributes;
                    variant.Cost = row.VariantCost;
                    variant.SalePrice = row.VariantSalePrice;
                    variant.IsActive = row.IsActive;
                    CommerceFeatureHelpers.TouchUpdate(variant, current);
                    updatedVariants++;
                }
            }

            messages.Add($"Fila {row.RowNumber}: {row.InternalCode} procesado correctamente.");
        }

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(accountId, null, "ProductImport", null, "imported", $"Importación masiva procesada: {parsed.Rows.Count} filas válidas", ct);

        var result = new ProductImportResultDto(parsed.Rows.Count, createdProducts, updatedProducts, createdVariants, updatedVariants, parsed.Preview.ErrorRows, messages);
        return AppResult<ProductImportResultDto>.Ok(result);
    }
}
