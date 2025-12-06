using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using TerraScale.MinimalEndpoints.Tests;

namespace TerraScale.MinimalEndpoints.Tests;

public class GroupConfigurationTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Group_RoutePrefix_Is_Applied_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/weather?city=London");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Group_Name_Is_Used_For_OpenAPI_Grouping()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");

        var swaggerDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var paths = swaggerDoc!.RootElement.GetProperty("paths");
        
        var weatherPaths = paths.EnumerateObject()
            .Where(p => p.Name.Contains("/weather"))
            .ToList();
        
        await Assert.That(weatherPaths).Count().IsGreaterThan(0);
        
        var userPaths = paths.EnumerateObject()
            .Where(p => p.Name.Contains("/users"))
            .ToList();
        
        await Assert.That(userPaths).Count().IsGreaterThan(0);
    }

    [Test]
    public async Task Group_Tags_Are_Applied_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");

        var swaggerDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = swaggerDoc!.RootElement;
        
        var tags = root.GetProperty("tags").EnumerateArray();
        await Assert.That(tags.Count()).IsGreaterThan(0);
        
        var tagNames = tags.Select(t => t.GetProperty("name").GetString()).ToList();
        await Assert.That(tagNames).Contains("Weather API");
        await Assert.That(tagNames).Contains("User Management");
    }

    [Test]
    public async Task Group_Configure_Method_Is_Called()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/services/greet?name=Test");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var swaggerResponse = await client.GetAsync("/openapi/v1.json");
        var swaggerDoc = await swaggerResponse.Content.ReadFromJsonAsync<JsonDocument>();
        
        var paths = swaggerDoc!.RootElement.GetProperty("paths");
        
        if (paths.TryGetProperty("/api/services/greet", out var greetPath))
        {
            var getOperation = greetPath.GetProperty("get");
            var tags = getOperation.GetProperty("tags").EnumerateArray();
            var tagNames = tags.Select(t => t.GetString()).ToList();
            
            await Assert.That(tagNames).Contains("ServiceAPI");
        }
        else
        {
             var match = paths.EnumerateObject().FirstOrDefault(p => p.Name.Contains("/greet"));
             if (match.Value.ValueKind != JsonValueKind.Undefined)
             {
                 var getOperation = match.Value.GetProperty("get");
                 var tags = getOperation.GetProperty("tags").EnumerateArray();
                 var tagNames = tags.Select(t => t.GetString()).ToList();
                 await Assert.That(tagNames).Contains("ServiceAPI");
             }
             else
             {
                 Assert.Fail("Greet endpoint not found in OpenAPI");
             }
        }
    }

    [Test]
    public async Task Multiple_Groups_With_Same_RoutePrefix_Are_Handled()
    {
        var client = WebApplicationFactory.CreateClient();
        
        var weatherResponse = await client.GetAsync("/api/weather?city=London");
        var serviceResponse = await client.GetAsync("/api/services/greet?name=Test");

        await Assert.That(weatherResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(serviceResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Nested_Groups_Are_Supported()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/grouped/test");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Group_Inheritance_Works_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/weather?city=London");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Group_Filters_Are_Applied()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/api/new-features");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var hasCustomHeader = response.Headers.Contains("X-Custom-Header");
        await Assert.That(hasCustomHeader).IsTrue();
    }
}
