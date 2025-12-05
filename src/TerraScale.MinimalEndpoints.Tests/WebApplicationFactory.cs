using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Core.Interfaces;
using TerraScale.MinimalEndpoints.Example;

namespace TerraScale.MinimalEndpoints.Tests;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        _ = Server;

        return Task.CompletedTask;
    }
}
