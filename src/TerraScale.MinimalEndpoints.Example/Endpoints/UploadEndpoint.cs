using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class UploadEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/upload";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Post;

    [Produces("application/json")]
    public async Task<IResult> Upload([FromForm] IFormFile file)
    {
        await Task.Delay(1);
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        return Ok(new { FileName = file.FileName, Size = file.Length });
    }

    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.DisableAntiforgery();
    }
}
