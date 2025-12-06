using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

using TerraScale.MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

    [UsedImplicitly]
public class ServiceEndpoints : BaseMinimalApiEndpoint<ServiceApiGroup>
{
    public override string Route => "greet";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;
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
