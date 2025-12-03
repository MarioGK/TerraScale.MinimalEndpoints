using Microsoft.AspNetCore.Routing;

namespace TerraScale.MinimalEndpoints.Groups;

/// <summary>
/// Interface for endpoint groups. Implementations may override Name to
/// provide a friendly display name for OpenAPI grouping, define a RoutePrefix,
/// and configure the group via RouteGroupBuilder.
/// </summary>
public interface IEndpointGroup
{
    /// <summary>
    /// Friendly name used for grouping. Default implementations may just use the
    /// class name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The route prefix for this group. Defaults to "/" in the base class.
    /// </summary>
    string RoutePrefix { get; }

    /// <summary>
    /// Configures the route group builder (e.g. adding authentication, filters, etc.).
    /// </summary>
    /// <param name="builder">The route group builder.</param>
    void Configure(RouteGroupBuilder builder);
}

/// <summary>
/// Convenience base class for endpoint groups. By default the group name is
/// the class name, but derived classes can override Name to use a more
/// descriptive value.
/// </summary>
public abstract class EndpointGroup : IEndpointGroup
{
    /// <inheritdoc />
    public virtual string Name => GetType().Name;

    /// <inheritdoc />
    public virtual string RoutePrefix => "/";

    /// <inheritdoc />
    public virtual void Configure(RouteGroupBuilder builder) { }
}
