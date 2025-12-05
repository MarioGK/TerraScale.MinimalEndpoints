using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using TerraScale.MinimalEndpoints;
using TerraScale.MinimalEndpoints.Groups;

namespace TerraScale.MinimalEndpoints.Example.Groups;

[EndpointGroupName("Custom Group Name")]
[Authorize(Roles = "Admin")]
public class MyTestGroup : EndpointGroup
{
    public override string Name => "My Test Group";
    public override string RoutePrefix => "/grouped";

    public override void Configure(RouteGroupBuilder builder)
    {
        builder.WithTags("MyGroupTag");
    }
}