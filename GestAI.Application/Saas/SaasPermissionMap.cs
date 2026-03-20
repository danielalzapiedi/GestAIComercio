using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Application.Saas;

public static class SaasPermissionMap
{
    private static readonly SaasModule[] DefaultEmployeeModules =
    {
        SaasModule.Dashboard,
        SaasModule.Branches,
        SaasModule.Warehouses,
        SaasModule.Categories,
        SaasModule.Products,
        SaasModule.Customers,
        SaasModule.Suppliers,
        SaasModule.Quotes,
        SaasModule.Sales,
        SaasModule.Purchases,
        SaasModule.Cash
    };

    public static bool HasAccess(InternalUserRole? role, SaasPlanDefinition? plan, SaasModule module, bool isOwner, bool isPlatformAdmin = false, IReadOnlyCollection<SaasModule>? assignedModules = null)
    {
        var modules = GetEffectiveModules(role, isOwner, isPlatformAdmin, assignedModules);
        return modules.Contains(module) && IsEnabledByPlan(plan, module);
    }

    public static bool IsEnabledByPlan(SaasPlanDefinition? plan, SaasModule module)
    {
        if (module == SaasModule.PlatformTenants) return true;
        if (plan is null) return false;
        return true;
    }

    public static IReadOnlyCollection<SaasModule> GetEffectiveModules(InternalUserRole? role, bool isOwner, bool isPlatformAdmin = false, IReadOnlyCollection<SaasModule>? assignedModules = null)
    {
        if (isPlatformAdmin) return new[] { SaasModule.PlatformTenants };
        if (isOwner || role == InternalUserRole.Owner) return GetTenantModules();
        if (role != InternalUserRole.Employee) return Array.Empty<SaasModule>();

        var normalizedAssigned = NormalizeAssignedModules(assignedModules);
        return normalizedAssigned.Count > 0 ? normalizedAssigned : DefaultEmployeeModules;
    }

    public static IReadOnlyCollection<SaasModule> GetTenantModules()
        => Enum.GetValues<SaasModule>().Where(x => x != SaasModule.PlatformTenants).ToArray();

    public static IReadOnlyCollection<SaasModule> NormalizeAssignedModules(IEnumerable<SaasModule>? modules)
        => modules?
            .Where(x => x != SaasModule.PlatformTenants)
            .Distinct()
            .OrderBy(x => (int)x)
            .ToArray()
           ?? Array.Empty<SaasModule>();

    public static IReadOnlyCollection<SaasModule> ParseAssignedModules(string? modules)
        => NormalizeAssignedModules((modules ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Enum.TryParse<SaasModule>(x, out var module) ? module : (SaasModule?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value));

    public static string? SerializeAssignedModules(IEnumerable<SaasModule>? modules)
    {
        var normalized = NormalizeAssignedModules(modules);
        return normalized.Count == 0 ? null : string.Join(',', normalized);
    }
}
