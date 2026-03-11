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
    public void SaasPermissionMap_RespectsPlanFeatures()
    {
        var plan = new SaasPlanDefinition
        {
            IncludesReports = false,
            IncludesOperations = true
        };

        Assert.False(SaasPermissionMap.IsEnabledByPlan(plan, SaasModule.Reports));
        Assert.True(SaasPermissionMap.IsEnabledByPlan(plan, SaasModule.Housekeeping));
    }

    [Fact]
    public void SaasPermissionMap_ReceptionRole_HasLimitedAccess()
    {
        var plan = new SaasPlanDefinition { IncludesReports = true, IncludesOperations = true };

        Assert.True(SaasPermissionMap.HasAccess(InternalUserRole.Reception, plan, SaasModule.Dashboard, false));
        Assert.False(SaasPermissionMap.HasAccess(InternalUserRole.Reception, plan, SaasModule.Configuration, false));
    }
}
