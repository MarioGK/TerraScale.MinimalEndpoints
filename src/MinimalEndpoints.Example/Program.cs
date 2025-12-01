using MinimalEndpoints.Example.Services;
using TerraScale.MinimalEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

// Register application services
builder.Services.AddSingleton<IGreetingService, GreetingService>();
builder.Services.AddSingleton<IUserService, UserService>();

// Register Minimal Endpoints
builder.Services.AddMinimalEndpoints();

var app = builder.Build();

app.UseHttpsRedirection();

// Map Minimal Endpoints
app.MapMinimalEndpoints();

app.Run();