using GestAI.Application.Common;
using GestAI.Application.Commerce;
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
        Assert.False(SaasPermissionMap.HasAccess(null, plan, SaasModule.Products, false, true));
        Assert.False(SaasPermissionMap.HasAccess(null, plan, SaasModule.Users, false, true));
    }

    [Fact]
    public void GetBranchesQueryValidator_RejectsInvalidPaging()
    {
        var validator = new GetBranchesQueryValidator();
        var result = validator.Validate(new GetBranchesQuery(Page: 0, PageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(GetBranchesQuery.Page));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(GetBranchesQuery.PageSize));
    }

    [Fact]
    public void GetProductsQueryValidator_AllowsExpectedPagingBounds()
    {
        var validator = new GetProductsQueryValidator();
        var result = validator.Validate(new GetProductsQuery(Page: 1, PageSize: 100));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetTenantListQueryValidator_RejectsInvalidPageSize()
    {
        var validator = new GetTenantListQueryValidator();
        var result = validator.Validate(new GetTenantListQuery(Page: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(GetTenantListQuery.PageSize));
    }
}
