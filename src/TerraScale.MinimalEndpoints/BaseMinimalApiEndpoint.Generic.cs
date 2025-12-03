using TerraScale.MinimalEndpoints.Groups;

namespace TerraScale.MinimalEndpoints;

/// <summary>
/// Generic variant of BaseMinimalApiEndpoint to allow simple declaration
/// of an endpoint group via a type parameter (TGroup).
/// </summary>
public abstract class BaseMinimalApiEndpoint<TGroup> : BaseMinimalApiEndpoint
    where TGroup : IEndpointGroup, new()
{
    public override Type GroupType => typeof(TGroup);
}
