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
    public void SaasPermissionMap_Employee_HasCommerceModules()
    {
        var plan = new SaasPlanDefinition();
        Assert.True(SaasPermissionMap.HasAccess(InternalUserRole.Employee, plan, SaasModule.Products, false));
        Assert.False(SaasPermissionMap.HasAccess(InternalUserRole.Employee, plan, SaasModule.Users, false));
    }

    [Fact]
    public void SaasPermissionMap_PlatformAdmin_HasPlatformAccess()
    {
        var plan = new SaasPlanDefinition();
        Assert.True(SaasPermissionMap.HasAccess(null, plan, SaasModule.PlatformTenants, false, true));
    }
}
