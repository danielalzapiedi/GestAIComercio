using GestAI.Api.Middleware;
using GestAI.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace GestAI.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public async Task AppResultFailure_IsMappedTo_ProblemDetailsWithForbiddenStatus()
    {
        var filter = new AppResultHttpMappingFilter();
        var context = CreateContext(AppResult.Fail("forbidden", "Acceso denegado"));

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        var mapped = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, mapped.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(mapped.Value);
        Assert.Equal("forbidden", problem.Extensions["errorCode"]);
        Assert.Equal("trace-contract", problem.Extensions["correlationId"]);
    }


    [Fact]
    public async Task AppResultFailure_WithoutMessage_UsesCatalogDefaultMessage()
    {
        var filter = new AppResultHttpMappingFilter();
        var context = CreateContext(AppResult.Fail("forbidden", null!));

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        var mapped = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(mapped.Value);
        Assert.Equal("No tenés permisos para ejecutar esta acción.", problem.Detail);
    }

    [Fact]
    public async Task GenericAppResultFailure_IsMappedTo_ConflictStatus()
    {
        var filter = new AppResultHttpMappingFilter();
        var context = CreateContext(AppResult<int>.Fail("duplicate_code", "Código duplicado"));

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        var mapped = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status409Conflict, mapped.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(mapped.Value);
        Assert.Equal("duplicate_code", problem.Extensions["errorCode"]);
    }

    [Fact]
    public async Task SuccessfulAppResult_RemainsUnchanged()
    {
        var filter = new AppResultHttpMappingFilter();
        var payload = AppResult<int>.Ok(123);
        var context = CreateContext(payload);

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Same(payload, result.Value);
        Assert.Null(result.StatusCode);
    }

    private static ResultExecutingContext CreateContext(object value)
    {
        var http = new DefaultHttpContext();
        http.TraceIdentifier = "trace-contract";

        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var result = new ObjectResult(value);
        return new ResultExecutingContext(action, filters, result, controller: null);
    }

    private static ResultExecutedContext CreateExecutedContext(ResultExecutingContext context)
        => new(context, new List<IFilterMetadata>(), context.Result, controller: null);
}
