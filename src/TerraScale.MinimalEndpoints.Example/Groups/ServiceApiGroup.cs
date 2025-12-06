using TerraScale.MinimalEndpoints;
using TerraScale.MinimalEndpoints.Groups;

namespace TerraScale.MinimalEndpoints.Example.Groups;

public class ServiceApiGroup : EndpointGroup
{
    public override string Name => "Service API";
    public override string RoutePrefix => "/api/services";
    
    public override void Configure(RouteGroupBuilder builder)
    {
        builder.WithTags("ServiceAPI");
    }
}
