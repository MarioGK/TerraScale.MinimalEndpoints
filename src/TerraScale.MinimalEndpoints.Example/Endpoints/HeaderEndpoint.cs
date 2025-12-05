using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class HeaderEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/header";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public Task<IResult> GetHeader([FromHeader(Name = "X-Test-Header")] string headerValue)
    {
        return Task.FromResult(Ok(new { Value = headerValue }));
    }
}
