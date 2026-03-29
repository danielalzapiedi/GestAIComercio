using GestAI.Application.Saas;
using System.Text.Json;
using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GestAI.Application.Commerce;

public sealed record GetRelease6SeedDataQuery : IRequest<AppResult<Release6SeedDataDto>>;
public sealed record GetFiscalConfigurationQuery : IRequest<AppResult<FiscalConfigurationDto?>>;
public sealed record UpsertFiscalConfigurationCommand(
    int Id,
    string LegalName,
    string TaxIdentifier,
    string? GrossIncomeTaxId,
    int PointOfSale,
    InvoiceType DefaultInvoiceType,
    FiscalIntegrationMode IntegrationMode,
    bool UseSandbox,
    bool IsActive,
    string? CertificateReference,
    string? PrivateKeyReference,
    string? ApiBaseUrl,
    string? Observations) : IRequest<AppResult<int>>;
public sealed record UploadFiscalCredentialCommand(string FileName, string ContentBase64, string? ContentType, bool IsPrivateKey) : IRequest<AppResult<string>>;

public sealed record GetInvoicesQuery(string? Search = null, InvoiceStatus? Status = null, int? SaleId = null, int? CustomerId = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<CommercialInvoiceListItemDto>>>;
public sealed record GetInvoiceByIdQuery(int Id) : IRequest<AppResult<CommercialInvoiceDetailDto>>;
public sealed record GetInvoicePdfQuery(int Id) : IRequest<AppResult<DocumentFileResult>>;
public sealed record CreateInvoiceCommand(int SaleId, int? FiscalConfigurationId, InvoiceType? InvoiceType, DateTime? IssuedAtUtc, decimal TaxRate) : IRequest<AppResult<int>>;
public sealed record SubmitInvoiceToArcaCommand(int InvoiceId) : IRequest<AppResult>;

public sealed record GetDeliveryNotesQuery(string? Search = null, DeliveryNoteStatus? Status = null, int? SaleId = null, int? WarehouseId = null, int Page = 1, int PageSize = 20) : IRequest<AppResult<PagedResult<DeliveryNoteListItemDto>>>;
public sealed record GetDeliveryNoteByIdQuery(int Id) : IRequest<AppResult<DeliveryNoteDetailDto>>;
public sealed record GetDeliveryNotePdfQuery(int Id) : IRequest<AppResult<DocumentFileResult>>;
public sealed record CreateDeliveryNoteCommand(int SaleId, int WarehouseId, int? CommercialInvoiceId, DateTime? DeliveredAtUtc, string? Observations, IReadOnlyList<CreateDeliveryNoteLineInput> Items) : IRequest<AppResult<int>>;
public sealed record CreateDeliveryNoteLineInput(int SaleItemId, decimal QuantityDelivered);

public sealed record GetOperationalReportQuery(DateOnly From, DateOnly To, int Top = 10) : IRequest<AppResult<OperationalReportDto>>;
public sealed record GetRelease6DashboardQuery : IRequest<AppResult<Release6DashboardDto>>;
public sealed record GetDocumentTraceabilityQuery(string? EntityName = null, string? Search = null, int Take = 100) : IRequest<AppResult<DocumentTraceabilityDto>>;

public static class Release6Helpers
{
    public const int CommercialInvoiceFiscalStatusMaxLength = 500;
    public const int FiscalSubmissionErrorMaxLength = 1000;
    public const int AuditSummaryMaxLength = 2000;
    public const int DocumentChangeSummaryMaxLength = 500;
    public const int MaxFiscalCredentialBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedFiscalCertificateExtensions = new(StringComparer.OrdinalIgnoreCase) { ".crt", ".cer", ".pem", ".pfx", ".p12" };
    private static readonly HashSet<string> AllowedFiscalPrivateKeyExtensions = new(StringComparer.OrdinalIgnoreCase) { ".key", ".pem" };

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireSalesAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Sales, ct);

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequirePurchasesAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Purchases, ct);

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireProductsAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Products, ct);

    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value!.Length <= maxLength)
            return value;

        if (maxLength <= 3)
            return value[..maxLength];

        return value[..(maxLength - 3)] + "...";
    }

    public static bool IsAllowedFiscalCredentialExtension(string? fileName, bool isPrivateKey)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return isPrivateKey
            ? AllowedFiscalPrivateKeyExtensions.Contains(extension)
            : AllowedFiscalCertificateExtensions.Contains(extension);
    }

    public static async Task<DocumentSequence> NextSequenceAsync(IAppDbContext db, int accountId, string documentType, int pointOfSale, string prefix, ICurrentUser current, CancellationToken ct)
    {
        var sequence = await db.DocumentSequences.FirstOrDefaultAsync(x => x.AccountId == accountId && x.DocumentType == documentType && x.PointOfSale == pointOfSale, ct);
        if (sequence is null)
        {
            sequence = new DocumentSequence
            {
                AccountId = accountId,
                DocumentType = documentType,
                PointOfSale = pointOfSale,
                Prefix = prefix,
                LastNumber = 0,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            };
            db.DocumentSequences.Add(sequence);
        }

        sequence.LastNumber += 1;
        sequence.ModifiedAtUtc = DateTime.UtcNow;
        sequence.ModifiedByUserId = current.UserId;
        return sequence;
    }

    public static string FormatNumber(DocumentSequence sequence)
        => $"{sequence.Prefix}-{sequence.PointOfSale:D4}-{sequence.LastNumber:D8}";

    public static async Task AppendChangeAsync(IAppDbContext db, ICurrentUser current, int accountId, string entityName, int entityId, string documentNumber, string action, string summary, object? changedFields, string? relatedDocumentNumber, CancellationToken ct)
    {
        db.DocumentChangeLogs.Add(new DocumentChangeLog
        {
            AccountId = accountId,
            EntityName = entityName,
            EntityId = entityId,
            DocumentNumber = documentNumber,
            Action = action,
            Summary = summary,
            ChangedFields = changedFields is null ? null : JsonSerializer.Serialize(changedFields),
            RelatedDocumentNumber = relatedDocumentNumber,
            UserId = current.UserId,
            UserName = current.FullName ?? current.Email,
            ChangedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public static async Task<FiscalConfiguration?> GetActiveFiscalConfigurationAsync(IAppDbContext db, int accountId, int? id, CancellationToken ct)
    {
        var query = db.FiscalConfigurations.Where(x => x.AccountId == accountId && x.IsActive);
        if (id.HasValue)
            query = query.Where(x => x.Id == id.Value);
        return await query.OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
    }

    public static CommercialInvoiceDetailDto MapInvoiceDetail(CommercialInvoice invoice, List<LookupDto> deliveryNotes)
        => new(
            invoice.Id,
            invoice.Number,
            invoice.InvoiceType,
            invoice.Status,
            invoice.SaleId,
            invoice.Sale.Number,
            invoice.CustomerId,
            invoice.Customer.Name,
            invoice.FiscalConfigurationId,
            invoice.PointOfSale,
            invoice.SequentialNumber,
            invoice.IssuedAtUtc,
            invoice.Subtotal,
            invoice.TaxAmount,
            invoice.OtherTaxesAmount,
            invoice.Total,
            invoice.CurrencyCode,
            invoice.FiscalStatusDetail,
            invoice.Cae,
            invoice.CaeDueDateUtc,
            invoice.LastSubmissionAtUtc,
            invoice.Items.OrderBy(x => x.SortOrder).Select(x => new InvoiceLineDto(x.Id, x.SaleItemId, x.ProductId, x.ProductVariantId, x.Description, x.InternalCode, x.Quantity, x.UnitPrice, x.LineSubtotal, x.TaxRate, x.TaxAmount, x.SortOrder)).ToList(),
            invoice.FiscalSubmissions.OrderByDescending(x => x.AttemptNumber).Select(x => new FiscalSubmissionDto(x.Id, x.AttemptNumber, x.Status, x.RequestedAtUtc, x.RespondedAtUtc, x.RequestPayload, x.ResponsePayload, x.ErrorMessage, x.ExternalReference)).ToList(),
            deliveryNotes,
            invoice.CreatedByUserId,
            invoice.CreatedAtUtc,
            invoice.ModifiedByUserId,
            invoice.ModifiedAtUtc);

    public static DeliveryNoteDetailDto MapDeliveryDetail(DeliveryNote note)
        => new(
            note.Id,
            note.Number,
            note.Status,
            note.SaleId,
            note.Sale.Number,
            note.CustomerId,
            note.Customer.Name,
            note.WarehouseId,
            note.Warehouse.Name,
            note.CommercialInvoiceId,
            note.CommercialInvoice?.Number,
            note.DeliveredAtUtc,
            note.Observations,
            note.TotalQuantity,
            note.PendingQuantity,
            note.Items.OrderBy(x => x.SortOrder).Select(x => new DeliveryNoteLineDto(x.Id, x.SaleItemId, x.ProductId, x.ProductVariantId, x.Description, x.InternalCode, x.QuantityOrdered, x.QuantityDelivered, x.QuantityOrdered - x.QuantityDelivered, x.SortOrder)).ToList(),
            note.CreatedByUserId,
            note.CreatedAtUtc,
            note.ModifiedByUserId,
            note.ModifiedAtUtc);
}

public sealed class UpsertFiscalConfigurationCommandValidator : AbstractValidator<UpsertFiscalConfigurationCommand>
{
    public UpsertFiscalConfigurationCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxIdentifier).NotEmpty().MaximumLength(32);
        RuleFor(x => x.PointOfSale).GreaterThan(0);
    }
}

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.SaleId).GreaterThan(0);
        RuleFor(x => x.TaxRate).GreaterThanOrEqualTo(0);
    }
}

public sealed class UploadFiscalCredentialCommandValidator : AbstractValidator<UploadFiscalCredentialCommand>
{
    public UploadFiscalCredentialCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentBase64).NotEmpty();
        RuleFor(x => x.ContentBase64).MaximumLength(Release6Helpers.MaxFiscalCredentialBytes * 2);
    }
}

public sealed class CreateDeliveryNoteCommandValidator : AbstractValidator<CreateDeliveryNoteCommand>
{
    public CreateDeliveryNoteCommandValidator()
    {
        RuleFor(x => x.SaleId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        When(x => x.Items.Count > 0, () =>
        {
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.SaleItemId).GreaterThan(0);
                item.RuleFor(x => x.QuantityDelivered).GreaterThan(0);
            });
        });
    }
}

public sealed class GetRelease6SeedDataQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetRelease6SeedDataQuery, AppResult<Release6SeedDataDto>>
{
    public async Task<AppResult<Release6SeedDataDto>> Handle(GetRelease6SeedDataQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<Release6SeedDataDto>.Fail(scope.ErrorCode, scope.Message);

        var sales = await db.Sales.AsNoTracking().Where(x => x.AccountId == scope.AccountId).OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id).Take(100)
            .Select(x => new LookupDto(x.Id, $"{x.Number} · {x.Customer.Name}"))
            .ToListAsync(ct);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IsActive).OrderBy(x => x.Name)
            .Select(x => new LookupDto(x.Id, x.Name))
            .ToListAsync(ct);
        var invoices = await db.CommercialInvoices.AsNoTracking().Where(x => x.AccountId == scope.AccountId).OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id).Take(100)
            .Select(x => new LookupDto(x.Id, x.Number))
            .ToListAsync(ct);
        var fiscal = await db.FiscalConfigurations.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IsActive).OrderByDescending(x => x.Id)
            .Select(x => new FiscalConfigurationDto(x.Id, x.LegalName, x.TaxIdentifier, x.GrossIncomeTaxId, x.PointOfSale, x.DefaultInvoiceType, x.IntegrationMode, x.UseSandbox, x.IsActive, x.CertificateReference, x.PrivateKeyReference, x.ApiBaseUrl, x.Observations, x.LastConnectionCheckAtUtc, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);

        return AppResult<Release6SeedDataDto>.Ok(new Release6SeedDataDto(sales, warehouses, invoices, fiscal));
    }
}

public sealed class GetFiscalConfigurationQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetFiscalConfigurationQuery, AppResult<FiscalConfigurationDto?>>
{
    public async Task<AppResult<FiscalConfigurationDto?>> Handle(GetFiscalConfigurationQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<FiscalConfigurationDto?>.Fail(scope.ErrorCode, scope.Message);

        var item = await db.FiscalConfigurations.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IsActive).OrderByDescending(x => x.Id)
            .Select(x => new FiscalConfigurationDto(x.Id, x.LegalName, x.TaxIdentifier, x.GrossIncomeTaxId, x.PointOfSale, x.DefaultInvoiceType, x.IntegrationMode, x.UseSandbox, x.IsActive, x.CertificateReference, x.PrivateKeyReference, x.ApiBaseUrl, x.Observations, x.LastConnectionCheckAtUtc, x.CreatedByUserId, x.CreatedAtUtc, x.ModifiedByUserId, x.ModifiedAtUtc))
            .FirstOrDefaultAsync(ct);
        return AppResult<FiscalConfigurationDto?>.Ok(item);
    }
}

public sealed class UpsertFiscalConfigurationCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<UpsertFiscalConfigurationCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(UpsertFiscalConfigurationCommand request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        var entity = request.Id > 0
            ? await db.FiscalConfigurations.FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct)
            : null;

        var changes = new Dictionary<string, object?>
        {
            [nameof(request.LegalName)] = request.LegalName,
            [nameof(request.TaxIdentifier)] = request.TaxIdentifier,
            [nameof(request.PointOfSale)] = request.PointOfSale,
            [nameof(request.DefaultInvoiceType)] = request.DefaultInvoiceType,
            [nameof(request.IntegrationMode)] = request.IntegrationMode,
            [nameof(request.UseSandbox)] = request.UseSandbox,
            [nameof(request.IsActive)] = request.IsActive
        };

        if (entity is null)
        {
            entity = new FiscalConfiguration { AccountId = scope.AccountId };
            db.FiscalConfigurations.Add(entity);
            CommerceFeatureHelpers.TouchCreate(entity, current);
        }
        else
        {
            CommerceFeatureHelpers.TouchUpdate(entity, current);
        }

        entity.LegalName = request.LegalName.Trim();
        entity.TaxIdentifier = request.TaxIdentifier.Trim();
        entity.GrossIncomeTaxId = request.GrossIncomeTaxId?.Trim();
        entity.PointOfSale = request.PointOfSale;
        entity.DefaultInvoiceType = request.DefaultInvoiceType;
        entity.IntegrationMode = request.IntegrationMode;
        entity.UseSandbox = request.UseSandbox;
        entity.IsActive = request.IsActive;
        entity.CertificateReference = request.CertificateReference?.Trim();
        entity.PrivateKeyReference = request.PrivateKeyReference?.Trim();
        entity.ApiBaseUrl = request.ApiBaseUrl?.Trim();
        entity.Observations = request.Observations?.Trim();

        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(FiscalConfiguration), entity.Id, request.Id > 0 ? "updated" : "created", $"Configuración fiscal {(request.Id > 0 ? "actualizada" : "creada")} para PV {entity.PointOfSale}.", ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, nameof(FiscalConfiguration), entity.Id, $"FISC-{entity.PointOfSale:D4}", request.Id > 0 ? "updated" : "created", $"Configuración fiscal {(request.Id > 0 ? "actualizada" : "creada")}.", changes, null, ct);
        return AppResult<int>.Ok(entity.Id);
    }
}

public sealed class UploadFiscalCredentialCommandHandler(IUserAccessService access, IFiscalCredentialStore store)
    : IRequestHandler<UploadFiscalCredentialCommand, AppResult<string>>
{
    public async Task<AppResult<string>> Handle(UploadFiscalCredentialCommand request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<string>.Fail(scope.ErrorCode, scope.Message);

        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.ContentBase64);
        }
        catch (FormatException)
        {
            return AppResult<string>.Fail("invalid_file", "El archivo fiscal no tiene un contenido válido.");
        }

        if (content.Length == 0)
            return AppResult<string>.Fail("empty_file", "El archivo fiscal está vacío.");
        if (content.Length > Release6Helpers.MaxFiscalCredentialBytes)
            return AppResult<string>.Fail("file_too_large", $"El archivo fiscal supera el límite de {Release6Helpers.MaxFiscalCredentialBytes / (1024 * 1024)} MB.");
        if (!Release6Helpers.IsAllowedFiscalCredentialExtension(request.FileName, request.IsPrivateKey))
            return AppResult<string>.Fail("invalid_extension", request.IsPrivateKey
                ? "La clave privada debe ser .key o .pem."
                : "El certificado debe ser .crt, .cer, .pem, .pfx o .p12.");

        var reference = await store.SaveAsync(scope.AccountId, request.FileName, content, request.IsPrivateKey, ct);
        return AppResult<string>.Ok(reference);
    }
}

public sealed class GetInvoicesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetInvoicesQuery, AppResult<PagedResult<CommercialInvoiceListItemDto>>>
{
    public async Task<AppResult<PagedResult<CommercialInvoiceListItemDto>>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<PagedResult<CommercialInvoiceListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var query = db.CommercialInvoices.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.Number.Contains(request.Search) || x.Customer.Name.Contains(request.Search) || x.Sale.Number.Contains(request.Search));
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        if (request.SaleId.HasValue)
            query = query.Where(x => x.SaleId == request.SaleId.Value);
        if (request.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new CommercialInvoiceListItemDto(x.Id, x.Number, x.InvoiceType, x.Status, x.SaleId, x.Sale.Number, x.CustomerId, x.Customer.Name, x.PointOfSale, x.SequentialNumber, x.IssuedAtUtc, x.Subtotal, x.TaxAmount, x.Total, x.Cae, x.CaeDueDateUtc, x.LastSubmissionAtUtc))
            .ToListAsync(ct);

        return AppResult<PagedResult<CommercialInvoiceListItemDto>>.Ok(new PagedResult<CommercialInvoiceListItemDto>(items, total, request.Page, request.PageSize));
    }
}

public sealed class GetInvoiceByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetInvoiceByIdQuery, AppResult<CommercialInvoiceDetailDto>>
{
    public async Task<AppResult<CommercialInvoiceDetailDto>> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<CommercialInvoiceDetailDto>.Fail(scope.ErrorCode, scope.Message);

        var invoice = await db.CommercialInvoices
            .Include(x => x.Sale)
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .Include(x => x.FiscalSubmissions)
            .Include(x => x.DeliveryNotes)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);
        if (invoice is null)
            return AppResult<CommercialInvoiceDetailDto>.Fail("not_found", "Factura no encontrada.");

        var notes = invoice.DeliveryNotes.OrderByDescending(x => x.DeliveredAtUtc).Select(x => new LookupDto(x.Id, x.Number)).ToList();
        return AppResult<CommercialInvoiceDetailDto>.Ok(Release6Helpers.MapInvoiceDetail(invoice, notes));
    }
}

public sealed class GetInvoicePdfQueryHandler(IAppDbContext db, IUserAccessService access, ICommercialDocumentPdfService pdfService)
    : IRequestHandler<GetInvoicePdfQuery, AppResult<DocumentFileResult>>
{
    public async Task<AppResult<DocumentFileResult>> Handle(GetInvoicePdfQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<DocumentFileResult>.Fail(scope.ErrorCode, scope.Message);

        var invoice = await db.CommercialInvoices
            .AsNoTracking()
            .Include(x => x.Sale)
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .Include(x => x.DeliveryNotes)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);
        if (invoice is null)
            return AppResult<DocumentFileResult>.Fail("not_found", "Factura no encontrada.");

        return AppResult<DocumentFileResult>.Ok(await pdfService.BuildInvoicePdfAsync(invoice, ct));
    }
}

public sealed class CreateInvoiceCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateInvoiceCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var sale = await db.Sales.Include(x => x.Customer).Include(x => x.Items).Include(x => x.Invoices).FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.SaleId, ct);
        if (sale is null)
            return AppResult<int>.Fail("not_found", "Venta no encontrada.");
        if (sale.Status == SaleStatus.Cancelled)
            return AppResult<int>.Fail("invalid_state", "No se puede facturar una venta cancelada.");

        var config = await Release6Helpers.GetActiveFiscalConfigurationAsync(db, scope.AccountId, request.FiscalConfigurationId, ct);
        if (config is null)
            return AppResult<int>.Fail("fiscal_configuration_required", "Configurá el punto de venta fiscal antes de emitir facturas.");

        var sequence = await Release6Helpers.NextSequenceAsync(db, scope.AccountId, nameof(CommercialInvoice), config.PointOfSale, "FC", current, ct);
        var issueDate = request.IssuedAtUtc ?? DateTime.UtcNow;
        var subtotal = Release6Helpers.RoundMoney(sale.Items.Sum(x => x.LineSubtotal));
        var taxAmount = Release6Helpers.RoundMoney(subtotal * request.TaxRate);
        var invoice = new CommercialInvoice
        {
            AccountId = scope.AccountId,
            SaleId = sale.Id,
            CustomerId = sale.CustomerId,
            FiscalConfigurationId = config.Id,
            Number = Release6Helpers.FormatNumber(sequence),
            PointOfSale = config.PointOfSale,
            SequentialNumber = sequence.LastNumber,
            InvoiceType = request.InvoiceType ?? config.DefaultInvoiceType,
            Status = InvoiceStatus.Draft,
            IssuedAtUtc = issueDate,
            CurrencyCode = "ARS",
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            OtherTaxesAmount = 0m,
            Total = subtotal + taxAmount,
            FiscalStatusDetail = "Pendiente de autorización fiscal."
        };
        CommerceFeatureHelpers.TouchCreate(invoice, current);
        db.CommercialInvoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        foreach (var item in sale.Items.OrderBy(x => x.SortOrder))
        {
            db.CommercialInvoiceItems.Add(new CommercialInvoiceItem
            {
                AccountId = scope.AccountId,
                CommercialInvoiceId = invoice.Id,
                SaleItemId = item.Id,
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                Description = item.Description,
                InternalCode = item.InternalCode,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineSubtotal = item.LineSubtotal,
                TaxRate = request.TaxRate,
                TaxAmount = Release6Helpers.RoundMoney(item.LineSubtotal * request.TaxRate),
                SortOrder = item.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(CommercialInvoice), invoice.Id, "created", $"Factura {invoice.Number} generada desde venta {sale.Number}.", ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, nameof(CommercialInvoice), invoice.Id, invoice.Number, "created", $"Factura generada desde venta {sale.Number}.", new { sale.Number, config.PointOfSale, invoice.InvoiceType, request.TaxRate }, sale.Number, ct);
        return AppResult<int>.Ok(invoice.Id);
    }
}

public sealed class SubmitInvoiceToArcaCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit, IFiscalIntegrationService fiscalIntegration)
    : IRequestHandler<SubmitInvoiceToArcaCommand, AppResult>
{
    public async Task<AppResult> Handle(SubmitInvoiceToArcaCommand request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult.Fail(scope.ErrorCode, scope.Message);

        var invoice = await db.CommercialInvoices
            .Include(x => x.FiscalSubmissions)
            .Include(x => x.FiscalConfiguration)
            .Include(x => x.Customer)
            .Include(x => x.Sale)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.InvoiceId, ct);
        if (invoice is null)
            return AppResult.Fail("not_found", "Factura no encontrada.");
        if (invoice.FiscalConfiguration is null)
            return AppResult.Fail("fiscal_configuration_required", "La factura no tiene configuración fiscal asociada.");
        if (invoice.Status == InvoiceStatus.Authorized)
            return AppResult.Fail("already_authorized", "La factura ya fue autorizada.");

        invoice.Status = InvoiceStatus.PendingAuthorization;
        invoice.LastSubmissionAtUtc = DateTime.UtcNow;
        invoice.FiscalStatusDetail = "Enviada a ARCA.";
        CommerceFeatureHelpers.TouchUpdate(invoice, current);
        await db.SaveChangesAsync(ct);

        var result = await fiscalIntegration.AuthorizeInvoiceAsync(invoice, invoice.FiscalConfiguration, ct);
        var fiscalStatusDetail = Release6Helpers.Truncate(result.StatusDetail, Release6Helpers.CommercialInvoiceFiscalStatusMaxLength);
        var submissionError = Release6Helpers.Truncate(result.ErrorMessage, Release6Helpers.FiscalSubmissionErrorMaxLength);
        var submission = new FiscalDocumentSubmission
        {
            AccountId = scope.AccountId,
            CommercialInvoiceId = invoice.Id,
            AttemptNumber = invoice.FiscalSubmissions.Count + 1,
            Status = result.Status,
            RequestedAtUtc = DateTime.UtcNow,
            RespondedAtUtc = DateTime.UtcNow,
            RequestPayload = result.RequestPayload,
            ResponsePayload = result.ResponsePayload,
            ErrorMessage = submissionError,
            ExternalReference = result.ExternalReference,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = current.UserId
        };
        db.FiscalDocumentSubmissions.Add(submission);

        invoice.Status = result.Status switch
        {
            FiscalSubmissionStatus.Authorized => InvoiceStatus.Authorized,
            FiscalSubmissionStatus.Rejected => InvoiceStatus.Rejected,
            _ => InvoiceStatus.IntegrationError
        };
        invoice.Cae = result.Cae;
        invoice.CaeDueDateUtc = result.CaeDueDateUtc;
        invoice.FiscalStatusDetail = fiscalStatusDetail;
        invoice.LastSubmissionAtUtc = DateTime.UtcNow;
        CommerceFeatureHelpers.TouchUpdate(invoice, current);
        await db.SaveChangesAsync(ct);

        var auditSummary = Release6Helpers.Truncate($"Factura {invoice.Number} enviada a ARCA: {invoice.FiscalStatusDetail}", Release6Helpers.AuditSummaryMaxLength) ?? $"Factura {invoice.Number} enviada a ARCA.";
        var changeSummary = Release6Helpers.Truncate($"Resultado fiscal: {invoice.FiscalStatusDetail}", Release6Helpers.DocumentChangeSummaryMaxLength) ?? "Resultado fiscal actualizado.";
        await audit.WriteAsync(scope.AccountId, null, nameof(CommercialInvoice), invoice.Id, "submitted", auditSummary, ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, nameof(CommercialInvoice), invoice.Id, invoice.Number, "submitted", changeSummary, new { result.Status, result.Cae, result.CaeDueDateUtc, ErrorMessage = submissionError }, invoice.Sale.Number, ct);
        return AppResult.Ok();
    }
}

public sealed class GetDeliveryNotesQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetDeliveryNotesQuery, AppResult<PagedResult<DeliveryNoteListItemDto>>>
{
    public async Task<AppResult<PagedResult<DeliveryNoteListItemDto>>> Handle(GetDeliveryNotesQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<PagedResult<DeliveryNoteListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var query = db.DeliveryNotes.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.Number.Contains(request.Search) || x.Customer.Name.Contains(request.Search) || x.Sale.Number.Contains(request.Search));
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        if (request.SaleId.HasValue)
            query = query.Where(x => x.SaleId == request.SaleId.Value);
        if (request.WarehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == request.WarehouseId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.DeliveredAtUtc).ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new DeliveryNoteListItemDto(x.Id, x.Number, x.Status, x.SaleId, x.Sale.Number, x.CustomerId, x.Customer.Name, x.WarehouseId, x.Warehouse.Name, x.CommercialInvoiceId, x.CommercialInvoice != null ? x.CommercialInvoice.Number : null, x.DeliveredAtUtc, x.TotalQuantity, x.PendingQuantity))
            .ToListAsync(ct);
        return AppResult<PagedResult<DeliveryNoteListItemDto>>.Ok(new PagedResult<DeliveryNoteListItemDto>(items, total, request.Page, request.PageSize));
    }
}

public sealed class GetDeliveryNoteByIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetDeliveryNoteByIdQuery, AppResult<DeliveryNoteDetailDto>>
{
    public async Task<AppResult<DeliveryNoteDetailDto>> Handle(GetDeliveryNoteByIdQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<DeliveryNoteDetailDto>.Fail(scope.ErrorCode, scope.Message);

        var note = await db.DeliveryNotes
            .Include(x => x.Sale)
            .Include(x => x.Customer)
            .Include(x => x.Warehouse)
            .Include(x => x.CommercialInvoice)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);
        if (note is null)
            return AppResult<DeliveryNoteDetailDto>.Fail("not_found", "Remito no encontrado.");

        return AppResult<DeliveryNoteDetailDto>.Ok(Release6Helpers.MapDeliveryDetail(note));
    }
}

public sealed class GetDeliveryNotePdfQueryHandler(IAppDbContext db, IUserAccessService access, ICommercialDocumentPdfService pdfService)
    : IRequestHandler<GetDeliveryNotePdfQuery, AppResult<DocumentFileResult>>
{
    public async Task<AppResult<DocumentFileResult>> Handle(GetDeliveryNotePdfQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<DocumentFileResult>.Fail(scope.ErrorCode, scope.Message);

        var note = await db.DeliveryNotes
            .AsNoTracking()
            .Include(x => x.Sale)
            .Include(x => x.Customer)
            .Include(x => x.Warehouse)
            .Include(x => x.CommercialInvoice)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.Id, ct);
        if (note is null)
            return AppResult<DocumentFileResult>.Fail("not_found", "Remito no encontrado.");

        return AppResult<DocumentFileResult>.Ok(await pdfService.BuildDeliveryNotePdfAsync(note, ct));
    }
}

public sealed class CreateDeliveryNoteCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateDeliveryNoteCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateDeliveryNoteCommand request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        await using var tx = await db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var sale = await db.Sales.Include(x => x.Customer).Include(x => x.Items).Include(x => x.DeliveryNotes).ThenInclude(x => x.Items).FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.SaleId, ct);
        if (sale is null)
            return AppResult<int>.Fail("not_found", "Venta no encontrada.");
        if (sale.Status == SaleStatus.Cancelled)
            return AppResult<int>.Fail("invalid_state", "No se puede remitir una venta cancelada.");

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.WarehouseId && x.IsActive, ct);
        if (warehouse is null)
            return AppResult<int>.Fail("warehouse_not_found", "Depósito no encontrado.");

        CommercialInvoice? invoice = null;
        if (request.CommercialInvoiceId.HasValue)
        {
            invoice = await db.CommercialInvoices.FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.Id == request.CommercialInvoiceId.Value, ct);
            if (invoice is null)
                return AppResult<int>.Fail("invoice_not_found", "Factura asociada no encontrada.");
        }

        var pendingBySaleItem = sale.Items.ToDictionary(
            x => x.Id,
            x => x.Quantity - sale.DeliveryNotes.SelectMany(d => d.Items).Where(i => i.SaleItemId == x.Id).Sum(i => i.QuantityDelivered));

        var requestedItems = request.Items.Count == 0
            ? sale.Items.Where(x => pendingBySaleItem[x.Id] > 0m).Select(x => new CreateDeliveryNoteLineInput(x.Id, pendingBySaleItem[x.Id])).ToList()
            : request.Items.ToList();

        foreach (var item in requestedItems)
        {
            if (!pendingBySaleItem.TryGetValue(item.SaleItemId, out var pending) || pending <= 0m)
                return AppResult<int>.Fail("invalid_quantity", "Uno de los ítems ya no tiene saldo pendiente para remitir.");
            if (item.QuantityDelivered > pending)
                return AppResult<int>.Fail("invalid_quantity", "La cantidad entregada supera el saldo pendiente de la venta.");
        }

        var sequence = await Release6Helpers.NextSequenceAsync(db, scope.AccountId, nameof(DeliveryNote), 1, "REM", current, ct);
        var note = new DeliveryNote
        {
            AccountId = scope.AccountId,
            Number = Release6Helpers.FormatNumber(sequence),
            Status = DeliveryNoteStatus.Issued,
            SaleId = sale.Id,
            CustomerId = sale.CustomerId,
            WarehouseId = warehouse.Id,
            CommercialInvoiceId = invoice?.Id,
            DeliveredAtUtc = request.DeliveredAtUtc ?? DateTime.UtcNow,
            Observations = request.Observations?.Trim()
        };
        CommerceFeatureHelpers.TouchCreate(note, current);
        db.DeliveryNotes.Add(note);
        await db.SaveChangesAsync(ct);

        foreach (var line in sale.Items.OrderBy(x => x.SortOrder))
        {
            var delivered = requestedItems.FirstOrDefault(x => x.SaleItemId == line.Id)?.QuantityDelivered ?? 0m;
            if (delivered <= 0m)
                continue;

            db.DeliveryNoteItems.Add(new DeliveryNoteItem
            {
                AccountId = scope.AccountId,
                DeliveryNoteId = note.Id,
                SaleItemId = line.Id,
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                Description = line.Description,
                InternalCode = line.InternalCode,
                QuantityOrdered = line.Quantity,
                QuantityDelivered = delivered,
                SortOrder = line.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            });

            var stock = await db.ProductWarehouseStocks.FirstOrDefaultAsync(x => x.AccountId == scope.AccountId && x.ProductId == line.ProductId && x.ProductVariantId == line.ProductVariantId && x.WarehouseId == warehouse.Id, ct);
            if (stock is null)
            {
                stock = new ProductWarehouseStock
                {
                    AccountId = scope.AccountId,
                    ProductId = line.ProductId,
                    ProductVariantId = line.ProductVariantId,
                    WarehouseId = warehouse.Id,
                    QuantityOnHand = 0m
                };
                CommerceFeatureHelpers.TouchCreate(stock, current);
                db.ProductWarehouseStocks.Add(stock);
            }

            if (stock.QuantityOnHand - delivered < 0m)
                return AppResult<int>.Fail("insufficient_stock", $"Stock insuficiente para {line.Description} en depósito {warehouse.Name}.");
            stock.QuantityOnHand -= delivered;
            stock.LastMovementAtUtc = note.DeliveredAtUtc;
            CommerceFeatureHelpers.TouchUpdate(stock, current);
            db.StockMovements.Add(new StockMovement
            {
                AccountId = scope.AccountId,
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                WarehouseId = warehouse.Id,
                MovementType = StockMovementType.Outbound,
                QuantityDelta = -delivered,
                Reason = $"Remito {note.Number}",
                Note = $"Salida logística por venta {sale.Number}",
                ReferenceGroup = note.Number,
                OccurredAtUtc = note.DeliveredAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = current.UserId
            });
        }

        await db.SaveChangesAsync(ct);

        note.TotalQuantity = await db.DeliveryNoteItems
            .Where(x => x.AccountId == scope.AccountId && x.DeliveryNoteId == note.Id)
            .SumAsync(x => x.QuantityDelivered, ct);

        var deliveredBySaleItem = await db.DeliveryNoteItems
            .Where(x => x.AccountId == scope.AccountId && x.DeliveryNote.SaleId == sale.Id)
            .GroupBy(x => x.SaleItemId)
            .Select(g => new { SaleItemId = g.Key, Delivered = g.Sum(i => i.QuantityDelivered) })
            .ToDictionaryAsync(x => x.SaleItemId, x => x.Delivered, ct);

        var totalPending = sale.Items.Sum(x =>
        {
            var delivered = deliveredBySaleItem.TryGetValue(x.Id, out var value) ? value : 0m;
            return Math.Max(0m, x.Quantity - delivered);
        });
        note.PendingQuantity = totalPending;
        note.Status = totalPending > 0m ? DeliveryNoteStatus.PartiallyDelivered : DeliveryNoteStatus.Delivered;
        CommerceFeatureHelpers.TouchUpdate(note, current);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(DeliveryNote), note.Id, "created", $"Remito {note.Number} emitido para venta {sale.Number} desde {warehouse.Name}.", ct);
        await Release6Helpers.AppendChangeAsync(db, current, scope.AccountId, nameof(DeliveryNote), note.Id, note.Number, "created", $"Remito generado para la venta {sale.Number}.", new { warehouse.Name, note.TotalQuantity, note.PendingQuantity }, sale.Number, ct);
        return AppResult<int>.Ok(note.Id);
    }
}

public sealed class GetOperationalReportQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetOperationalReportQuery, AppResult<OperationalReportDto>>
{
    public async Task<AppResult<OperationalReportDto>> Handle(GetOperationalReportQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<OperationalReportDto>.Fail(scope.ErrorCode, scope.Message);

        var fromUtc = request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = request.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var sales = await db.Sales.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IssuedAtUtc >= fromUtc && x.IssuedAtUtc < toUtc && x.Status != SaleStatus.Cancelled)
            .Select(x => new { x.IssuedAtUtc, x.Total })
            .ToListAsync(ct);
        var purchases = await db.PurchaseDocuments.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IssuedAtUtc >= fromUtc && x.IssuedAtUtc < toUtc && x.Status != PurchaseDocumentStatus.Cancelled)
            .Select(x => new { x.IssuedAtUtc, x.Total })
            .ToListAsync(ct);
        var topProductsData = await db.SaleItems.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.Sale.IssuedAtUtc >= fromUtc && x.Sale.IssuedAtUtc < toUtc && x.Sale.Status != SaleStatus.Cancelled)
            .GroupBy(x => new { x.ProductId, x.ProductVariantId, x.Description, x.InternalCode })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductVariantId,
                g.Key.Description,
                g.Key.InternalCode,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineSubtotal),
                Documents = g.Select(x => x.SaleId).Distinct().Count()
            })
            .OrderByDescending(x => x.Quantity)
            .ThenByDescending(x => x.Revenue)
            .Take(request.Top)
            .ToListAsync(ct);

        var topProducts = topProductsData
            .Select(x => new TopProductReportDto(x.ProductId, x.ProductVariantId, x.Description, x.InternalCode, x.Quantity, x.Revenue, x.Documents))
            .ToList();
        var stock = await db.ProductWarehouseStocks.AsNoTracking().Where(x => x.AccountId == scope.AccountId)
            .OrderBy(x => x.Product.Name).ThenBy(x => x.ProductVariant != null ? x.ProductVariant.Name : x.Product.Name)
            .Select(x => new StockStatusReportDto(x.ProductId, x.ProductVariantId, x.ProductVariantId.HasValue ? x.Product.Name + " · " + x.ProductVariant!.Name : x.Product.Name, x.ProductVariantId.HasValue ? x.ProductVariant!.InternalCode : x.Product.InternalCode, x.Warehouse.Name, x.QuantityOnHand, x.Product.MinimumStock, x.QuantityOnHand <= x.Product.MinimumStock))
            .Take(100)
            .ToListAsync(ct);
        var customerBalance = await db.CustomerAccountMovements.AsNoTracking().Where(x => x.AccountId == scope.AccountId).SumAsync(x => x.DebitAmount - x.CreditAmount, ct);
        var supplierBalance = await db.SupplierAccountMovements.AsNoTracking().Where(x => x.AccountId == scope.AccountId).SumAsync(x => x.DebitAmount - x.CreditAmount, ct);
        var invoicesInPeriod = await db.CommercialInvoices.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IssuedAtUtc >= fromUtc && x.IssuedAtUtc < toUtc).SumAsync(x => x.Total, ct);
        var pendingDeliveryNotes = await db.DeliveryNotes.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.PendingQuantity > 0m).CountAsync(ct);

        var metrics = new List<ReportingMetricDto>
        {
            new("Ventas del período", sales.Sum(x => x.Total).ToString("C"), $"{sales.Count} documentos comerciales en el rango.", "primary"),
            new("Compras del período", purchases.Sum(x => x.Total).ToString("C"), $"{purchases.Count} comprobantes de compra emitidos.", "info"),
            new("Facturación emitida", invoicesInPeriod.ToString("C"), "Importe total de facturas creadas en el período.", "success"),
            new("Remitos pendientes", pendingDeliveryNotes.ToString(), "Ventas con entrega parcial o remitos con saldo pendiente.", pendingDeliveryNotes > 0 ? "warning" : "success"),
            new("Saldo clientes", customerBalance.ToString("C"), "Saldo comercial acumulado en cuentas corrientes de clientes.", customerBalance > 0 ? "warning" : "neutral"),
            new("Saldo proveedores", supplierBalance.ToString("C"), "Saldo comercial acumulado en cuentas corrientes de proveedores.", supplierBalance > 0 ? "info" : "neutral")
        };

        var salesByDay = sales.GroupBy(x => x.IssuedAtUtc.Date)
            .OrderBy(x => x.Key)
            .Select(x => new PeriodAmountDto(x.Key, x.Sum(i => i.Total), x.Count()))
            .ToList();
        var purchasesByDay = purchases.GroupBy(x => x.IssuedAtUtc.Date)
            .OrderBy(x => x.Key)
            .Select(x => new PeriodAmountDto(x.Key, x.Sum(i => i.Total), x.Count()))
            .ToList();

        return AppResult<OperationalReportDto>.Ok(new OperationalReportDto(request.From, request.To, metrics, salesByDay, purchasesByDay, topProducts, stock, customerBalance, supplierBalance));
    }
}

public sealed class GetRelease6DashboardQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetRelease6DashboardQuery, AppResult<Release6DashboardDto>>
{
    public async Task<AppResult<Release6DashboardDto>> Handle(GetRelease6DashboardQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<Release6DashboardDto>.Fail(scope.ErrorCode, scope.Message);

        var salesVolume = await db.Sales.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.Status != SaleStatus.Cancelled).OrderByDescending(x => x.IssuedAtUtc).Take(30).SumAsync(x => x.Total, ct);
        var invoicedAmount = await db.CommercialInvoices.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.Status == InvoiceStatus.Authorized).OrderByDescending(x => x.IssuedAtUtc).Take(30).SumAsync(x => x.Total, ct);
        var pendingInvoices = await db.CommercialInvoices.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.Status != InvoiceStatus.Authorized && x.Status != InvoiceStatus.Cancelled).CountAsync(ct);
        var pendingDeliveryNotes = await db.DeliveryNotes.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.PendingQuantity > 0m && x.Status != DeliveryNoteStatus.Cancelled).CountAsync(ct);
        var activeCustomers = await db.Customers.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IsActive).CountAsync(ct);
        var activeSuppliers = await db.Suppliers.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.IsActive).CountAsync(ct);
        var criticalStock = await db.ProductWarehouseStocks.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.QuantityOnHand <= x.Product.MinimumStock).CountAsync(ct);

        var highlights = new List<ReportingMetricDto>
        {
            new("Ventas 30 días", salesVolume.ToString("C"), "Volumen reciente para seguimiento operativo.", "primary"),
            new("Facturado autorizado", invoicedAmount.ToString("C"), "Facturas con CAE o autorización mock aprobada.", "success"),
            new("Pendiente de facturar", pendingInvoices.ToString(), "Comprobantes comerciales todavía no autorizados fiscalmente.", pendingInvoices > 0 ? "warning" : "success"),
            new("Pendiente de remitir", pendingDeliveryNotes.ToString(), "Entregas comerciales todavía abiertas.", pendingDeliveryNotes > 0 ? "warning" : "success"),
            new("Stock crítico", criticalStock.ToString(), "SKUs en o por debajo del mínimo configurado.", criticalStock > 0 ? "danger" : "success")
        };

        return AppResult<Release6DashboardDto>.Ok(new Release6DashboardDto(salesVolume, invoicedAmount, pendingInvoices, pendingDeliveryNotes, activeCustomers, activeSuppliers, criticalStock, highlights));
    }
}

public sealed class GetDocumentTraceabilityQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetDocumentTraceabilityQuery, AppResult<DocumentTraceabilityDto>>
{
    public async Task<AppResult<DocumentTraceabilityDto>> Handle(GetDocumentTraceabilityQuery request, CancellationToken ct)
    {
        var scope = await Release6Helpers.RequireSalesAsync(access, ct);
        if (!scope.Success)
            return AppResult<DocumentTraceabilityDto>.Fail(scope.ErrorCode, scope.Message);

        var historyQuery = db.DocumentChangeLogs.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        var auditQuery = db.AuditLogs.AsNoTracking().Where(x => x.AccountId == scope.AccountId);

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            historyQuery = historyQuery.Where(x => x.EntityName == request.EntityName);
            auditQuery = auditQuery.Where(x => x.EntityName == request.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            historyQuery = historyQuery.Where(x => x.DocumentNumber.Contains(request.Search) || x.Summary.Contains(request.Search));
            auditQuery = auditQuery.Where(x => (x.Summary ?? string.Empty).Contains(request.Search));
        }

        var history = await historyQuery.OrderByDescending(x => x.ChangedAtUtc).Take(request.Take)
            .Select(x => new DocumentChangeLogDto(x.Id, x.EntityName, x.EntityId, x.DocumentNumber, x.Action, x.Summary, x.ChangedFields, x.RelatedDocumentNumber, x.UserName, x.ChangedAtUtc))
            .ToListAsync(ct);
        var audit = await auditQuery.OrderByDescending(x => x.CreatedAtUtc).Take(request.Take)
            .Select(x => new AccountAuditItemDto(x.CreatedAtUtc, x.EntityName, x.Action, x.Summary ?? string.Empty, x.UserName))
            .ToListAsync(ct);

        return AppResult<DocumentTraceabilityDto>.Ok(new DocumentTraceabilityDto(history, audit));
    }
}
