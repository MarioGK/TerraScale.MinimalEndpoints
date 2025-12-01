namespace TerraScale.MinimalEndpoints;

/// <summary>
/// Marker interface for endpoint groups. Implementations may override Name to
/// provide a friendly display name for OpenAPI grouping.
/// </summary>
public interface IEndpointGroup
{
    /// <summary>
    /// Friendly name used for grouping. Default implementations may just use the
    /// class name.
    /// </summary>
    string Name { get; }
}

/// <summary>
/// Convenience base class for endpoint groups. By default the group name is
/// the class name, but derived classes can override Name to use a more
/// descriptive value.
/// </summary>
public abstract class EndpointGroup : IEndpointGroup
{
    public virtual string Name => GetType().Name;
}
