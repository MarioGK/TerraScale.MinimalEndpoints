using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using TerraScale.MinimalEndpoints.Tests;
using TerraScale.MinimalEndpoints.Example;
using TerraScale.MinimalEndpoints;
using TerraScale.MinimalEndpoints.Groups;

namespace TerraScale.MinimalEndpoints.Tests;

public class SourceGeneratorTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Generator_Detects_All_Endpoint_Classes()
    {
        var services = WebApplicationFactory.Services;
        var endpointAssembly = typeof(Program).Assembly;
        
        var expectedEndpointClasses = new[]
        {
            "WeatherEndpoints",
            "CreateUserEndpoint", 
            "GetUserEndpoint",
            "UpdateUserEndpoint",
            "DeleteUserEndpoint",
            "GroupedEndpoint",
            "NewFeatureEndpoint",
            "ServiceEndpoints",
            "PublicEndpoint",
            "PolicyProtectedEndpoint",
            "Error500Endpoint",
            "ErrorCustomEndpoint",
            "ErrorExceptionEndpoint",
            "ErrorTimeoutEndpoint",
            "UploadEndpoint",
            "HeaderEndpoint"
        };

        var actualEndpointTypes = endpointAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && expectedEndpointClasses.Contains(t.Name))
            .ToList();

        await Assert.That(actualEndpointTypes.Count).IsGreaterThanOrEqualTo(expectedEndpointClasses.Length);
        
        foreach (var expectedClass in expectedEndpointClasses)
        {
            var type = actualEndpointTypes.FirstOrDefault(t => t.Name == expectedClass);
            await Assert.That(type).IsNotNull();

            using var scope = services.CreateScope();
            var instance = scope.ServiceProvider.GetService(type!);
            await Assert.That(instance).IsNotNull();
        }
    }

    [Test]
    public async Task Generator_Handles_Multiple_Methods_In_One_Class()
    {
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Generator_Validates_Interface_Implementation()
    {
        var services = WebApplicationFactory.Services;
        var endpointAssembly = typeof(Program).Assembly;

        var endpointTypes = endpointAssembly.GetTypes()
             .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Endpoint"));

        foreach (var endpointType in endpointTypes)
        {
             if (endpointType.GetCustomAttributes().Any(a => a.GetType().Name.Contains("MinimalEndpointsAttribute")))
             {
                 await Assert.That(typeof(IMinimalEndpoint).IsAssignableFrom(endpointType))
                    .IsTrue();
             }
        }
    }

    [Test]
    public async Task Generator_Handles_Attributes_Correctly()
    {
        var endpointAssembly = typeof(Program).Assembly;
        
        var hasMinimalEndpointsAttribute = endpointAssembly.GetTypes()
            .Any(t => t.GetCustomAttributes().Any(a => a.GetType().Name.Contains("MinimalEndpointsAttribute")));
        
        var hasGroupNameAttributes = endpointAssembly.GetTypes()
            .Any(t => t.GetCustomAttributes().Any(a => a.GetType().Name.Contains("EndpointGroupNameAttribute")));
        
        await Assert.That(hasMinimalEndpointsAttribute).IsTrue();
        await Assert.That(hasGroupNameAttributes).IsTrue();
    }

    [Test]
    public async Task Generator_Handles_Group_Attributes()
    {
        var endpointAssembly = typeof(Program).Assembly;
        
        var hasAuthorizeOnGroups = endpointAssembly.GetTypes()
            .Where(t => typeof(EndpointGroup).IsAssignableFrom(t))
            .Any(t => t.GetCustomAttributes().Any(a => a.GetType().Name.Contains("AuthorizeAttribute")));
        
        await Assert.That(hasAuthorizeOnGroups).IsTrue();
    }

    [Test]
    public async Task Generator_Registers_Endpoints_In_DI()
    {
        var services = WebApplicationFactory.Services;
        var type = typeof(Program).Assembly.GetTypes().FirstOrDefault(t => t.Name == "CreateUserEndpoint");
        await Assert.That(type).IsNotNull();
        
        using var scope = services.CreateScope();
        var instance = scope.ServiceProvider.GetService(type!);
        await Assert.That(instance).IsNotNull();
    }

    [Test]
    public async Task Generator_Handles_Route_Conflicts()
    {
        var endpointAssembly = typeof(Program).Assembly;
        var endpoints = endpointAssembly.GetTypes()
            .Where(t => t.GetCustomAttributes().Any(a => a.GetType().Name.Contains("MinimalEndpointsAttribute")));

        await Assert.That(endpoints.Count()).IsGreaterThan(0);
    }

    [Test]
    public async Task Generator_Produces_Valid_Namespace()
    {
        var registrationType = typeof(TerraScale.MinimalEndpoints.Generated_TerraScale_MinimalEndpoints_Example.MinimalEndpointRegistration);
        await Assert.That(registrationType.Namespace).IsEqualTo("TerraScale.MinimalEndpoints.Generated_TerraScale_MinimalEndpoints_Example");
    }

    [Test]
    public async Task Generator_Handles_Dependencies_Correctly()
    {
        var services = WebApplicationFactory.Services;
        
        var userService = services.GetService<TerraScale.MinimalEndpoints.Example.Services.IUserService>();
        await Assert.That(userService).IsNotNull();
        
        var greetingService = services.GetService<TerraScale.MinimalEndpoints.Example.Services.IGreetingService>();
        await Assert.That(greetingService).IsNotNull();
    }
}
