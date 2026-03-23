using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Common;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GestAI.Application.Commerce;

public sealed record GetCommercialDocumentSeedDataQuery : IRequest<AppResult<CommercialDocumentSeedDataDto>>;

public sealed record GetQuotesQuery(string? Search = null, QuoteStatus? Status = null, int? CustomerId = null, bool? OnlyConvertible = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<QuoteListItemDto>>>;
public sealed record GetQuoteByIdQuery(int Id) : IRequest<AppResult<QuoteDetailDto>>;
public sealed record CreateQuoteCommand(int CustomerId, QuoteStatus Status, DateTime IssuedAtUtc, DateTime? ValidUntilUtc, string? Observations, IReadOnlyList<CommercialLineInput> Items) : IRequest<AppResult<int>>;
public sealed record UpdateQuoteCommand(int Id, int CustomerId, QuoteStatus Status, DateTime IssuedAtUtc, DateTime? ValidUntilUtc, string? Observations, IReadOnlyList<CommercialLineInput> Items) : IRequest<AppResult>;
public sealed record ConvertQuoteToSaleCommand(int QuoteId, SaleStatus SaleStatus, DateTime? IssuedAtUtc, string? Observations) : IRequest<AppResult<int>>;

public sealed record GetSalesQuery(string? Search = null, SaleStatus? Status = null, int? CustomerId = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<SaleListItemDto>>>;
public sealed record GetSaleByIdQuery(int Id) : IRequest<AppResult<SaleDetailDto>>;
public sealed record CreateSaleCommand(int CustomerId, SaleStatus Status, DateTime IssuedAtUtc, string? Observations, IReadOnlyList<CommercialLineInput> Items) : IRequest<AppResult<int>>;
public sealed record UpdateSaleCommand(int Id, int CustomerId, SaleStatus Status, DateTime IssuedAtUtc, string? Observations, IReadOnlyList<CommercialLineInput> Items) : IRequest<AppResult>;
public sealed record CreateQuickSaleCommand(int CustomerId, SaleStatus Status, DateTime IssuedAtUtc, string? Observations, IReadOnlyList<QuickCommercialLineDto> Items) : IRequest<AppResult<int>>;

public sealed record CommercialLineInput(int ProductId, int? ProductVariantId, string? Description, decimal Quantity, decimal UnitPrice, int SortOrder);

internal static class CommercialCircuitHelpers
{
    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireQuotesAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Quotes, ct);

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireSalesAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Sales, ct);

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireSalesAndQuotesAsync(IUserAccessService access, CancellationToken ct)
    {
        var sales = await RequireSalesAsync(access, ct);
        if (!sales.Success) return sales;
        var quotes = await RequireQuotesAsync(access, ct);
        if (!quotes.Success) return quotes;
        return sales;
    }

    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static bool IsQuoteConvertible(Quote quote, DateTime utcNow)
    {
        if (quote.Status is QuoteStatus.Rejected or QuoteStatus.Expired or QuoteStatus.Converted)
            return false;

        return !quote.ValidUntilUtc.HasValue || quote.ValidUntilUtc.Value >= utcNow.Date;
    }

    public static bool IsQuoteConvertible(QuoteStatus status, DateTime? validUntilUtc, DateTime utcNow)
    {
        if (status is QuoteStatus.Rejected or QuoteStatus.Expired or QuoteStatus.Converted)
            return false;

        return !validUntilUtc.HasValue || validUntilUtc.Value >= utcNow.Date;
    }

    public static string BuildSkuLabel(string productName, string? variantName)
        => string.IsNullOrWhiteSpace(variantName) ? productName : $"{productName} · {variantName}";

    public static async Task<Dictionary<(int ProductId, int? ProductVariantId), CommercialSkuData>> LoadSkuDataAsync(IAppDbContext db, int accountId, CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new CommercialSkuData(x.Id, null, x.Name, null, x.InternalCode, x.SalePrice, x.IsActive))
            .ToListAsync(ct);

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new CommercialSkuData(x.ProductId, x.Id, x.Product.Name, x.Name, x.InternalCode, x.SalePrice, x.IsActive && x.Product.IsActive))
            .ToListAsync(ct);

        return products.Concat(variants).ToDictionary(x => (x.ProductId, x.ProductVariantId));
    }

    public static List<CommercialPreparedLine> PrepareLines(IReadOnlyList<CommercialLineInput> items, Dictionary<(int ProductId, int? ProductVariantId), CommercialSkuData> skuData)
    {
        var prepared = new List<CommercialPreparedLine>();

        foreach (var item in items.OrderBy(x => x.SortOrder).ThenBy(x => x.ProductId))
        {
            if (!skuData.TryGetValue((item.ProductId, item.ProductVariantId), out var sku))
                throw new InvalidOperationException("sku_not_found");

            var description = string.IsNullOrWhiteSpace(item.Description)
                ? BuildSkuLabel(sku.ProductName, sku.VariantName)
                : item.Description.Trim();
            var internalCode = sku.InternalCode;
            var quantity = item.Quantity;
            var unitPrice = RoundMoney(item.UnitPrice);
            var lineSubtotal = RoundMoney(quantity * unitPrice);
            prepared.Add(new CommercialPreparedLine(item.ProductId, item.ProductVariantId, description, internalCode, quantity, unitPrice, lineSubtotal, item.SortOrder));
        }

        return prepared;
    }

    public static void ApplyQuoteLines(Quote quote, IReadOnlyList<CommercialPreparedLine> lines, ICurrentUser current)
    {
        quote.Items.Clear();
        foreach (var line in lines.OrderBy(x => x.SortOrder))
        {
            quote.Items.Add(new QuoteItem
            {
                AccountId = quote.AccountId,
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                Description = line.Description,
                InternalCode = line.InternalCode,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineSubtotal = line.LineSubtotal,
                SortOrder = line.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            });
        }

        quote.Subtotal = RoundMoney(lines.Sum(x => x.LineSubtotal));
        quote.Total = quote.Subtotal;
    }

    public static void ApplySaleLines(Sale sale, IReadOnlyList<CommercialPreparedLine> lines, ICurrentUser current)
    {
        sale.Items.Clear();
        foreach (var line in lines.OrderBy(x => x.SortOrder))
        {
            sale.Items.Add(new SaleItem
            {
                AccountId = sale.AccountId,
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                Description = line.Description,
                InternalCode = line.InternalCode,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineSubtotal = line.LineSubtotal,
                SortOrder = line.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            });
        }

        sale.Subtotal = RoundMoney(lines.Sum(x => x.LineSubtotal));
        sale.Total = sale.Subtotal;
    }

    public static List<CommercialLineDto> MapQuoteLines(Quote quote)
        => quote.Items.OrderBy(x => x.SortOrder).Select(x => new CommercialLineDto(x.ProductId, x.ProductVariantId, x.Description, x.InternalCode, x.Quantity, x.UnitPrice, x.LineSubtotal, x.SortOrder)).ToList();

    public static List<CommercialLineDto> MapSaleLines(Sale sale)
        => sale.Items.OrderBy(x => x.SortOrder).Select(x => new CommercialLineDto(x.ProductId, x.ProductVariantId, x.Description, x.InternalCode, x.Quantity, x.UnitPrice, x.LineSubtotal, x.SortOrder)).ToList();

    public static string BuildQuoteNumber(int id) => $"P-{id:D6}";
    public static string BuildSaleNumber(int id) => $"V-{id:D6}";

    internal sealed record CommercialSkuData(int ProductId, int? ProductVariantId, string ProductName, string? VariantName, string InternalCode, decimal SalePrice, bool IsActive);
    internal sealed record CommercialPreparedLine(int ProductId, int? ProductVariantId, string Description, string InternalCode, decimal Quantity, decimal UnitPrice, decimal LineSubtotal, int SortOrder);
}

public sealed class GetCommercialDocumentSeedDataQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCommercialDocumentSeedDataQuery, AppResult<CommercialDocumentSeedDataDto>>
{
    public async Task<AppResult<CommercialDocumentSeedDataDto>> Handle(GetCommercialDocumentSeedDataQuery request, CancellationToken ct)
    {
        var quotes = await CommercialCircuitHelpers.RequireQuotesAsync(access, ct);
        var sales = await CommercialCircuitHelpers.RequireSalesAsync(access, ct);
        if (!quotes.Success && !sales.Success)
            return AppResult<CommercialDocumentSeedDataDto>.Fail(quotes.ErrorCode, quotes.Message);

        var accountId = quotes.Success ? quotes.AccountId : sales.AccountId;
        var customers = await db.Customers.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive).OrderBy(x => x.Name).Select(x => new LookupDto(x.Id, x.Name)).ToListAsync(ct);
        var products = await db.Products.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive).OrderBy(x => x.Name).Select(x => new CommercialSkuLookupDto(x.Id, null, x.Name, x.InternalCode, x.SalePrice, x.IsActive)).ToListAsync(ct);
        var variants = await db.ProductVariants.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive && x.Product.IsActive).OrderBy(x => x.Product.Name).ThenBy(x => x.Name).Select(x => new CommercialSkuLookupDto(x.ProductId, x.Id, x.Product.Name + " · " + x.Name, x.InternalCode, x.SalePrice, x.IsActive)).ToListAsync(ct);
        return AppResult<CommercialDocumentSeedDataDto>.Ok(new CommercialDocumentSeedDataDto(customers, products, variants));
    }
}

public sealed class GetQuotesQueryValidator : AbstractValidator<GetQuotesQuery>
{
    public GetQuotesQueryValidator() => CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
}

public sealed class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateQuoteCommandValidator : AbstractValidator<UpdateQuoteCommand>
{
    public UpdateQuoteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class GetSalesQueryValidator : AbstractValidator<GetSalesQuery>
{
    public GetSalesQueryValidator() => CommerceFeatureHelpers.AddPagingRules(this, x => x.Page, x => x.PageSize);
}

public sealed class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateSaleCommandValidator : AbstractValidator<UpdateSaleCommand>
{
    public UpdateSaleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateQuickSaleCommandValidator : AbstractValidator<CreateQuickSaleCommand>
{
    public CreateQuickSaleCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public sealed class ConvertQuoteToSaleCommandValidator : AbstractValidator<ConvertQuoteToSaleCommand>
{
    public ConvertQuoteToSaleCommandValidator() => RuleFor(x => x.QuoteId).GreaterThan(0);
}

public sealed class GetQuotesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetQuotesQuery, AppResult<PagedResult<QuoteListItemDto>>>
{
    public async Task<AppResult<PagedResult<QuoteListItemDto>>> Handle(GetQuotesQuery request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireQuotesAsync(access, ct);
        if (!scope.Success) return AppResult<PagedResult<QuoteListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var accountId = scope.AccountId;
        var query = db.Quotes.AsNoTracking().Where(x => x.AccountId == accountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Number.Contains(search) || x.Customer.Name.Contains(search));
        }
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        if (request.CustomerId.HasValue) query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        if (request.OnlyConvertible == true)
        {
            var now = DateTime.UtcNow.Date;
            query = query.Where(x => x.Status != QuoteStatus.Rejected && x.Status != QuoteStatus.Expired && x.Status != QuoteStatus.Converted && (!x.ValidUntilUtc.HasValue || x.ValidUntilUtc.Value >= now));
        }

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, CommerceFeatureHelpers.MaxPageSize);
        var nowUtc = DateTime.UtcNow;
        var items = await query.OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QuoteListItemDto(x.Id, x.Number, x.Status, x.CustomerId, x.Customer.Name, x.IssuedAtUtc, x.ValidUntilUtc, x.Subtotal, x.Total, x.Items.Count, x.Status != QuoteStatus.Rejected && x.Status != QuoteStatus.Expired && x.Status != QuoteStatus.Converted && (!x.ValidUntilUtc.HasValue || x.ValidUntilUtc.Value >= nowUtc.Date), x.ConvertedToSaleAtUtc))
            .ToListAsync(ct);

        return AppResult<PagedResult<QuoteListItemDto>>.Ok(new PagedResult<QuoteListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetQuoteByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetQuoteByIdQuery, AppResult<QuoteDetailDto>>
{
    public async Task<AppResult<QuoteDetailDto>> Handle(GetQuoteByIdQuery request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireQuotesAsync(access, ct);
        if (!scope.Success) return AppResult<QuoteDetailDto>.Fail(scope.ErrorCode, scope.Message);

        var quote = await db.Quotes.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.GeneratedSales)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);

        if (quote is null) return AppResult<QuoteDetailDto>.Fail("not_found", "Presupuesto no encontrado.");

        var generatedSale = quote.GeneratedSales.OrderByDescending(x => x.Id).FirstOrDefault();
        return AppResult<QuoteDetailDto>.Ok(new QuoteDetailDto(
            quote.Id,
            quote.Number,
            quote.Status,
            quote.CustomerId,
            quote.Customer.Name,
            quote.IssuedAtUtc,
            quote.ValidUntilUtc,
            quote.Observations,
            quote.Subtotal,
            quote.Total,
            CommercialCircuitHelpers.MapQuoteLines(quote),
            quote.CreatedByUserId,
            quote.CreatedAtUtc,
            quote.ModifiedByUserId,
            quote.ModifiedAtUtc,
            quote.ConvertedToSaleAtUtc,
            generatedSale?.Id,
            generatedSale?.Number,
            CommercialCircuitHelpers.IsQuoteConvertible(quote, DateTime.UtcNow)));
    }
}

public sealed class CreateQuoteCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateQuoteCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateQuoteCommand request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireQuotesAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        if (!await db.Customers.AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.CustomerId && x.IsActive, ct))
            return AppResult<int>.Fail("customer_not_found", "Cliente no encontrado o inactivo.");
        if (request.Status == QuoteStatus.Converted)
            return AppResult<int>.Fail("invalid_status", "El presupuesto no puede nacer como convertido.");

        var skuData = await CommercialCircuitHelpers.LoadSkuDataAsync(db, scope.AccountId, ct);
        List<CommercialCircuitHelpers.CommercialPreparedLine> prepared;
        try { prepared = CommercialCircuitHelpers.PrepareLines(request.Items, skuData); }
        catch { return AppResult<int>.Fail("item_not_found", "Uno o más ítems no existen en el catálogo actual."); }

        var entity = new Quote
        {
            AccountId = scope.AccountId,
            CustomerId = request.CustomerId,
            Status = request.Status,
            IssuedAtUtc = request.IssuedAtUtc,
            ValidUntilUtc = request.ValidUntilUtc,
            Observations = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim()
        };
        CommerceFeatureHelpers.TouchCreate(entity, current);
        CommercialCircuitHelpers.ApplyQuoteLines(entity, prepared, current);

        db.Quotes.Add(entity);
        await db.SaveChangesAsync(ct);
        entity.Number = CommercialCircuitHelpers.BuildQuoteNumber(entity.Id);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, "Quote", entity.Id, "created", $"Presupuesto {entity.Number} creado", ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UpdateQuoteCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateQuoteCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateQuoteCommand request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireQuotesAsync(access, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);

        var quote = await db.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);
        if (quote is null) return AppResult.Fail("not_found", "Presupuesto no encontrado.");
        if (quote.Status == QuoteStatus.Converted) return AppResult.Fail("already_converted", "No se puede editar un presupuesto ya convertido.");
        if (!await db.Customers.AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.CustomerId && x.IsActive, ct))
            return AppResult.Fail("customer_not_found", "Cliente no encontrado o inactivo.");
        if (request.Status == QuoteStatus.Converted)
            return AppResult.Fail("invalid_status", "El presupuesto no puede quedar manualmente como convertido.");

        var skuData = await CommercialCircuitHelpers.LoadSkuDataAsync(db, scope.AccountId, ct);
        List<CommercialCircuitHelpers.CommercialPreparedLine> prepared;
        try { prepared = CommercialCircuitHelpers.PrepareLines(request.Items, skuData); }
        catch { return AppResult.Fail("item_not_found", "Uno o más ítems no existen en el catálogo actual."); }

        quote.CustomerId = request.CustomerId;
        quote.Status = request.Status;
        quote.IssuedAtUtc = request.IssuedAtUtc;
        quote.ValidUntilUtc = request.ValidUntilUtc;
        quote.Observations = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim();
        CommerceFeatureHelpers.TouchUpdate(quote, current);
        CommercialCircuitHelpers.ApplyQuoteLines(quote, prepared, current);

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, "Quote", quote.Id, "updated", $"Presupuesto {quote.Number} actualizado", ct);
        return AppResult.Ok();
    }
}

public sealed class ConvertQuoteToSaleCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<ConvertQuoteToSaleCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(ConvertQuoteToSaleCommand request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireSalesAndQuotesAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var quote = await db.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.QuoteId, ct);
            if (quote is null) return AppResult<int>.Fail("not_found", "Presupuesto no encontrado.");
            if (!CommercialCircuitHelpers.IsQuoteConvertible(quote, DateTime.UtcNow))
                return AppResult<int>.Fail("invalid_status", "El presupuesto no está en estado válido para convertirse a venta.");
            if (await db.Sales.AnyAsync(x => x.AccountId == scope.AccountId && x.SourceQuoteId == quote.Id, ct))
                return AppResult<int>.Fail("already_converted", "El presupuesto ya fue convertido a una venta.");

            var sale = new Sale
            {
                AccountId = scope.AccountId,
                CustomerId = quote.CustomerId,
                Status = request.SaleStatus,
                IssuedAtUtc = request.IssuedAtUtc ?? DateTime.UtcNow,
                Observations = string.IsNullOrWhiteSpace(request.Observations) ? quote.Observations : request.Observations.Trim(),
                SourceQuoteId = quote.Id,
                Subtotal = quote.Subtotal,
                Total = quote.Total
            };
            CommerceFeatureHelpers.TouchCreate(sale, current);
            foreach (var item in quote.Items.OrderBy(x => x.SortOrder))
            {
                sale.Items.Add(new SaleItem
                {
                    AccountId = scope.AccountId,
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    Description = item.Description,
                    InternalCode = item.InternalCode,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineSubtotal = item.LineSubtotal,
                    SortOrder = item.SortOrder,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = current.UserId
                });
            }

            db.Sales.Add(sale);
            await db.SaveChangesAsync(ct);
            sale.Number = CommercialCircuitHelpers.BuildSaleNumber(sale.Id);
            quote.Status = QuoteStatus.Converted;
            quote.ConvertedToSaleAtUtc = DateTime.UtcNow;
            CommerceFeatureHelpers.TouchUpdate(quote, current);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await audit.WriteAsync(scope.AccountId, null, "Sale", sale.Id, "created", $"Venta {sale.Number} creada desde {quote.Number}", ct);
            await audit.WriteAsync(scope.AccountId, null, "Quote", quote.Id, "converted", $"Presupuesto {quote.Number} convertido en venta {sale.Number}", ct);
            return AppResult<int>.Ok(sale.Id);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}

public sealed class GetSalesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSalesQuery, AppResult<PagedResult<SaleListItemDto>>>
{
    public async Task<AppResult<PagedResult<SaleListItemDto>>> Handle(GetSalesQuery request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireSalesAsync(access, ct);
        if (!scope.Success) return AppResult<PagedResult<SaleListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var query = db.Sales.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Number.Contains(search) || x.Customer.Name.Contains(search) || (x.SourceQuote != null && x.SourceQuote.Number.Contains(search)));
        }
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        if (request.CustomerId.HasValue) query = query.Where(x => x.CustomerId == request.CustomerId.Value);

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, CommerceFeatureHelpers.MaxPageSize);
        var items = await query.OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SaleListItemDto(x.Id, x.Number, x.Status, x.CustomerId, x.Customer.Name, x.IssuedAtUtc, x.Subtotal, x.Total, x.Items.Count, x.SourceQuoteId, x.SourceQuote != null ? x.SourceQuote.Number : null))
            .ToListAsync(ct);

        return AppResult<PagedResult<SaleListItemDto>>.Ok(new PagedResult<SaleListItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetSaleByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSaleByIdQuery, AppResult<SaleDetailDto>>
{
    public async Task<AppResult<SaleDetailDto>> Handle(GetSaleByIdQuery request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireSalesAsync(access, ct);
        if (!scope.Success) return AppResult<SaleDetailDto>.Fail(scope.ErrorCode, scope.Message);

        var sale = await db.Sales.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.SourceQuote)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);

        if (sale is null) return AppResult<SaleDetailDto>.Fail("not_found", "Venta no encontrada.");

        return AppResult<SaleDetailDto>.Ok(new SaleDetailDto(
            sale.Id,
            sale.Number,
            sale.Status,
            sale.CustomerId,
            sale.Customer.Name,
            sale.IssuedAtUtc,
            sale.Observations,
            sale.Subtotal,
            sale.Total,
            CommercialCircuitHelpers.MapSaleLines(sale),
            sale.CreatedByUserId,
            sale.CreatedAtUtc,
            sale.ModifiedByUserId,
            sale.ModifiedAtUtc,
            sale.SourceQuoteId,
            sale.SourceQuote?.Number));
    }
}

public sealed class CreateSaleCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateSaleCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateSaleCommand request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireSalesAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        if (!await db.Customers.AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.CustomerId && x.IsActive, ct))
            return AppResult<int>.Fail("customer_not_found", "Cliente no encontrado o inactivo.");

        var skuData = await CommercialCircuitHelpers.LoadSkuDataAsync(db, scope.AccountId, ct);
        List<CommercialCircuitHelpers.CommercialPreparedLine> prepared;
        try { prepared = CommercialCircuitHelpers.PrepareLines(request.Items, skuData); }
        catch { return AppResult<int>.Fail("item_not_found", "Uno o más ítems no existen en el catálogo actual."); }

        var sale = new Sale
        {
            AccountId = scope.AccountId,
            CustomerId = request.CustomerId,
            Status = request.Status,
            IssuedAtUtc = request.IssuedAtUtc,
            Observations = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim()
        };
        CommerceFeatureHelpers.TouchCreate(sale, current);
        CommercialCircuitHelpers.ApplySaleLines(sale, prepared, current);
        db.Sales.Add(sale);
        await db.SaveChangesAsync(ct);
        sale.Number = CommercialCircuitHelpers.BuildSaleNumber(sale.Id);
        await FinancialHelpers.UpsertCustomerMovementAsync(db, sale, current, ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, "Sale", sale.Id, "created", $"Venta {sale.Number} creada", ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, "Sale", sale.Id, sale.Number, "created", $"Venta {sale.Number} creada.", new { sale.CustomerId, sale.Status, sale.Total }, sale.SourceQuote?.Number, ct);
        return AppResult<int>.Ok(sale.Id);
    }
}

public sealed class UpdateSaleCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpdateSaleCommand, AppResult>
{
    public async Task<AppResult> Handle(UpdateSaleCommand request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireSalesAsync(access, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);

        var sale = await db.Sales.Include(x => x.Items).FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);
        if (sale is null) return AppResult.Fail("not_found", "Venta no encontrada.");
        if (!await db.Customers.AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.CustomerId && x.IsActive, ct))
            return AppResult.Fail("customer_not_found", "Cliente no encontrado o inactivo.");

        var skuData = await CommercialCircuitHelpers.LoadSkuDataAsync(db, scope.AccountId, ct);
        List<CommercialCircuitHelpers.CommercialPreparedLine> prepared;
        try { prepared = CommercialCircuitHelpers.PrepareLines(request.Items, skuData); }
        catch { return AppResult.Fail("item_not_found", "Uno o más ítems no existen en el catálogo actual."); }

        sale.CustomerId = request.CustomerId;
        sale.Status = request.Status;
        sale.IssuedAtUtc = request.IssuedAtUtc;
        sale.Observations = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim();
        CommerceFeatureHelpers.TouchUpdate(sale, current);
        CommercialCircuitHelpers.ApplySaleLines(sale, prepared, current);
        await FinancialHelpers.UpsertCustomerMovementAsync(db, sale, current, ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, "Sale", sale.Id, "updated", $"Venta {sale.Number} actualizada", ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, "Sale", sale.Id, sale.Number, "updated", $"Venta {sale.Number} actualizada.", new { sale.CustomerId, sale.Status, sale.Total, sale.Observations }, sale.SourceQuote?.Number, ct);
        return AppResult.Ok();
    }
}

public sealed class CreateQuickSaleCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateQuickSaleCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateQuickSaleCommand request, CancellationToken ct)
    {
        var scope = await CommercialCircuitHelpers.RequireSalesAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        if (!await db.Customers.AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.CustomerId && x.IsActive, ct))
            return AppResult<int>.Fail("customer_not_found", "Cliente no encontrado o inactivo.");

        var skuData = await CommercialCircuitHelpers.LoadSkuDataAsync(db, scope.AccountId, ct);
        var items = request.Items.Select((x, idx) =>
        {
            if (!skuData.TryGetValue((x.ProductId, x.ProductVariantId), out var sku)) throw new InvalidOperationException("item_not_found");
            return new CommercialLineInput(x.ProductId, x.ProductVariantId, CommercialCircuitHelpers.BuildSkuLabel(sku.ProductName, sku.VariantName), x.Quantity, sku.SalePrice, idx + 1);
        }).ToList();

        var command = new CreateSaleCommand(request.CustomerId, request.Status, request.IssuedAtUtc, request.Observations, items);
        var handler = new CreateSaleCommandHandler(db, access, current, audit);
        return await handler.Handle(command, ct);
    }
}
