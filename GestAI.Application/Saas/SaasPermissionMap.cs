using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Application.Saas;

public static class SaasPermissionMap
{
    public static bool HasAccess(InternalUserRole? role, SaasPlanDefinition? plan, SaasModule module, bool isOwner, bool isPlatformAdmin = false)
    {
        if (isPlatformAdmin) return module == SaasModule.PlatformTenants;
        if (isOwner) return module is not SaasModule.PlatformTenants && IsEnabledByPlan(plan, module);
        if (role is null) return false;

        var byRole = role.Value switch
        {
            InternalUserRole.Owner => module is not SaasModule.PlatformTenants,
            InternalUserRole.Employee => module is SaasModule.Dashboard
                or SaasModule.Branches
                or SaasModule.Warehouses
                or SaasModule.Categories
                or SaasModule.Products
                or SaasModule.Customers
                or SaasModule.Suppliers,
            _ => false
        };

        return byRole && IsEnabledByPlan(plan, module);
    }

    public static bool IsEnabledByPlan(SaasPlanDefinition? plan, SaasModule module)
    {
        if (module == SaasModule.PlatformTenants) return true;
        if (plan is null) return false;
        return true;
    }
}
