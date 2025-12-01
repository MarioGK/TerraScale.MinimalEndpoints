using Microsoft.AspNetCore.Mvc;
// removed attribute usage; routing and metadata are now declared via IMinimalEndpoint/BaseMinimalApiEndpoint
using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Example.Services;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class UpdateUserEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/users/{id}";
    public override string HttpMethod => "PUT";
    public override string? GroupName => "User Management";
    
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<IResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserRequest request, [FromServices] IUserService userService)
    {
        await Task.Delay(1);
        var user = userService.Update(id, request.Name);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}
