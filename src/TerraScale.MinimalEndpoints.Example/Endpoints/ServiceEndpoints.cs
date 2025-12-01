using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
// removed attribute usage; routing and metadata are now declared via IMinimalEndpoint/BaseMinimalApiEndpoint
using TerraScale.MinimalEndpoints.Example.Services;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

    [UsedImplicitly]
public class ServiceEndpoints : BaseMinimalApiEndpoint
{
    public override string Route => "api/services/greet";
    public override string HttpMethod => "GET";
    public override string? GroupName => "Service API";
    /// <summary>
    /// Greets a user with a personalized message
    /// </summary>
    /// <param name="name">The name to greet</param>
    /// <param name="service">The greeting service</param>
    /// <returns>A personalized greeting message</returns>
    /// <remarks>
    /// This endpoint demonstrates dependency injection with FromServices.
    /// Uses the greeting service to create a personalized message.
    /// </remarks>
    /// <response code="200">Greeting message generated successfully</response>
    /// <response code="400">Invalid name provided</response>
    
    [Produces("text/plain")]
    public async Task<string> Greet([FromQuery] string name,
        [FromServices] IGreetingService service)
    {
        await Task.Delay(1); // Simulate async work
        return service.Greet(name);
    }
}
