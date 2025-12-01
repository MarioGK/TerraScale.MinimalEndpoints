using Microsoft.AspNetCore.Mvc;
// removed attribute usage; routing and metadata are now declared via IMinimalEndpoint/BaseMinimalApiEndpoint

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class NewFeatureEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "/api/new-features";
    public override string HttpMethod => "GET";
    
    public async Task<IResult> Handle()
    {
        await Task.CompletedTask;

        // Check if HttpContext is injected
        if (Context == null)
            return Results.Problem("Context is null");

        // Check helper methods
        return Ok(new { Message = "Success", User = User.Identity?.Name ?? "Anonymous" });
    }

    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.Append("X-Custom-Header", "Configured");
            return await next(context);
        });
    }
}
