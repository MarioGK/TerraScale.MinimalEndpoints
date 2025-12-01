namespace TerraScale.MinimalEndpoints;

/// <summary>
/// Interface that all minimal endpoint classes must implement
/// </summary>
public interface IMinimalEndpoint
{
    /// <summary>
    /// Gets the group type for this endpoint. Endpoints can declare their group
    /// by returning the group's type (for example: typeof(UserManagementGroup)).
    /// The generator will resolve the group's name from the type and use it for
    /// OpenAPI grouping. This replaces the older GroupName string property.
    /// </summary>
    System.Type? GroupType { get; }
    
    /// <summary>
    /// Gets the tags for this endpoint (used for OpenAPI categorization)
    /// </summary>
    string[]? Tags { get; }

    /// <summary>
    /// The route for this endpoint. Implementations should return the route
    /// pattern (e.g. "api/users/{id}"). This replaces the previous
    /// class-level attribute based route configuration.
    /// </summary>
    string? Route { get; }

    /// <summary>
    /// The HTTP method for this endpoint. Use the EndpointHttpMethod enum to
    /// declare the verb in a type-safe way.
    /// </summary>
    EndpointHttpMethod? HttpMethod { get; }
}
