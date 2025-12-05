using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Example.Services;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class Error500Endpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/error/500";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public Task<IResult> InternalServerError()
    {
        return Task.FromResult(Problem(statusCode: 500, detail: "Internal server error"));
    }
}

public class ErrorCustomEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/error/custom";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public Task<IResult> CustomError()
    {
        return Task.FromResult(BadRequest("Custom error message"));
    }
}

public class ErrorExceptionEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/error/exception";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public Task<IResult> ThrowException()
    {
        // To throw exception in async task
        throw new InvalidOperationException("Test exception");
        // Or return Task.FromException(new InvalidOperationException(...));
    }
}

public class ErrorTimeoutEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/slow/timeout";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public async Task<string> SlowEndpoint()
    {
        // Simulate a slow operation
        await Task.Delay(TimeSpan.FromSeconds(2));
        return "Response after delay";
    }
}
