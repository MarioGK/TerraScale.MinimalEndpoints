using System.Net;
using System.Net.Http.Json;
using TerraScale.MinimalEndpoints.Tests;

namespace TerraScale.MinimalEndpoints.Tests;

public class GranularUserTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task DeleteUser_NonExistent_ReturnsFalse()
    {
        var client = WebApplicationFactory.CreateClient();
        var token = TestHelpers.GenerateToken("Admin");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync("/api/users/999999");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<bool>();
        await Assert.That(result).IsFalse();
    }
}
