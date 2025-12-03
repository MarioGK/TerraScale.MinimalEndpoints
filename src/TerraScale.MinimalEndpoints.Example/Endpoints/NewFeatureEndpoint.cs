using Microsoft.AspNetCore.Mvc;


namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class NewFeatureEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "/api/new-features";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;
    
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
