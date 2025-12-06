using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TerraScale.MinimalEndpoints.Tests;
using TerraScale.MinimalEndpoints.Example.Models;

namespace TerraScale.MinimalEndpoints.Tests;

public class AuthenticationTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Endpoint_With_Authorize_Attribute_Returns_401_Without_Credentials()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/users/1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Endpoint_With_Authorize_Attribute_Returns_200_With_Valid_Credentials()
    {
        var client = WebApplicationFactory.CreateClient();
        
        var token = TestHelpers.GenerateToken("Admin");

        // Create User first
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        createRequest.Content = JsonContent.Create(new CreateUserRequest("TestUserAuth"));
        var createResponse = await client.SendAsync(createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{createdUser!.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Endpoint_With_AllowAnonymous_Attribute_Returns_200_Without_Credentials()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/public/test");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Endpoint_With_Role_Based_Authorization_Returns_Correct_Responses()
    {
        var client = WebApplicationFactory.CreateClient();
        
        // Create User first with Admin token
        var setupToken = TestHelpers.GenerateToken("Admin");
        var setupRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        setupRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", setupToken);
        setupRequest.Content = JsonContent.Create(new CreateUserRequest("TestUserRole"));
        var setupRes = await client.SendAsync(setupRequest);
        var user = await setupRes.Content.ReadFromJsonAsync<User>();
        var userId = user!.Id;

        // Test with Admin role
        var adminToken = TestHelpers.GenerateToken("Admin");
        var adminRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        
        var adminResponse = await client.SendAsync(adminRequest);
        await Assert.That(adminResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Test with User role (should be forbidden for admin-only endpoint)
        var userToken = TestHelpers.GenerateToken("User");
        var userRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        
        var userResponse = await client.SendAsync(userRequest);
        await Assert.That(userResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Multiple_Authorize_Attributes_On_Endpoint_And_Group_Are_Combined()
    {
        var client = WebApplicationFactory.CreateClient();

        // Group has [Authorize(Roles="Admin")]
        // Request without token
        var response = await client.GetAsync("/grouped/test");

        // Should return 401 (Unauthorized) because no token
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        // Test with User token (should be 403 Forbidden)
        var userToken = TestHelpers.GenerateToken("User");
        var request = new HttpRequestMessage(HttpMethod.Get, "/grouped/test");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var responseForbidden = await client.SendAsync(request);
        await Assert.That(responseForbidden.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        // Test with Admin token (should be 200 OK)
        var adminToken = TestHelpers.GenerateToken("Admin");
        var requestOk = new HttpRequestMessage(HttpMethod.Get, "/grouped/test");
        requestOk.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var responseOk = await client.SendAsync(requestOk);
        await Assert.That(responseOk.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Policy_Based_Authorization_Works_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/policy-protected");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Authentication_Scheme_Is_Handled_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/weather?city=London");

        // Weather endpoint doesn't require auth
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
