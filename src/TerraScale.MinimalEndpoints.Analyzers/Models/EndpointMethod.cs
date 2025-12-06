namespace TerraScale.MinimalEndpoints.Analyzers.Models;

internal class EndpointMethod
{
    public string ClassNamespace { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public bool HasExplicitRoute { get; set; }
    public List<EndpointParameter> Parameters { get; set; } = new();
    public string ReturnType { get; set; } = string.Empty;
    public string ReturnTypeInner { get; set; } = "void";
    public bool HasAuthorize { get; set; }
    public bool HasAllowAnonymous { get; set; }
    public string? Policy { get; set; }
    public string? Roles { get; set; }
    public string? AuthenticationSchemes { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsDeprecated { get; set; }
    public string? GroupName { get; set; }
    public string? GroupTypeFullName { get; set; }
    public List<ProducesInfo> Produces { get; set; } = new();
    public List<string> Consumes { get; set; } = new();
    public Dictionary<int, string> ResponseDescriptions { get; set; } = new();
    public Dictionary<string, string> ParameterDescriptions { get; set; } = new();
    public bool HasConfigureMethod { get; set; }
    public List<string> EndpointFilters { get; set; } = new();
}

internal class ProducesInfo
{
    public int StatusCode { get; set; }
    public string? ResponseType { get; set; }
    public List<string> ContentTypes { get; set; } = new();
}
