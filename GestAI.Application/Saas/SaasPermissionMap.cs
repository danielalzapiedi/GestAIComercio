using GestAI.Domain.Entities;
using GestAI.Domain.Enums;

namespace GestAI.Application.Saas;

public static class SaasPermissionMap
{
    public static bool HasAccess(InternalUserRole? role, SaasPlanDefinition? plan, SaasModule module, bool isOwner)
    {
        if (isOwner) return IsEnabledByPlan(plan, module);
        if (role is null) return false;

        var byRole = role.Value switch
        {
            InternalUserRole.Owner => true,
            InternalUserRole.Admin => true,
            InternalUserRole.Reception => module is SaasModule.Dashboard,
            InternalUserRole.Operations => module is SaasModule.Dashboard,
            _ => false
        };

        return byRole && IsEnabledByPlan(plan, module);
    }

    public static bool IsEnabledByPlan(SaasPlanDefinition? plan, SaasModule module)
    {
        if (plan is null) return false;
        return true;
    }
}
