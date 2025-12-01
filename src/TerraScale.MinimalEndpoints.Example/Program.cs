using TerraScale.MinimalEndpoints;
// Use the generated registration helpers for this assembly
using TerraScale.MinimalEndpoints.Generated_TerraScale_MinimalEndpoints_Example;
using TerraScale.MinimalEndpoints.Example.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

// Register application services
builder.Services.AddSingleton<IGreetingService, GreetingService>();
builder.Services.AddSingleton<IUserService, UserService>();

// Register Minimal Endpoints (generated)
builder.Services.AddGeneratedMinimalEndpoints();

var app = builder.Build();

app.UseHttpsRedirection();

// Map Minimal Endpoints (generated)
app.MapGeneratedMinimalEndpoints();

app.Run();