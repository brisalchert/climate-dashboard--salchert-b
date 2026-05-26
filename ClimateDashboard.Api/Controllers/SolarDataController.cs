namespace ClimateDashboard.Api.Controllers;

using System.Text.Json;
using Shared.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SolarDataController(
  INasaPowerService nasaPowerService,
  ILogger<SolarDataController> logger
) : ControllerBase
{
  // GET request for solar point data
  [HttpGet("point")]
  public async Task<IActionResult> GetPointData(double lon, double lat, DateTime date)
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation("Fetching solar data for Lon: {Lon}, Lat: {Lat}, Date: {Date}",
        lon, lat, date.ToString("yyyy-MM-dd"));
    }

    try
    {
      var feature = await nasaPowerService.GetSolarPointAsync(lon, lat, date);
      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Successfully retrieved feature");
      }

      return Ok(feature);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while calling NASA API for Date: {Date}", date);

      return Problem("Failed to retrieve data from NASA. Check server logs for details.");
    }
  }

  [HttpGet("region")]
  public async Task GetLargeRegion(
    [FromQuery] double lonMin, [FromQuery] double lonMax,
    [FromQuery] double latMin, [FromQuery] double latMax, [FromQuery] DateTime date)
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation(
        "Fetching solar region data for LonMin: {LonMin}, LonMax{LonMax}, LatMin: {LatMin}, LatMax: {LatMax}, Date: {Date}",
        lonMin, lonMax, latMin, latMax, date.ToString("yyyy-MM-dd"));
    }

    Response.ContentType = "text/event-stream";
    Response.Headers.Append("Cache-Control", "no-cache");
    Response.Headers.Append("Connection", "keep-alive");

    const double step = 10.0;
    var concurrencySemaphore = new SemaphoreSlim(3, 3);
    var streamSemaphore = new SemaphoreSlim(1, 1);
    var tasks = new List<Task>();

    var startLon = Math.Floor(lonMin / 10) * 10;
    var endLon = Math.Ceiling(lonMax / 10) * 10;
    var startLat = Math.Floor(latMin / 10) * 10;
    var endLat = Math.Ceiling(latMax / 10) * 10;

    for (var lon = startLon; lon < endLon; lon += step)
    {
      for (var lat = startLat; lat < endLat; lat += step)
      {
        var currentLon = lon;
        var currentLat = lat;

        tasks.Add(Task.Run(async () =>
        {
          await concurrencySemaphore.WaitAsync();
          try
          {
            await Task.Delay(250);

            var result = await GetStandardizedChunk(currentLon, currentLon + step, currentLat, currentLat + step, date);

            if (result is OkObjectResult { Value: { } value })
            {
              var jsonChunk = JsonSerializer.Serialize(value);
              await streamSemaphore.WaitAsync();
              try
              {
                // Clean, non-blocking asynchronous streaming execution
                await Response.WriteAsync($"data: {jsonChunk}\n\n");
                await Response.Body.FlushAsync();
              }
              finally
              {
                streamSemaphore.Release(); // Free the stream gate for the next chunk
              }
            }
          }
          finally
          {
            concurrencySemaphore.Release();
          }
        }));
      }
    }

    await Task.WhenAll(tasks);
  }

  // GET request for solar region data
  [HttpGet("chunk")]
  public async Task<IActionResult> GetStandardizedChunk(double lonMin, double lonMax, double latMin, double latMax,
    DateTime date)
  {
    // Block external calls to this endpoint
    var isLocal = HttpContext.Connection.RemoteIpAddress == null ||
                  HttpContext.Connection.RemoteIpAddress.Equals(HttpContext.Connection.LocalIpAddress);

    if (!isLocal)
    {
      return Forbid("Direct access to sub-chunks is restricted.");
    }

    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation(
        "Fetching solar chunk data for LonMin: {LonMin}, LonMax{LonMax}, LatMin: {LatMin}, LatMax: {LatMax}, Date: {Date}",
        lonMin, lonMax, latMin, latMax, date.ToString("yyyy-MM-dd"));
    }

    try
    {
      var features = await nasaPowerService.GetNasaSolarRegionAsync(lonMin, lonMax, latMin, latMax, date);
      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Successfully retrieved {Features} features", features.Count);
      }

      return Ok(features);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while calling NASA API for Date: {Date}", date);

      return Problem("Failed to retrieve data from NASA. Check server logs for details.");
    }
  }
}
