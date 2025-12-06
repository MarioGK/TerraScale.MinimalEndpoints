using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using TerraScale.MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class DeleteUserEndpoint : BaseMinimalApiEndpoint<UserManagementGroup>
{
    public override string Route => "{id}";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Delete;
    
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public async Task<IResult> DeleteUser([FromRoute] int id, [FromServices] IUserService userService)
    {
        await Task.Delay(1);
        var result = userService.Delete(id);
        return Results.Ok(result);
    }
}
