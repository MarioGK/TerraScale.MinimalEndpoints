using TerraScale.MinimalEndpoints;
// Use the generated registration helpers for this assembly
using TerraScale.MinimalEndpoints.Generated_TerraScale_MinimalEndpoints_Example;
using TerraScale.MinimalEndpoints.Example.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.OpenApi;

namespace TerraScale.MinimalEndpoints.Example;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi("v1", options =>
        {
            options.ShouldInclude = _ => true;
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                if (document.Components == null)
                {
                    document.Components = new OpenApiComponents();
                }
                var components = document.Components!;
                if (components.SecuritySchemes == null)
                {
                    components.SecuritySchemes = new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
                }
                components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });
                return Task.CompletedTask;
            });
        });
        builder.Services.AddAntiforgery();

        // Add Auth services
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie("Cookies", options => {
            options.Events.OnRedirectToLogin = context => {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context => {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            };
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "Test",
                ValidAudience = "Test",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-for-tests-1234567890")),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Cookies", JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        // Register application services
        builder.Services.AddSingleton<IGreetingService, GreetingService>();
        builder.Services.AddSingleton<IUserService, UserService>();

        // Register Minimal Endpoints (generated)
        builder.Services.AddGeneratedMinimalEndpoints();

        var app = builder.Build();

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        // Map OpenAPI
        app.MapOpenApi();

        // Map Minimal Endpoints (generated)
        app.MapGeneratedMinimalEndpoints();

        app.Run();
    }
}
