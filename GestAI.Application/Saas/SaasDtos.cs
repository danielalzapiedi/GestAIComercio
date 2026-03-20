using GestAI.Domain.Enums;

namespace GestAI.Application.Saas;

public sealed record ModuleAccessDto(SaasModule Module, bool Allowed);

public sealed record CurrentUserAccessDto(
    int? AccountId,
    InternalUserRole? Role,
    bool IsOwner,
    bool IsActive,
    bool IsPlatformAdmin,
    List<ModuleAccessDto> Modules);

public sealed record AccountSummaryDto(
    int AccountId,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    string PlanName,
    SaasPlanCode PlanCode,
    string PlanStatus,
    DateTime StartedAtUtc,
    int MaxProperties,
    int MaxUnits,
    int MaxUsers,
    bool IncludesReports,
    bool IncludesOperations,
    int CurrentProperties,
    int CurrentUnits,
    int CurrentUsers);

public sealed record UpdateAccountCommand(string Name, int? PlanDefinitionId) : MediatR.IRequest<GestAI.Application.Common.AppResult>;

public sealed record AccountUserListItemDto(
    string UserId,
    string DisplayName,
    string Email,
    bool IsActive,
    InternalUserRole Role,
    DateTime? LastLoginAtUtc,
    DateTime InvitedAtUtc);

public sealed record UpsertAccountUserCommand(
    string? UserId,
    string Nombre,
    string Apellido,
    string Email,
    bool IsActive,
    InternalUserRole Role,
    string? Password) : MediatR.IRequest<GestAI.Application.Common.AppResult<string>>;

public sealed record ToggleAccountUserStatusCommand(string UserId, bool IsActive) : MediatR.IRequest<GestAI.Application.Common.AppResult>;

public sealed record GetAccountUsersQuery : MediatR.IRequest<GestAI.Application.Common.AppResult<List<AccountUserListItemDto>>>;
public sealed record GetCurrentUserAccessQuery : MediatR.IRequest<GestAI.Application.Common.AppResult<CurrentUserAccessDto>>;
public sealed record GetAccountSummaryQuery : MediatR.IRequest<GestAI.Application.Common.AppResult<AccountSummaryDto>>;
public sealed record GetAccountAuditQuery(int Take = 30, string? EntityName = null, string? UserName = null, DateOnly? From = null, DateOnly? To = null) : MediatR.IRequest<GestAI.Application.Common.AppResult<List<AccountAuditItemDto>>>;

public sealed record AccountAuditItemDto(DateTime CreatedAtUtc, string EntityName, string Action, string Summary, string? UserName);
