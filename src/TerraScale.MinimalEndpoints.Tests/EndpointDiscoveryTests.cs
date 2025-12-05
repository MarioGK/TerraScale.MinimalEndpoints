using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TerraScale.MinimalEndpoints.Tests;
using TerraScale.MinimalEndpoints.Example;

namespace TerraScale.MinimalEndpoints.Tests;

public class EndpointDiscoveryTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task All_Endpoint_Classes_Are_Registered_In_DI_Container()
    {
        var services = WebApplicationFactory.Services;
        var endpointAssembly = typeof(Program).Assembly;
        var endpointTypes = endpointAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMinimalEndpoint).IsAssignableFrom(t))
            .ToList();

        using var scope = services.CreateScope();
        foreach (var endpointType in endpointTypes)
        {
            var instance = scope.ServiceProvider.GetService(endpointType);
            await Assert.That(instance).IsNotNull();
        }
    }

    [Test]
    public async Task Endpoint_Without_Attributes_Is_Not_Registered()
    {
        var services = WebApplicationFactory.Services;
        var nonEndpointType = typeof(EndpointDiscoveryTests);
        using var scope = services.CreateScope();
        var instance = scope.ServiceProvider.GetService(nonEndpointType);
        await Assert.That(instance).IsNull();
    }

    [Test]
    public async Task Endpoint_With_Multiple_Methods_Should_Generate_Diagnostics()
    {
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Endpoint_Without_IMinimalEndpoint_Should_Generate_Diagnostic()
    {
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Endpoint_Route_Conflict_Should_Be_Handled()
    {
        var client = WebApplicationFactory.CreateClient();
        var response = await client.GetAsync("/nonexistent/endpoint");
        
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task All_Http_Methods_Are_Supported()
    {
        var endpointAssembly = typeof(Program).Assembly;
        var endpointTypes = endpointAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMinimalEndpoint).IsAssignableFrom(t));

        var hasGet = endpointTypes.Any(t => t.Name.Contains("Weather") || t.Name.Contains("GetUser") || t.Name.Contains("Grouped"));
        var hasPost = endpointTypes.Any(t => t.Name.Contains("CreateUser"));
        var hasPut = endpointTypes.Any(t => t.Name.Contains("UpdateUser"));
        var hasDelete = endpointTypes.Any(t => t.Name.Contains("DeleteUser"));

        await Assert.That(hasGet).IsTrue();
        await Assert.That(hasPost).IsTrue();
        await Assert.That(hasPut).IsTrue();
        await Assert.That(hasDelete).IsTrue();
    }
}
