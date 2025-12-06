using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TerraScale.MinimalEndpoints.Attributes;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class PublicEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/public/test";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public Task<string> Get()
    {
        return Task.FromResult("Public content");
    }
}

[MinimalEndpoints("api/policy-protected")]
[Authorize(Policy = "AdminOnly")]
public class PolicyProtectedEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/policy-protected";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public Task<string> Get()
    {
        return Task.FromResult("Protected by policy");
    }
}
