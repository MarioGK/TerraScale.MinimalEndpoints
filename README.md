# MinimalEndpoints

A source generator library that makes it easier to work with Minimal APIs in ASP.NET Core by providing a per-endpoint file pattern with automatic registration.

## Features

- **Per-endpoint file pattern**: Enforces the Single Responsibility Principle by requiring one endpoint per file.
- **Automatic registration**: Endpoints are automatically discovered and registered.
- **Dependency injection support**: Use `[FromServices]` attribute to inject services.
- **Parameter binding**: Support for `[FromBody]`, `[FromRoute]`, `[FromQuery]` attributes.
- **Base route support**: Define a base route for all endpoints in a class.
- **Source generation**: Compile-time generation with no runtime overhead.
- **OpenAPI support**: Built-in support for OpenAPI metadata including Tags, Summaries, Descriptions, Produces, and Consumes.

## Usage

### 1. Define an endpoint class

Each endpoint class must implement `IMinimalEndpoint` or inherit from `BaseMinimalApiEndpoint`. The library enforces a strict "one endpoint per file" rule to promote clean architecture.

```csharp
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints;
using MinimalEndpoints.Attributes;

[MinimalEndpoints("api/v1/users")]
[EndpointGroupName("User Management")]
public class CreateUserEndpoint : BaseMinimalApiEndpoint
{
    [HttpPost]
    [Produces("application/json", StatusCode = 201)]
    [Consumes("application/json")]
    public async Task<User> CreateUser(
        [FromServices] IUserService userService, 
        [FromBody] CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request);
        return user;
    }
}
```

### 2. Register endpoints in Program.cs

Use the generated extension methods to register services and map endpoints.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add application services
builder.Services.AddScoped<IUserService, UserService>();

// Register Minimal Endpoints services (automatically registers all endpoint classes)
builder.Services.AddMinimalEndpoints();

var app = builder.Build();

// Map Minimal Endpoints routes
app.MapMinimalEndpoints();

app.Run();
```

## Attributes

### `[MinimalEndpoints(baseRoute)]`
Marks a class as containing a minimal API endpoint. Provides a base route.

### `[EndpointGroupName(name)]`
Specifies the group name for OpenAPI documentation.

### `[Produces(contentType, StatusCode = 200)]`
Specifies the response content type and status code for OpenAPI.

### `[Consumes(contentType)]`
Specifies the request content type for OpenAPI.

### `[ResponseDescription(statusCode, description)]`
Adds a description for a specific response status code.

### Standard Attributes
- `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`
- `[FromServices]`, `[FromBody]`, `[FromRoute]`, `[FromQuery]`
- `[Authorize]`, `[AllowAnonymous]`

## Base Class & Interface

### `BaseMinimalApiEndpoint`
A convenience base class that implements `IMinimalEndpoint`. It provides default implementation for `GroupName` and `Tags`.

### `IMinimalEndpoint`
The interface that all endpoint classes must implement.
```csharp
public interface IMinimalEndpoint
{
    string? GroupName { get; }
    string[]? Tags { get; }
}
```

## Generated Code

The source generator automatically creates a `MinimalEndpointRegistration` class with:
- `AddMinimalEndpoints()`: Registers all endpoint classes in DI.
- `MapMinimalEndpoints()`: Maps all endpoints to routes.

The generated code handles:
- Service resolution
- Parameter binding
- OpenAPI metadata generation (Tags, Summary, Description, Produces/Accepts)
- Authentication/Authorization metadata

## Best Practices

- **One Endpoint Per File**: The library enforces this pattern. Split your endpoints (Get, Post, Put, Delete) into separate classes.
- **Use BaseMinimalApiEndpoint**: Inherit from this class to avoid implementing the interface manually.
- **Use XML Documentation**: The generator automatically extracts `<summary>`, `<remarks>`, `<response>`, and `<param>` tags from XML documentation to populate OpenAPI descriptions.

## Example

```csharp
[MinimalEndpoints("api/weather")]
public class GetWeatherEndpoint : BaseMinimalApiEndpoint
{
    /// <summary>
    /// Gets weather information
    /// </summary>
    /// <response code="200">Returns weather data</response>
    [HttpGet]
    [Produces("application/json")]
    public async Task<WeatherForecast> Handle()
    {
        // ...
    }
}
```
