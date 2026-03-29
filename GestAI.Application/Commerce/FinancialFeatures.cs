using FluentValidation;
using GestAI.Application.Abstractions;
using GestAI.Application.Common;
using GestAI.Domain.Entities.Commerce;
using GestAI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GestAI.Application.Commerce;

public sealed record GetCashDashboardQuery() : IRequest<AppResult<CashDashboardDto>>;
public sealed record OpenCashSessionCommand(decimal OpeningBalance, string? Note) : IRequest<AppResult<int>>;
public sealed record CloseCashSessionCommand(decimal ClosingBalanceDeclared, string? Note) : IRequest<AppResult>;
public sealed record CreateCashManualMovementCommand(CashMovementDirection Direction, decimal Amount, string Concept, DateTime OccurredAtUtc, string? Observations) : IRequest<AppResult<int>>;
public sealed record CreateCustomerCollectionCommand(int CustomerId, DateTime CollectedAtUtc, decimal Amount, PaymentMethod PaymentMethod, bool ImpactCash, string Concept, string? Observations, IReadOnlyList<AllocationInputDto> Allocations) : IRequest<AppResult<int>>;
public sealed record CreateSupplierPaymentCommand(int SupplierId, DateTime PaidAtUtc, decimal Amount, PaymentMethod PaymentMethod, bool ImpactCash, string Concept, string? Observations, IReadOnlyList<AllocationInputDto> Allocations) : IRequest<AppResult<int>>;
public sealed record GetCustomerCurrentAccountsQuery(string? Search = null, bool? OnlyWithBalance = null) : IRequest<AppResult<List<CurrentAccountListItemDto>>>;
public sealed record GetCustomerCurrentAccountByCustomerIdQuery(int CustomerId) : IRequest<AppResult<CurrentAccountSummaryDto>>;
public sealed record GetSupplierCurrentAccountsQuery(string? Search = null, bool? OnlyWithBalance = null) : IRequest<AppResult<List<CurrentAccountListItemDto>>>;
public sealed record GetSupplierCurrentAccountBySupplierIdQuery(int SupplierId) : IRequest<AppResult<CurrentAccountSummaryDto>>;

internal static class FinancialHelpers
{
    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static async Task<(bool Success, int AccountId, string ErrorCode, string Message)> RequireCashAsync(IUserAccessService access, CancellationToken ct)
        => await CommerceFeatureHelpers.RequireModuleAccessAsync(access, SaasModule.Cash, ct);

    public static bool SaleGeneratesDebt(SaleStatus status) => status is SaleStatus.Confirmed or SaleStatus.Completed;
    public static bool PurchaseGeneratesDebt(PurchaseDocumentStatus status) => status is PurchaseDocumentStatus.Issued or PurchaseDocumentStatus.PartiallyReceived or PurchaseDocumentStatus.Received;

    public static string BuildCashReference(int id) => $"CJ-{id:D6}";
    public static string BuildCollectionReference(int id) => $"COB-{id:D6}";
    public static string BuildPaymentReference(int id) => $"PAG-{id:D6}";

    public static async Task<CashRegister> EnsureDefaultCashRegisterAsync(IAppDbContext db, int accountId, ICurrentUser current, CancellationToken ct)
    {
        var register = await db.CashRegisters.FirstOrDefaultAsync(x => x.AccountId == accountId && x.IsDefault, ct);
        if (register is not null) return register;

        var branch = await db.Branches.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive).OrderBy(x => x.Id).Select(x => new { x.Id }).FirstOrDefaultAsync(ct);
        register = new CashRegister
        {
            AccountId = accountId,
            BranchId = branch?.Id,
            Name = "Caja principal",
            Code = "CAJA-01",
            IsDefault = true,
            IsActive = true
        };
        CommerceFeatureHelpers.TouchCreate(register, current);
        db.CashRegisters.Add(register);
        await db.SaveChangesAsync(ct);
        return register;
    }

    public static async Task<CashSession?> GetOpenSessionAsync(IAppDbContext db, int accountId, int registerId, CancellationToken ct)
        => await db.CashSessions.Include(x => x.Movements)
            .FirstOrDefaultAsync(x => x.AccountId == accountId && x.CashRegisterId == registerId && x.Status == CashSessionStatus.Open, ct);

    public static decimal ComputeSessionBalance(CashSession session)
        => RoundMoney(session.Movements.Sum(x => x.Direction == CashMovementDirection.In ? x.Amount : -x.Amount));

    public static async Task<CashMovement> CreateCashMovementAsync(
        IAppDbContext db,
        int accountId,
        CashRegister register,
        CashSession session,
        CashMovementDirection direction,
        CashMovementOriginType originType,
        PaymentMethod paymentMethod,
        decimal amount,
        string concept,
        DateTime occurredAtUtc,
        string? observations,
        ICurrentUser current,
        int? customerId = null,
        int? supplierId = null,
        CancellationToken ct = default)
    {
        var movement = new CashMovement
        {
            AccountId = accountId,
            CashRegisterId = register.Id,
            CashSessionId = session.Id,
            Direction = direction,
            OriginType = originType,
            PaymentMethod = paymentMethod,
            Amount = RoundMoney(amount),
            Concept = concept.Trim(),
            Observations = string.IsNullOrWhiteSpace(observations) ? null : observations.Trim(),
            OccurredAtUtc = occurredAtUtc,
            CustomerId = customerId,
            SupplierId = supplierId,
            ReferenceNumber = "TEMP"
        };
        CommerceFeatureHelpers.TouchCreate(movement, current);
        db.CashMovements.Add(movement);
        await db.SaveChangesAsync(ct);
        movement.ReferenceNumber = BuildCashReference(movement.Id);
        return movement;
    }

    public static async Task UpsertCustomerMovementAsync(IAppDbContext db, Sale sale, ICurrentUser current, CancellationToken ct)
    {
        var movement = await db.CustomerAccountMovements.FirstOrDefaultAsync(x => x.AccountId == sale.AccountId && x.SaleId == sale.Id, ct);
        var debit = SaleGeneratesDebt(sale.Status) ? RoundMoney(sale.Total) : 0m;
        if (movement is null)
        {
            movement = new CustomerAccountMovement
            {
                AccountId = sale.AccountId,
                CustomerId = sale.CustomerId,
                SaleId = sale.Id,
                MovementType = CustomerAccountMovementType.SaleDocument,
                PaymentMethod = PaymentMethod.AccountCredit,
                ReferenceNumber = sale.Number,
                IssuedAtUtc = sale.IssuedAtUtc,
                DebitAmount = debit,
                CreditAmount = 0m,
                Description = $"Venta {sale.Number}",
                Note = sale.Status == SaleStatus.Cancelled ? "Comprobante cancelado: impacto en saldo anulado." : sale.Observations
            };
            CommerceFeatureHelpers.TouchCreate(movement, current);
            db.CustomerAccountMovements.Add(movement);
            return;
        }

        movement.CustomerId = sale.CustomerId;
        movement.ReferenceNumber = sale.Number;
        movement.IssuedAtUtc = sale.IssuedAtUtc;
        movement.DebitAmount = debit;
        movement.CreditAmount = 0m;
        movement.Description = $"Venta {sale.Number}";
        movement.Note = sale.Status == SaleStatus.Cancelled ? "Comprobante cancelado: impacto en saldo anulado." : sale.Observations;
        movement.PaymentMethod = PaymentMethod.AccountCredit;
        CommerceFeatureHelpers.TouchUpdate(movement, current);
    }

    public static decimal PendingForDebitMovement(int movementId, decimal debitAmount, IEnumerable<(int TargetMovementId, decimal Amount)> allocations)
        => RoundMoney(Math.Max(0m, debitAmount - allocations.Where(x => x.TargetMovementId == movementId).Sum(x => x.Amount)));

    public static decimal PendingForCreditMovement(int movementId, decimal creditAmount, IEnumerable<(int SourceMovementId, decimal Amount)> allocations)
        => RoundMoney(Math.Max(0m, creditAmount - allocations.Where(x => x.SourceMovementId == movementId).Sum(x => x.Amount)));
}

public sealed class OpenCashSessionCommandValidator : AbstractValidator<OpenCashSessionCommand>
{
    public OpenCashSessionCommandValidator() => RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
}

public sealed class CloseCashSessionCommandValidator : AbstractValidator<CloseCashSessionCommand>
{
    public CloseCashSessionCommandValidator() => RuleFor(x => x.ClosingBalanceDeclared).GreaterThanOrEqualTo(0);
}

public sealed class CreateCashManualMovementCommandValidator : AbstractValidator<CreateCashManualMovementCommand>
{
    public CreateCashManualMovementCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Concept).NotEmpty().MaximumLength(250);
    }
}

public sealed class CreateCustomerCollectionCommandValidator : AbstractValidator<CreateCustomerCollectionCommand>
{
    public CreateCustomerCollectionCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Concept).NotEmpty().MaximumLength(250);
        RuleForEach(x => x.Allocations).ChildRules(item =>
        {
            item.RuleFor(x => x.DocumentId).GreaterThan(0);
            item.RuleFor(x => x.Amount).GreaterThan(0);
        });
    }
}

public sealed class CreateSupplierPaymentCommandValidator : AbstractValidator<CreateSupplierPaymentCommand>
{
    public CreateSupplierPaymentCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Concept).NotEmpty().MaximumLength(250);
        RuleForEach(x => x.Allocations).ChildRules(item =>
        {
            item.RuleFor(x => x.DocumentId).GreaterThan(0);
            item.RuleFor(x => x.Amount).GreaterThan(0);
        });
    }
}

public sealed class OpenCashSessionCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<OpenCashSessionCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(OpenCashSessionCommand request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        var register = await FinancialHelpers.EnsureDefaultCashRegisterAsync(db, scope.AccountId, current, ct);
        if (await db.CashSessions.AnyAsync(x => x.AccountId == scope.AccountId && x.CashRegisterId == register.Id && x.Status == CashSessionStatus.Open, ct))
            return AppResult<int>.Fail("cash_session_open", "Ya existe una caja abierta para la caja principal.");

        var session = new CashSession
        {
            AccountId = scope.AccountId,
            CashRegisterId = register.Id,
            Status = CashSessionStatus.Open,
            OpenedAtUtc = DateTime.UtcNow,
            OpenedByUserId = current.UserId,
            OpeningBalance = FinancialHelpers.RoundMoney(request.OpeningBalance),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };
        CommerceFeatureHelpers.TouchCreate(session, current);
        db.CashSessions.Add(session);
        await db.SaveChangesAsync(ct);

        if (session.OpeningBalance > 0m)
        {
            await FinancialHelpers.CreateCashMovementAsync(db, scope.AccountId, register, session, CashMovementDirection.In, CashMovementOriginType.Opening, PaymentMethod.Cash, session.OpeningBalance, "Apertura de caja", session.OpenedAtUtc, request.Note, current, ct: ct);
            await db.SaveChangesAsync(ct);
        }

        await audit.WriteAsync(scope.AccountId, null, nameof(CashSession), session.Id, "opened", $"Caja abierta con saldo inicial {session.OpeningBalance:C}.", ct);
        return AppResult<int>.Ok(session.Id);
    }
}

public sealed class CloseCashSessionCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CloseCashSessionCommand, AppResult>
{
    public async Task<AppResult> Handle(CloseCashSessionCommand request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult.Fail(scope.ErrorCode, scope.Message);

        var register = await FinancialHelpers.EnsureDefaultCashRegisterAsync(db, scope.AccountId, current, ct);
        var session = await FinancialHelpers.GetOpenSessionAsync(db, scope.AccountId, register.Id, ct);
        if (session is null) return AppResult.Fail("cash_session_required", "No hay una caja abierta para cerrar.");

        var expected = FinancialHelpers.ComputeSessionBalance(session);
        var declared = FinancialHelpers.RoundMoney(request.ClosingBalanceDeclared);
        var delta = FinancialHelpers.RoundMoney(declared - expected);
        if (delta != 0m)
        {
            await FinancialHelpers.CreateCashMovementAsync(db, scope.AccountId, register, session, delta > 0m ? CashMovementDirection.In : CashMovementDirection.Out, CashMovementOriginType.ClosingAdjustment, PaymentMethod.Cash, Math.Abs(delta), "Ajuste de cierre", DateTime.UtcNow, request.Note, current, ct: ct);
            await db.SaveChangesAsync(ct);
            session.Movements = await db.CashMovements.Where(x => x.CashSessionId == session.Id).ToListAsync(ct);
            expected = FinancialHelpers.ComputeSessionBalance(session);
        }

        session.Status = CashSessionStatus.Closed;
        session.ClosedAtUtc = DateTime.UtcNow;
        session.ClosedByUserId = current.UserId;
        session.ClosingBalanceExpected = expected;
        session.ClosingBalanceDeclared = declared;
        session.Note = string.IsNullOrWhiteSpace(request.Note) ? session.Note : request.Note.Trim();
        CommerceFeatureHelpers.TouchUpdate(session, current);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(CashSession), session.Id, "closed", $"Caja cerrada con saldo declarado {declared:C}.", ct);
        return AppResult.Ok();
    }
}

public sealed class CreateCashManualMovementCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateCashManualMovementCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateCashManualMovementCommand request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);

        var register = await FinancialHelpers.EnsureDefaultCashRegisterAsync(db, scope.AccountId, current, ct);
        var session = await FinancialHelpers.GetOpenSessionAsync(db, scope.AccountId, register.Id, ct);
        if (session is null) return AppResult<int>.Fail("cash_session_required", "Abrí una caja antes de registrar movimientos.");

        var movement = await FinancialHelpers.CreateCashMovementAsync(db, scope.AccountId, register, session, request.Direction, CashMovementOriginType.Manual, PaymentMethod.Cash, request.Amount, request.Concept, request.OccurredAtUtc, request.Observations, current, ct: ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(CashMovement), movement.Id, "created", $"Movimiento de caja {movement.ReferenceNumber} registrado por {movement.Amount:C}.", ct);
        return AppResult<int>.Ok(movement.Id);
    }
}

public sealed class CreateCustomerCollectionCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateCustomerCollectionCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateCustomerCollectionCommand request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        if (!await db.Customers.AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.CustomerId, ct))
            return AppResult<int>.Fail("customer_not_found", "Cliente no encontrado.");

        var allocationTotal = FinancialHelpers.RoundMoney(request.Allocations.Sum(x => x.Amount));
        if (allocationTotal > FinancialHelpers.RoundMoney(request.Amount))
            return AppResult<int>.Fail("invalid_allocations", "Las imputaciones superan el importe cobrado.");

        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        CashMovement? cashMovement = null;
        if (request.ImpactCash)
        {
            var register = await FinancialHelpers.EnsureDefaultCashRegisterAsync(db, scope.AccountId, current, ct);
            var session = await FinancialHelpers.GetOpenSessionAsync(db, scope.AccountId, register.Id, ct);
            if (session is null) return AppResult<int>.Fail("cash_session_required", "Abrí una caja antes de registrar cobros que impacten caja.");
            cashMovement = await FinancialHelpers.CreateCashMovementAsync(db, scope.AccountId, register, session, CashMovementDirection.In, CashMovementOriginType.CustomerCollection, request.PaymentMethod, request.Amount, request.Concept, request.CollectedAtUtc, request.Observations, current, customerId: request.CustomerId, ct: ct);
            await db.SaveChangesAsync(ct);
        }

        var movement = new CustomerAccountMovement
        {
            AccountId = scope.AccountId,
            CustomerId = request.CustomerId,
            MovementType = CustomerAccountMovementType.Collection,
            PaymentMethod = request.PaymentMethod,
            CashMovementId = cashMovement?.Id,
            ReferenceNumber = "TEMP",
            IssuedAtUtc = request.CollectedAtUtc,
            DebitAmount = 0m,
            CreditAmount = FinancialHelpers.RoundMoney(request.Amount),
            Description = request.Concept.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim()
        };
        CommerceFeatureHelpers.TouchCreate(movement, current);
        db.CustomerAccountMovements.Add(movement);
        await db.SaveChangesAsync(ct);
        movement.ReferenceNumber = FinancialHelpers.BuildCollectionReference(movement.Id);

        var openDocs = await db.CustomerAccountMovements
            .Where(x => x.AccountId == scope.AccountId && x.CustomerId == request.CustomerId && x.SaleId.HasValue)
            .ToDictionaryAsync(x => x.SaleId!.Value, x => x, ct);
        var openDocMovementIds = openDocs.Values.Select(m => m.Id).ToList();
        var existingAllocations = await db.CustomerAccountAllocations
            .Where(x => x.AccountId == scope.AccountId && openDocMovementIds.Contains(x.TargetMovementId))
            .ToListAsync(ct);

        foreach (var allocation in request.Allocations)
        {
            if (!openDocs.TryGetValue(allocation.DocumentId, out var target))
                return AppResult<int>.Fail("document_not_found", $"La venta {allocation.DocumentId} no existe o no impacta cuenta corriente.");

            var pending = FinancialHelpers.PendingForDebitMovement(target.Id, target.DebitAmount, existingAllocations.Select(x => (x.TargetMovementId, x.Amount)));
            if (allocation.Amount > pending)
                return AppResult<int>.Fail("allocation_exceeds_pending", $"La imputación supera el saldo pendiente del comprobante {target.ReferenceNumber}.");

            var entity = new CustomerAccountAllocation
            {
                AccountId = scope.AccountId,
                SourceMovementId = movement.Id,
                TargetMovementId = target.Id,
                AppliedAtUtc = request.CollectedAtUtc,
                Amount = FinancialHelpers.RoundMoney(allocation.Amount),
                Note = string.IsNullOrWhiteSpace(allocation.Note) ? null : allocation.Note.Trim()
            };
            CommerceFeatureHelpers.TouchCreate(entity, current);
            db.CustomerAccountAllocations.Add(entity);
            existingAllocations.Add(entity);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(CustomerAccountMovement), movement.Id, "created", $"Cobro {movement.ReferenceNumber} registrado por {movement.CreditAmount:C}.", ct);
        return AppResult<int>.Ok(movement.Id);
    }
}

public sealed class CreateSupplierPaymentCommandHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current, IAuditService audit)
    : IRequestHandler<CreateSupplierPaymentCommand, AppResult<int>>
{
    public async Task<AppResult<int>> Handle(CreateSupplierPaymentCommand request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<int>.Fail(scope.ErrorCode, scope.Message);
        if (!await db.Suppliers.AnyAsync(x => x.AccountId == scope.AccountId && x.Id == request.SupplierId, ct))
            return AppResult<int>.Fail("supplier_not_found", "Proveedor no encontrado.");

        var allocationTotal = FinancialHelpers.RoundMoney(request.Allocations.Sum(x => x.Amount));
        if (allocationTotal > FinancialHelpers.RoundMoney(request.Amount))
            return AppResult<int>.Fail("invalid_allocations", "Las imputaciones superan el importe pagado.");

        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        CashMovement? cashMovement = null;
        if (request.ImpactCash)
        {
            var register = await FinancialHelpers.EnsureDefaultCashRegisterAsync(db, scope.AccountId, current, ct);
            var session = await FinancialHelpers.GetOpenSessionAsync(db, scope.AccountId, register.Id, ct);
            if (session is null) return AppResult<int>.Fail("cash_session_required", "Abrí una caja antes de registrar pagos que impacten caja.");
            cashMovement = await FinancialHelpers.CreateCashMovementAsync(db, scope.AccountId, register, session, CashMovementDirection.Out, CashMovementOriginType.SupplierPayment, request.PaymentMethod, request.Amount, request.Concept, request.PaidAtUtc, request.Observations, current, supplierId: request.SupplierId, ct: ct);
            await db.SaveChangesAsync(ct);
        }

        var movement = new SupplierAccountMovement
        {
            AccountId = scope.AccountId,
            SupplierId = request.SupplierId,
            MovementType = SupplierAccountMovementType.Payment,
            PaymentMethod = request.PaymentMethod,
            CashMovementId = cashMovement?.Id,
            ReferenceNumber = "TEMP",
            IssuedAtUtc = request.PaidAtUtc,
            DebitAmount = 0m,
            CreditAmount = FinancialHelpers.RoundMoney(request.Amount),
            Description = request.Concept.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Observations) ? null : request.Observations.Trim()
        };
        CommerceFeatureHelpers.TouchCreate(movement, current);
        db.SupplierAccountMovements.Add(movement);
        await db.SaveChangesAsync(ct);
        movement.ReferenceNumber = FinancialHelpers.BuildPaymentReference(movement.Id);

        var documents = await db.SupplierAccountMovements
            .Where(x => x.AccountId == scope.AccountId && x.SupplierId == request.SupplierId && x.PurchaseDocumentId.HasValue)
            .ToDictionaryAsync(x => x.PurchaseDocumentId!.Value, x => x, ct);
        var allocations = await db.SupplierAccountAllocations.Where(x => x.AccountId == scope.AccountId).ToListAsync(ct);

        foreach (var allocation in request.Allocations)
        {
            if (!documents.TryGetValue(allocation.DocumentId, out var target))
                return AppResult<int>.Fail("document_not_found", $"La compra {allocation.DocumentId} no existe o no impacta cuenta corriente.");

            var pending = FinancialHelpers.PendingForDebitMovement(target.Id, target.DebitAmount, allocations.Select(x => (x.TargetMovementId, x.Amount)));
            if (allocation.Amount > pending)
                return AppResult<int>.Fail("allocation_exceeds_pending", $"La imputación supera el saldo pendiente del comprobante {target.ReferenceNumber}.");

            var entity = new SupplierAccountAllocation
            {
                AccountId = scope.AccountId,
                SourceMovementId = movement.Id,
                TargetMovementId = target.Id,
                AppliedAtUtc = request.PaidAtUtc,
                Amount = FinancialHelpers.RoundMoney(allocation.Amount),
                Note = string.IsNullOrWhiteSpace(allocation.Note) ? null : allocation.Note.Trim()
            };
            CommerceFeatureHelpers.TouchCreate(entity, current);
            db.SupplierAccountAllocations.Add(entity);
            allocations.Add(entity);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await audit.WriteAsync(scope.AccountId, null, nameof(SupplierAccountMovement), movement.Id, "created", $"Pago {movement.ReferenceNumber} registrado por {movement.CreditAmount:C}.", ct);
        return AppResult<int>.Ok(movement.Id);
    }
}

public sealed class GetCashDashboardQueryHandler(IAppDbContext db, IUserAccessService access, ICurrentUser current)
    : IRequestHandler<GetCashDashboardQuery, AppResult<CashDashboardDto>>
{
    public async Task<AppResult<CashDashboardDto>> Handle(GetCashDashboardQuery request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<CashDashboardDto>.Fail(scope.ErrorCode, scope.Message);

        var register = await FinancialHelpers.EnsureDefaultCashRegisterAsync(db, scope.AccountId, current, ct);
        var session = await db.CashSessions.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId && x.CashRegisterId == register.Id && x.Status == CashSessionStatus.Open)
            .OrderByDescending(x => x.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        var movements = await db.CashMovements.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId && x.CashRegisterId == register.Id)
            .OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Take(30)
            .Select(x => new CashMovementListItemDto(x.Id, x.Direction, x.OriginType, x.PaymentMethod, x.ReferenceNumber, x.OccurredAtUtc, x.Amount, x.Concept, x.Observations, x.CustomerId, x.Customer != null ? x.Customer.Name : null, x.SupplierId, x.Supplier != null ? x.Supplier.Name : null))
            .ToListAsync(ct);

        var totals = await db.CashMovements.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId && x.CashRegisterId == register.Id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalIn = g.Where(x => x.Direction == CashMovementDirection.In).Sum(x => x.Amount),
                TotalOut = g.Where(x => x.Direction == CashMovementDirection.Out).Sum(x => x.Amount)
            })
            .FirstOrDefaultAsync(ct);

        var totalIn = FinancialHelpers.RoundMoney(totals?.TotalIn ?? 0m);
        var totalOut = FinancialHelpers.RoundMoney(totals?.TotalOut ?? 0m);
        var currentBalance = FinancialHelpers.RoundMoney(totalIn - totalOut);

        CashSessionSummaryDto? openSession = null;
        if (session is not null)
        {
            var sessionTotals = await db.CashMovements.AsNoTracking()
                .Where(x => x.AccountId == scope.AccountId && x.CashRegisterId == register.Id && x.CashSessionId == session.Id)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    SessionIn = g.Where(x => x.Direction == CashMovementDirection.In).Sum(x => x.Amount),
                    SessionOut = g.Where(x => x.Direction == CashMovementDirection.Out).Sum(x => x.Amount)
                })
                .FirstOrDefaultAsync(ct);

            var sessionIn = FinancialHelpers.RoundMoney(sessionTotals?.SessionIn ?? 0m);
            var sessionOut = FinancialHelpers.RoundMoney(sessionTotals?.SessionOut ?? 0m);
            openSession = new CashSessionSummaryDto(session.Id, session.Status, session.OpenedAtUtc, session.OpenedByUserId, session.OpeningBalance, FinancialHelpers.RoundMoney(sessionIn - sessionOut), sessionIn, sessionOut, session.ClosedAtUtc, session.ClosingBalanceExpected, session.ClosingBalanceDeclared, session.Note);
        }

        return AppResult<CashDashboardDto>.Ok(new CashDashboardDto(
            new CashRegisterSummaryDto(register.Id, register.Name, register.Code, register.IsDefault, register.IsActive, register.BranchId, null),
            openSession,
            currentBalance,
            totalIn,
            totalOut,
            movements));
    }
}

public sealed class GetCustomerCurrentAccountsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCustomerCurrentAccountsQuery, AppResult<List<CurrentAccountListItemDto>>>
{
    public async Task<AppResult<List<CurrentAccountListItemDto>>> Handle(GetCustomerCurrentAccountsQuery request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<List<CurrentAccountListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var customers = db.Customers.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            customers = customers.Where(x => x.Name.Contains(search) || (x.DocumentNumber ?? string.Empty).Contains(search));
        }

        var customerList = await customers.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.DocumentNumber, x.IsActive }).ToListAsync(ct);
        var movementStats = await db.CustomerAccountMovements.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId)
            .GroupBy(x => x.CustomerId)
            .Select(g => new { CustomerId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount), LastMovementAtUtc = g.Max(x => (DateTime?)x.IssuedAtUtc) })
            .ToListAsync(ct);
        var allocations = await db.CustomerAccountAllocations.AsNoTracking().Where(x => x.AccountId == scope.AccountId).ToListAsync(ct);
        var debitMovements = await db.CustomerAccountMovements.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.SaleId.HasValue).ToListAsync(ct);

        var items = customerList.Select(customer =>
        {
            var stat = movementStats.FirstOrDefault(x => x.CustomerId == customer.Id);
            var docs = debitMovements.Where(x => x.CustomerId == customer.Id).ToList();
            var openDocuments = docs.Count(x => FinancialHelpers.PendingForDebitMovement(x.Id, x.DebitAmount, allocations.Select(a => (a.TargetMovementId, a.Amount))) > 0m);
            var balance = FinancialHelpers.RoundMoney((stat?.Debit ?? 0m) - (stat?.Credit ?? 0m));
            return new CurrentAccountListItemDto(customer.Id, customer.Name, customer.DocumentNumber, customer.IsActive, balance, stat?.Debit ?? 0m, stat?.Credit ?? 0m, openDocuments, stat?.LastMovementAtUtc);
        }).ToList();

        if (request.OnlyWithBalance == true)
            items = items.Where(x => x.Balance > 0m || x.OpenDocumentsCount > 0).ToList();

        return AppResult<List<CurrentAccountListItemDto>>.Ok(items.OrderByDescending(x => x.Balance).ThenBy(x => x.PartyName).ToList());
    }
}

public sealed class GetCustomerCurrentAccountByCustomerIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetCustomerCurrentAccountByCustomerIdQuery, AppResult<CurrentAccountSummaryDto>>
{
    public async Task<AppResult<CurrentAccountSummaryDto>> Handle(GetCustomerCurrentAccountByCustomerIdQuery request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<CurrentAccountSummaryDto>.Fail(scope.ErrorCode, scope.Message);

        var customer = await db.Customers.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.Id == request.CustomerId)
            .Select(x => new { x.Id, x.Name, x.DocumentNumber })
            .FirstOrDefaultAsync(ct);
        if (customer is null) return AppResult<CurrentAccountSummaryDto>.Fail("not_found", "Cliente no encontrado.");

        var movements = await db.CustomerAccountMovements.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId && x.CustomerId == request.CustomerId)
            .OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id)
            .ToListAsync(ct);
        var movementIds = movements.Select(m => m.Id).ToList();
        var allocations = await db.CustomerAccountAllocations.AsNoTracking().Where(x => x.AccountId == scope.AccountId && (movementIds.Contains(x.SourceMovementId) || movementIds.Contains(x.TargetMovementId))).ToListAsync(ct);

        var movementDtos = movements.Select(m =>
        {
            var applied = m.DebitAmount > 0m
                ? allocations.Where(x => x.TargetMovementId == m.Id).Sum(x => x.Amount)
                : allocations.Where(x => x.SourceMovementId == m.Id).Sum(x => x.Amount);
            var pending = m.DebitAmount > 0m
                ? FinancialHelpers.RoundMoney(Math.Max(0m, m.DebitAmount - applied))
                : FinancialHelpers.RoundMoney(Math.Max(0m, m.CreditAmount - applied));
            return new CurrentAccountMovementDto(m.Id, m.MovementType.ToString(), m.PaymentMethod, m.ReferenceNumber, m.Description, m.IssuedAtUtc, m.DebitAmount, m.CreditAmount, FinancialHelpers.RoundMoney(applied), pending, m.DebitAmount - m.CreditAmount, m.SaleId, m.Note);
        }).ToList();

        var pendingDocuments = movementDtos.Where(x => x.DebitAmount > 0m && x.PendingAmount > 0m)
            .Select(x => new PendingDocumentDto(x.DocumentId ?? 0, x.ReferenceNumber, x.IssuedAtUtc, x.DebitAmount, x.AppliedAmount, x.PendingAmount, x.Description))
            .OrderBy(x => x.IssuedAtUtc)
            .ToList();

        var recentAllocations = allocations.OrderByDescending(x => x.AppliedAtUtc).ThenByDescending(x => x.Id).Take(20)
            .Select(x => new CurrentAccountAllocationDto(x.Id, x.AppliedAtUtc, x.Amount, movements.First(m => m.Id == x.SourceMovementId).ReferenceNumber, movements.First(m => m.Id == x.TargetMovementId).ReferenceNumber, x.Note))
            .ToList();

        var debit = FinancialHelpers.RoundMoney(movements.Sum(x => x.DebitAmount));
        var credit = FinancialHelpers.RoundMoney(movements.Sum(x => x.CreditAmount));
        return AppResult<CurrentAccountSummaryDto>.Ok(new CurrentAccountSummaryDto(customer.Id, customer.Name, customer.DocumentNumber, debit - credit, debit, credit, pendingDocuments.Sum(x => x.PendingAmount), pendingDocuments.Count, movements.FirstOrDefault()?.IssuedAtUtc, movementDtos, pendingDocuments, recentAllocations));
    }
}

public sealed class GetSupplierCurrentAccountsQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSupplierCurrentAccountsQuery, AppResult<List<CurrentAccountListItemDto>>>
{
    public async Task<AppResult<List<CurrentAccountListItemDto>>> Handle(GetSupplierCurrentAccountsQuery request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<List<CurrentAccountListItemDto>>.Fail(scope.ErrorCode, scope.Message);

        var suppliers = db.Suppliers.AsNoTracking().Where(x => x.AccountId == scope.AccountId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            suppliers = suppliers.Where(x => x.Name.Contains(search) || x.TaxId.Contains(search));
        }

        var supplierList = await suppliers.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.TaxId, x.IsActive }).ToListAsync(ct);
        var movementStats = await db.SupplierAccountMovements.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId)
            .GroupBy(x => x.SupplierId)
            .Select(g => new { SupplierId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount), LastMovementAtUtc = g.Max(x => (DateTime?)x.IssuedAtUtc) })
            .ToListAsync(ct);
        var allocations = await db.SupplierAccountAllocations.AsNoTracking().Where(x => x.AccountId == scope.AccountId).ToListAsync(ct);
        var debitMovements = await db.SupplierAccountMovements.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.PurchaseDocumentId.HasValue).ToListAsync(ct);

        var items = supplierList.Select(supplier =>
        {
            var stat = movementStats.FirstOrDefault(x => x.SupplierId == supplier.Id);
            var docs = debitMovements.Where(x => x.SupplierId == supplier.Id).ToList();
            var openDocuments = docs.Count(x => FinancialHelpers.PendingForDebitMovement(x.Id, x.DebitAmount, allocations.Select(a => (a.TargetMovementId, a.Amount))) > 0m);
            var balance = FinancialHelpers.RoundMoney((stat?.Debit ?? 0m) - (stat?.Credit ?? 0m));
            return new CurrentAccountListItemDto(supplier.Id, supplier.Name, supplier.TaxId, supplier.IsActive, balance, stat?.Debit ?? 0m, stat?.Credit ?? 0m, openDocuments, stat?.LastMovementAtUtc);
        }).ToList();

        if (request.OnlyWithBalance == true)
            items = items.Where(x => x.Balance > 0m || x.OpenDocumentsCount > 0).ToList();

        return AppResult<List<CurrentAccountListItemDto>>.Ok(items.OrderByDescending(x => x.Balance).ThenBy(x => x.PartyName).ToList());
    }
}

public sealed class GetSupplierCurrentAccountBySupplierIdQueryHandler(IAppDbContext db, IUserAccessService access)
    : IRequestHandler<GetSupplierCurrentAccountBySupplierIdQuery, AppResult<CurrentAccountSummaryDto>>
{
    public async Task<AppResult<CurrentAccountSummaryDto>> Handle(GetSupplierCurrentAccountBySupplierIdQuery request, CancellationToken ct)
    {
        var scope = await FinancialHelpers.RequireCashAsync(access, ct);
        if (!scope.Success) return AppResult<CurrentAccountSummaryDto>.Fail(scope.ErrorCode, scope.Message);

        var supplier = await db.Suppliers.AsNoTracking().Where(x => x.AccountId == scope.AccountId && x.Id == request.SupplierId)
            .Select(x => new { x.Id, x.Name, x.TaxId })
            .FirstOrDefaultAsync(ct);
        if (supplier is null) return AppResult<CurrentAccountSummaryDto>.Fail("not_found", "Proveedor no encontrado.");

        var movements = await db.SupplierAccountMovements.AsNoTracking()
            .Where(x => x.AccountId == scope.AccountId && x.SupplierId == request.SupplierId)
            .OrderByDescending(x => x.IssuedAtUtc).ThenByDescending(x => x.Id)
            .ToListAsync(ct);
        var movementIds = movements.Select(m => m.Id).ToList();
        var allocations = await db.SupplierAccountAllocations.AsNoTracking().Where(x => x.AccountId == scope.AccountId && (movementIds.Contains(x.SourceMovementId) || movementIds.Contains(x.TargetMovementId))).ToListAsync(ct);

        var movementDtos = movements.Select(m =>
        {
            var applied = m.DebitAmount > 0m
                ? allocations.Where(x => x.TargetMovementId == m.Id).Sum(x => x.Amount)
                : allocations.Where(x => x.SourceMovementId == m.Id).Sum(x => x.Amount);
            var pending = m.DebitAmount > 0m
                ? FinancialHelpers.RoundMoney(Math.Max(0m, m.DebitAmount - applied))
                : FinancialHelpers.RoundMoney(Math.Max(0m, m.CreditAmount - applied));
            return new CurrentAccountMovementDto(m.Id, m.MovementType.ToString(), m.PaymentMethod, m.ReferenceNumber, m.Description, m.IssuedAtUtc, m.DebitAmount, m.CreditAmount, FinancialHelpers.RoundMoney(applied), pending, m.DebitAmount - m.CreditAmount, m.PurchaseDocumentId, m.Note);
        }).ToList();

        var pendingDocuments = movementDtos.Where(x => x.DebitAmount > 0m && x.PendingAmount > 0m)
            .Select(x => new PendingDocumentDto(x.DocumentId ?? 0, x.ReferenceNumber, x.IssuedAtUtc, x.DebitAmount, x.AppliedAmount, x.PendingAmount, x.Description))
            .OrderBy(x => x.IssuedAtUtc)
            .ToList();

        var recentAllocations = allocations.OrderByDescending(x => x.AppliedAtUtc).ThenByDescending(x => x.Id).Take(20)
            .Select(x => new CurrentAccountAllocationDto(x.Id, x.AppliedAtUtc, x.Amount, movements.First(m => m.Id == x.SourceMovementId).ReferenceNumber, movements.First(m => m.Id == x.TargetMovementId).ReferenceNumber, x.Note))
            .ToList();

        var debit = FinancialHelpers.RoundMoney(movements.Sum(x => x.DebitAmount));
        var credit = FinancialHelpers.RoundMoney(movements.Sum(x => x.CreditAmount));
        return AppResult<CurrentAccountSummaryDto>.Ok(new CurrentAccountSummaryDto(supplier.Id, supplier.Name, supplier.TaxId, debit - credit, debit, credit, pendingDocuments.Sum(x => x.PendingAmount), pendingDocuments.Count, movements.FirstOrDefault()?.IssuedAtUtc, movementDtos, pendingDocuments, recentAllocations));
    }
}
