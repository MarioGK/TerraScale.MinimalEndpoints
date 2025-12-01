# MinimalEndpoints - Project Analysis for Agents

## Overview
**MinimalEndpoints** is a C# source code generator that simplifies ASP.NET Core minimal API development by providing a FastEndpoints alternative built on top of minimal APIs.
It combines the power of ASP.NET Core's minimal APIs with an attribute-driven, class-based endpoint structure.

## Project Purpose
The library eliminates boilerplate code by automatically generating endpoint registration code from endpoint classes decorated with attributes, providing developers with:
- A clean, structured way to organize endpoints
- Automatic registration of endpoints
- Built-in support for OpenAPI/Swagger documentation
- Authorization and authentication handling
- Validation and parameter binding

## Architecture

### Core Components

#### 1. **IMinimalEndpoint & BaseMinimalApiEndpoint** (`src/MinimalEndpoints/`)
- **IMinimalEndpoint**: Interface that all endpoint classes must implement
  - Defines `GroupName` (for OpenAPI grouping)
  - Defines `Tags` (for OpenAPI categorization)
  
- **BaseMinimalApiEndpoint**: Base class providing:
  - Default implementations of `GroupName` and `Tags`
  - `HttpContext` property for accessing request context
  - `User` property (ClaimsPrincipal) for accessing current user
  - Helper methods for returning HTTP responses (Ok, Created, NotFound, BadRequest, Unauthorized, Forbid, NoContent, Problem)

#### 2. **Source Generator** (`MinimalEndpointGenerator.cs`)
- Implements `IIncrementalGenerator` interface (Roslyn)
- Discovers all classes with:
  - `[MinimalEndpoints(...)]` attribute
  - HTTP method attributes (`[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`)
- Validates that classes:
  - Implement `IMinimalEndpoint` interface
  - Contain only one HTTP method per file
  - Methods are async (return Task)
- Generates the `MinimalEndpoints.g.cs` file with endpoint registration code

#### 3. **Endpoint Analyzer** (`Analyzers/EndpointAnalyzer.cs`)
- Analyzes endpoint methods to extract:
  - HTTP method type (GET, POST, PUT, DELETE, PATCH)
  - Route patterns
  - Parameters with binding sources ([FromBody], [FromQuery], [FromRoute], [FromServices])
  - Authorization attributes and policies
  - OpenAPI metadata (summary, description, tags, deprecation)
  - XML documentation comments
  - Response types and content types
- Reports diagnostics for violations

#### 4. **Endpoint Registration Generator** (`Generators/EndpointRegistrationGenerator.cs`)
- Generates extension methods in `MinimalEndpointRegistration` class:
  - `AddMinimalEndpoints()`: Registers endpoint classes in DI container as scoped services
  - `MapMinimalEndpoints()`: Maps all endpoints to the route builder
- Handles:
  - Lambda parameter construction with proper binding attributes
  - HttpContext injection into base class's Context property
  - Method invocation with parameter passing
  - OpenAPI metadata application (summary, description, tags, produces, consumes, response codes)
  - Configuration method calls if present

#### 5. **Models** (`Models/`)
- **EndpointMethod**: Contains all metadata about an endpoint method
- **EndpointParameter**: Represents method parameters with binding information
- **ProducesInfo**: Encodes response type information

#### 6. **Attributes** (`Attributes/`)
- **MinimalEndpointsAttribute**: Marks a class as an endpoint and sets the base route
- **EndpointGroupNameAttribute**: Sets the OpenAPI group name
- **OpenApiAttributes**: Additional attributes for OpenAPI documentation control

#### 7. **Helpers** (`Helpers/`)
- **XmlDocumentationHelper**: Extracts XML documentation comments from code to populate OpenAPI metadata

### Example Project Structure (`src/MinimalEndpoints.Example/`)
Demonstrates the library usage with:
- **Endpoints**: Sample endpoint implementations (CreateUserEndpoint, GetUserEndpoint, etc.)
- **Models**: Data models used by endpoints
- **Services**: Business logic services injected into endpoints
- **Program.cs**: Application setup showing how to register and map endpoints

## Key Features

### 1. **Attribute-Driven Development**
```csharp
[MinimalEndpoints("api/users")]
[EndpointGroupName("User Management")]
public class CreateUserEndpoint : BaseMinimalApiEndpoint
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Produces("application/json", StatusCode = 201)]
    [Consumes("application/json")]
    public async Task<User> CreateUser([FromBody] CreateUserRequest request, [FromServices] IUserService userService)
    {
        return userService.Create(request.Name);
    }
}
```

### 2. **Automatic Code Generation**
- No manual endpoint registration needed
- Generates type-safe routing code
- Automatically injects dependencies

### 3. **OpenAPI/Swagger Support**
- XML documentation comments converted to OpenAPI metadata
- Automatic request/response type documentation
- Authorization requirement documentation
- Deprecation support

### 4. **Validation & Diagnostics**
- Enforces async methods (Task return type)
- Requires IMinimalEndpoint implementation
- One endpoint per file rule
- Clear diagnostic error messages with specific error codes:
  - ME001: Endpoint method must be async
  - ME002: Endpoint class must implement IMinimalEndpoint
  - ME003: Only one endpoint per file allowed

### 5. **Type Safety**
- Full compile-time checking
- Compile-time dependency resolution
- Type-safe parameter binding

## Development Workflow

### Adding a New Endpoint
1. Create a new class file inheriting from `BaseMinimalApiEndpoint`
2. Implement `IMinimalEndpoint` (inherited from base)
3. Apply `[MinimalEndpoints("route")]` attribute
4. Add HTTP method (`[HttpPost]`, etc.)
5. Implement the async method
6. Build the project - generator automatically creates registration code
7. Method is automatically available at runtime via `AddMinimalEndpoints()` and `MapMinimalEndpoints()`

### Building the Project
```bash
dotnet build
```
The source generator runs during build and creates `MinimalEndpoints.g.cs` with all endpoint registrations.

## Diagnostic Error Codes
- **TSME001**: Endpoint method must return `Task` (async requirement)
- **TSME002**: Endpoint class must implement `IMinimalEndpoint`
- **TSME003**: Only one HTTP method allowed per endpoint class

## Design Patterns Used
1. **Source Generation**: Roslyn-based code generation for automatic endpoint discovery
2. **Convention over Configuration**: Attributes drive behavior
3. **Dependency Injection**: Full DI integration
4. **Base Class Pattern**: Common functionality in `BaseMinimalApiEndpoint`
5. **Builder Pattern**: Route builder configuration in MapMinimalEndpoints
6. **Extension Methods**: Clean API for registration (`AddMinimalEndpoints()`, `MapMinimalEndpoints()`)

## Future Enhancement Opportunities
- Custom validators integration
- Rate limiting support
- Request/response logging
- Health check endpoint generation

## Testing
- Unit tests in `MinimalEndpoints.Tests/`
- Integration tests with example application
- WebApplicationFactory for testing HTTP endpoints
- Example endpoints in `MinimalEndpoints.Example/Endpoints/`
