using Microsoft.AspNetCore.Mvc;

using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class GetUserEndpoint : BaseMinimalApiEndpoint<UserManagementGroup>
{
    public override string Route => "api/users/{id}";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Get;
    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="userService">The user service</param>
    /// <returns>The user if found, or null</returns>
    /// <response code="200">User found or null if not found</response>
    
    [Produces("application/json")]
    public async Task<User?> GetUser([FromRoute] int id, [FromServices] IUserService userService)
    {
        await Task.Delay(1);
        return userService.Get(id);
    }
}
