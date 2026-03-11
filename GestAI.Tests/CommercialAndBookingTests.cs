using GestAI.Application.Common;
using GestAI.Application.Saas;
using GestAI.Domain.Entities;
using GestAI.Domain.Enums;
using Xunit;

namespace GestAI.Tests;

public class SaasCoreTests
{
    [Fact]
    public void DateRange_Overlaps_WhenRangesIntersect()
    {
        var overlaps = DateRange.Overlaps(new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 14), new DateOnly(2026, 3, 18));
        Assert.True(overlaps);
    }

    [Fact]
    public void SaasPermissionMap_AllowsConfiguredModulesForAdmin()
    {
        var plan = new SaasPlanDefinition();
        Assert.True(SaasPermissionMap.HasAccess(InternalUserRole.Admin, plan, SaasModule.Users, false));
    }

    [Fact]
    public void SaasPermissionMap_Reception_HasOnlyDashboard()
    {
        var plan = new SaasPlanDefinition();
        Assert.True(SaasPermissionMap.HasAccess(InternalUserRole.Reception, plan, SaasModule.Dashboard, false));
        Assert.False(SaasPermissionMap.HasAccess(InternalUserRole.Reception, plan, SaasModule.Users, false));
    }
}
