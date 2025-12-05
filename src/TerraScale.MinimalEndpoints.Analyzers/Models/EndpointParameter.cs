namespace TerraScale.MinimalEndpoints.Analyzers.Models;

internal class EndpointParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsFromServices { get; set; }
    public bool IsFromBody { get; set; }
    public bool IsFromRoute { get; set; }
    public bool IsFromQuery { get; set; }
    public bool IsFromHeader { get; set; }
    public string? FromHeaderName { get; set; }
    public bool IsFromForm { get; set; }
}
