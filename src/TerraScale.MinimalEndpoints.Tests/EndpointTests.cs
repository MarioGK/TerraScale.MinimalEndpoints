using System.Net;
using System.Net.Http.Json;
using System.Linq;
using TerraScale.MinimalEndpoints.Example.Models;
using TerraScale.MinimalEndpoints.Tests;

namespace TerraScale.MinimalEndpoints.Tests;

public class EndpointTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task GetWeather_ReturnsCorrectResponse_WithQueryParam()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/weather?city=London");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsEqualTo("Weather in London is Sunny");
    }

    [Test]
    public async Task ServiceEndpoint_ReturnsGreeting_WithDI()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/services/greet?name=Jules");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsEqualTo("Hello, Jules!");
    }

    [Test]
    public async Task UserEndpoints_CRUD_Flow()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 1. Create User
        var createRequest = new CreateUserRequest("Alice");
        var createResponse = await client.PostAsJsonAsync("/api/users", createRequest);

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
        await Assert.That(createdUser).IsNotNull();
        await Assert.That(createdUser!.Name).IsEqualTo("Alice");
        var userId = createdUser.Id;

        // 2. Get User
        var getResponse = await client.GetAsync($"/api/users/{userId}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var fetchedUser = await getResponse.Content.ReadFromJsonAsync<User>();
        await Assert.That(fetchedUser).IsEqualTo(createdUser);

        // 3. Update User
        var updateRequest = new UpdateUserRequest("Alice Updated");
        var updateResponse = await client.PutAsJsonAsync($"/api/users/{userId}", updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<User>();
        await Assert.That(updatedUser!.Name).IsEqualTo("Alice Updated");

        // 4. Delete User
        var deleteResponse = await client.DeleteAsync($"/api/users/{userId}");
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<bool>();
        await Assert.That(deleteResult).IsTrue();

        // 5. Verify Deleted (Get returns NotFound)
        var getResponse2 = await client.GetAsync($"/api/users/{userId}");
        await Assert.That(getResponse2.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task NewFeatures_VerifyContextAndConfigure()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/new-features");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify Header added by Configure -> AddEndpointFilter
        await Assert.That(response.Headers.Contains("X-Custom-Header")).IsTrue();
        var headerValues = response.Headers.GetValues("X-Custom-Header");
        await Assert.That(headerValues.FirstOrDefault()).IsEqualTo("Configured");

        // Verify Content (Ok helper and Context access)
        var content = await response.Content.ReadFromJsonAsync<NewFeatureResponse>();
        await Assert.That(content).IsNotNull();
        await Assert.That(content!.Message).IsEqualTo("Success");
    }
}
