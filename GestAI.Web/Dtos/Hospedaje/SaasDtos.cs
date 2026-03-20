namespace GestAI.Web.Dtos;

public enum InternalUserRole
{
    Owner = 0,
    Employee = 1
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
    Plans = 4,
    PlatformTenants = 5,
    Branches = 6,
    Warehouses = 7,
    Categories = 8,
    Products = 9,
    Customers = 10,
    Suppliers = 11,
    Quotes = 12,
    Sales = 13,
    Purchases = 14,
    Cash = 15
}

public sealed record ModuleAccessDto(SaasModule Module, bool Allowed);
public sealed record CurrentUserAccessDto(int? AccountId, InternalUserRole? Role, bool IsOwner, bool IsActive, bool IsPlatformAdmin, List<ModuleAccessDto> Modules);
public sealed record AccountSummaryDto(int AccountId, string Name, bool IsActive, DateTime CreatedAtUtc, string PlanName, SaasPlanCode PlanCode, string PlanStatus, DateTime StartedAtUtc, int MaxProperties, int MaxUnits, int MaxUsers, bool IncludesReports, bool IncludesOperations, int CurrentProperties, int CurrentUnits, int CurrentUsers);
public sealed record UpdateAccountCommand(string Name, int? PlanDefinitionId);
public sealed record AccountUserListItemDto(string UserId, string DisplayName, string Email, bool IsActive, InternalUserRole Role, List<SaasModule> AllowedModules, DateTime? LastLoginAtUtc, DateTime InvitedAtUtc);

public sealed class UpsertAccountUserCommand
{
    public string? UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public InternalUserRole Role { get; set; } = InternalUserRole.Employee;
    public List<SaasModule> AllowedModules { get; set; } = new();
    public string? Password { get; set; }
}

public sealed record AccountAuditItemDto(DateTime CreatedAtUtc, string EntityName, string Action, string Summary, string? UserName);
