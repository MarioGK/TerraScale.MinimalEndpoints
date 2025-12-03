using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using TerraScale.MinimalEndpoints;
using TerraScale.MinimalEndpoints.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

[EndpointGroupName("Custom Group Name")]
[Authorize(Roles = "Admin")]
public class MyTestGroup : EndpointGroup
{
    public override string RoutePrefix => "/grouped";

    public override void Configure(RouteGroupBuilder builder)
    {
        builder.WithTags("MyGroupTag");
    }
}

public class GroupedEndpoint : BaseMinimalApiEndpoint<MyTestGroup>
{
    public override string Route => "test";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;

    public Task<string> Handle()
    {
        return Task.FromResult("Grouped!");
    }
}
