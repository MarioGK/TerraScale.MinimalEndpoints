using Microsoft.AspNetCore.Mvc;

using TerraScale.MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class DeleteUserEndpoint : BaseMinimalApiEndpoint<UserManagementGroup>
{
    public override string Route => "api/users/{id}";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Delete;
    
    [Produces("application/json")]
    public async Task<IResult> DeleteUser([FromRoute] int id, [FromServices] IUserService userService)
    {
        await Task.Delay(1);
        var result = userService.Delete(id);
        return Results.Ok(result); // Returns boolean true/false
    }
}
