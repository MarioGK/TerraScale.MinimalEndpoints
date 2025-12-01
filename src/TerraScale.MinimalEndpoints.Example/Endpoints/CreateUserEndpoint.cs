using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// removed attribute usage; routing and metadata are now declared via IMinimalEndpoint/BaseMinimalApiEndpoint
using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Example.Services;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class CreateUserEndpoint : BaseMinimalApiEndpoint
{
    public override string Route => "api/users";
    public override string HttpMethod => "POST";
    public override string? GroupName => "User Management";
    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">The user creation request</param>
    /// <param name="userService">The user service</param>
    /// <returns>The newly created user</returns>
    /// <remarks>
    /// This endpoint requires Admin privileges to create new users.
    /// The user ID will be automatically generated.
    /// </remarks>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid user data provided</response>
    /// <response code="403">Admin privileges required</response>
    
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<User> CreateUser([FromBody] CreateUserRequest request, [FromServices] IUserService userService)
    {
        await Task.Delay(10); // Simulate async work
        return userService.Create(request.Name);
    }
}
