namespace GestAI.Web.Dtos;

public enum InternalUserRole
{
    Owner = 0,
    Admin = 1,
    Reception = 2,
    Operations = 3
}

public enum SaasPlanCode
{
    Starter = 0,
    Pro = 1,
    Manager = 2
}

public enum SaasModule
{
    Dashboard = 0,
    Users = 1,
    Configuration = 2,
    AuditLog = 3,
    Plans = 4
}

public sealed record ModuleAccessDto(SaasModule Module, bool Allowed);
public sealed record CurrentUserAccessDto(int? AccountId, InternalUserRole? Role, bool IsOwner, bool IsActive, List<ModuleAccessDto> Modules);
public sealed record AccountSummaryDto(int AccountId, string Name, bool IsActive, DateTime CreatedAtUtc, string PlanName, SaasPlanCode PlanCode, string PlanStatus, DateTime StartedAtUtc, int MaxProperties, int MaxUnits, int MaxUsers, bool IncludesReports, bool IncludesOperations, int CurrentProperties, int CurrentUnits, int CurrentUsers);
public sealed record UpdateAccountCommand(string Name, int? PlanDefinitionId);
public sealed record AccountUserListItemDto(string UserId, string DisplayName, string Email, bool IsActive, InternalUserRole Role, DateTime? LastLoginAtUtc, DateTime InvitedAtUtc);

public sealed class UpsertAccountUserCommand
{
    public string? UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public InternalUserRole Role { get; set; }
    public string? Password { get; set; }
}

public sealed record AccountAuditItemDto(DateTime CreatedAtUtc, string EntityName, string Action, string Summary, string? UserName);
