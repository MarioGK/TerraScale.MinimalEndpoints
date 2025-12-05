using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TerraScale.MinimalEndpoints.Tests;

namespace TerraScale.MinimalEndpoints.Tests;

public class FeatureTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task FileUpload_Works_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "file", "test.bin");

        var response = await client.PostAsync("/api/upload", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UploadResult>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FileName).IsEqualTo("test.bin");
        await Assert.That(result.Size).IsEqualTo(4);
    }

    [Test]
    public async Task FromHeader_Parameter_Binds_Correctly()
    {
        var client = WebApplicationFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Header", "test-value");

        var response = await client.GetAsync("/api/header");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HeaderResult>();
        await Assert.That(content).IsNotNull();
        await Assert.That(content!.Value).IsEqualTo("test-value");
    }
}

public record UploadResult(string FileName, long Size);
public record HeaderResult(string Value);
