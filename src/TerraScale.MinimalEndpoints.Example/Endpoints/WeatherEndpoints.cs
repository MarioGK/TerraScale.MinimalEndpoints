using Microsoft.AspNetCore.Mvc;
// removed attribute usage; routing and metadata are now declared via IMinimalEndpoint/BaseMinimalApiEndpoint

namespace TerraScale.MinimalEndpoints.Example.Endpoints;

public class WeatherEndpoints : BaseMinimalApiEndpoint
{
    public override string Route => "api/weather";
    public override string HttpMethod => "GET";
    public override string? GroupName => "Weather API";
    /// <summary>
    /// Gets weather information for a city
    /// </summary>
    /// <param name="city">The city name to get weather for</param>
    /// <returns>Weather information for the specified city</returns>
    /// <remarks>
    /// This endpoint provides a simple weather forecast.
    /// Currently returns a static sunny weather for all cities.
    /// </remarks>
    /// <response code="200">Weather information retrieved successfully</response>
    /// <response code="400">Invalid city name provided</response>
    
    [Produces("application/json", "text/plain")]
    public async Task<string> GetWeather([FromQuery] string city)
    {
        await Task.Delay(1); // Simulate async work
        return $"Weather in {city} is Sunny";
    }
}
