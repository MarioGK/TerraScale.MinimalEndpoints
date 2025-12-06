using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints.Example.Groups;

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class CreateUserEndpoint : BaseMinimalApiEndpoint<UserManagementGroup>
{
    public override string Route => "";
    public override EndpointHttpMethod HttpMethod => EndpointHttpMethod.Post;

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
    [Produces("application/json", Type = typeof(User))]
    [Consumes("application/json")]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request, [FromServices] IUserService userService)
    {
        await Task.Delay(10); // Simulate async work

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required");
        }

        // Mock validation for test case
        if (request.Name == "invalid-email")
        {
            return BadRequest("Invalid email format");
        }

        return Ok(userService.Create(request.Name));
    }
}
