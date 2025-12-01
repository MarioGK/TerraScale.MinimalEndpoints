using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TerraScale.MinimalEndpoints;

/// <summary>
/// Base class for minimal endpoints with default implementation for GroupName and Tags
/// </summary>
public abstract class BaseMinimalApiEndpoint : IMinimalEndpoint
{
    /// <inheritdoc/>
    /// <inheritdoc/>
    public virtual System.Type? GroupType => null;

    /// <inheritdoc/>
    public virtual string[]? Tags => null;

    /// <summary>
    /// The route for this endpoint. Defaults to null/empty which will allow
    /// generator to fall back to a convention-based route if needed.
    /// </summary>
    public virtual string? Route => null;

    /// <summary>
    /// The HTTP method for this endpoint (e.g. "GET", "POST"). Defaults to null
    /// which will allow existing Http* attributes to be used for backward
    /// compatibility by the analyzer/generator.
    /// </summary>
    public virtual EndpointHttpMethod? HttpMethod => null;
    
    /// <summary>
    /// Gets or sets the HttpContext for the current request.
    /// </summary>
    public HttpContext Context { get; set; } = null!;

    /// <summary>
    /// Gets the current user from the HttpContext.
    /// </summary>
    public ClaimsPrincipal User => Context?.User ?? new ClaimsPrincipal();

    /// <summary>
    /// Returns an OK (200) result.
    /// </summary>
    protected IResult Ok() => Results.Ok();

    /// <summary>
    /// Returns an OK (200) result with value.
    /// </summary>
    protected static IResult Ok<T>(T value) => Results.Ok(value);

    /// <summary>
    /// Returns a Created (201) result.
    /// </summary>
    protected IResult Created(string uri, object? value) => Results.Created(uri, value);

    /// <summary>
    /// Returns a Created (201) result.
    /// </summary>
    protected IResult Created<T>(string uri, T value) => Results.Created(uri, value);

    /// <summary>
    /// Returns a NotFound (404) result.
    /// </summary>
    protected IResult NotFound() => Results.NotFound();

    /// <summary>
    /// Returns a NotFound (404) result with value.
    /// </summary>
    protected IResult NotFound<T>(T value) => Results.NotFound(value);

    /// <summary>
    /// Returns a BadRequest (400) result.
    /// </summary>
    protected IResult BadRequest() => Results.BadRequest();

    /// <summary>
    /// Returns a BadRequest (400) result with error.
    /// </summary>
    protected IResult BadRequest<T>(T error) => Results.BadRequest(error);

    /// <summary>
    /// Returns a Unauthorized (401) result.
    /// </summary>
    protected IResult Unauthorized() => Results.Unauthorized();

    /// <summary>
    /// Returns a Forbidden (403) result.
    /// </summary>
    protected IResult Forbid() => Results.Forbid();

    /// <summary>
    /// Returns a NoContent (204) result.
    /// </summary>
    protected IResult NoContent() => Results.NoContent();

    /// <summary>
    /// Returns a Problem (500) result.
    /// </summary>
    protected IResult Problem(string? detail = null, string? instance = null, int? statusCode = null, string? title = null, string? type = null, IDictionary<string, object?>? extensions = null)
        => Results.Problem(detail, instance, statusCode, title, type, extensions);
}
