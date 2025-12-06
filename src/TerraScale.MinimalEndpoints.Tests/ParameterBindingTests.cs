using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Tests;

namespace TerraScale.MinimalEndpoints.Tests;

public class ParameterBindingTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task FromQuery_Parameter_Binds_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/weather?city=London&country=UK");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsEqualTo("Weather in London is Sunny");
    }

    [Test]
    public async Task FromRoute_Parameter_Binds_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createUser = new CreateUserRequest("RouteTest");
        var createResponse = await client.PostAsJsonAsync("/api/users", createUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        var response = await client.GetAsync($"/api/users/{createdUser!.Id}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<User>();
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.Id).IsEqualTo(createdUser.Id);
    }

    [Test]
    public async Task FromBody_Parameter_Binds_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createUser = new CreateUserRequest("John Doe");
        var response = await client.PostAsJsonAsync("/api/users", createUser);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var createdUser = await response.Content.ReadFromJsonAsync<User>();
        await Assert.That(createdUser).IsNotNull();
        await Assert.That(createdUser!.Name).IsEqualTo("John Doe");
    }

    [Test]
    public async Task FromServices_Parameter_Binds_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/services/greet?name=Test");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsEqualTo("Hello, Test!");
    }

    [Test]
    public async Task Multiple_Parameters_Bind_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createUser = new CreateUserRequest("UpdateTest");
        var createResponse = await client.PostAsJsonAsync("/api/users", createUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        var updateUser = new UpdateUserRequest("Updated Name");
        var response = await client.PutAsJsonAsync($"/api/users/{createdUser!.Id}", updateUser);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updatedUser = await response.Content.ReadFromJsonAsync<User>();
        await Assert.That(updatedUser).IsNotNull();
        await Assert.That(updatedUser!.Name).IsEqualTo("Updated Name");
    }

    [Test]
    public async Task Missing_Required_Parameter_Returns_ValidationError()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(""));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Invalid_Parameter_Returns_ValidationError()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createUser = new CreateUserRequest(""); // Empty name should be invalid
        var response = await client.PostAsJsonAsync("/api/users", createUser);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Complex_Object_Parameter_Binds_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var complexRequest = new
        {
            Name = "Complex User",
            Profile = new
            {
                Age = 30,
                Preferences = new[] { "option1", "option2" }
            }
        };

        var response = await client.PostAsJsonAsync("/api/users", complexRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var createdUser = await response.Content.ReadFromJsonAsync<User>();
        await Assert.That(createdUser).IsNotNull();
        await Assert.That(createdUser!.Name).IsEqualTo("Complex User");
    }

    [Test]
    public async Task Optional_Parameter_Handles_Null_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/weather"); // No city parameter

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("Weather in"); // Should use default behavior
    }

    [Test]
    public async Task Parameter_Validation_Works_Correctly()
    {
        // Test that validation attributes work
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Test with invalid email format (assuming email validation exists)
        var invalidUser = new CreateUserRequest("invalid-email");
        var response = await client.PostAsJsonAsync("/api/users", invalidUser);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}
