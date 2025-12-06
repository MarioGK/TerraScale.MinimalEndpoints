using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class GetUserEndpoint : BaseMinimalApiEndpoint<UserManagementGroup>
{
    public override string Route => "{id}";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;
    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="userService">The user service</param>
    /// <returns>The user if found, or null</returns>
    /// <response code="200">User found</response>
    /// <response code="404">User not found</response>
    
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public async Task<IResult> GetUser([FromRoute] int id, [FromServices] IUserService userService)
    {
        await Task.Delay(1);
        var user = userService.Get(id);
        if (user == null)
        {
             return NotFound();
        }
        return Ok(user);
    }
}
