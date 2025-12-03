using Microsoft.AspNetCore.Mvc;

using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class UpdateUserEndpoint : BaseMinimalApiEndpoint<UserManagementGroup>
{
    public override string Route => "api/users/{id}";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Put;
    
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<IResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserRequest request, [FromServices] IUserService userService)
    {
        await Task.Delay(1);
        var user = userService.Update(id, request.Name);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}
