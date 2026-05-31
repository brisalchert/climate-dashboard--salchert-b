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
    try
    {
      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Fetching solar data for Lon: {Lon}, Lat: {Lat}, Date: {Date}",
          lon, lat, date.ToString("yyyy-MM-dd"));
      }

      var feature = await nasaPowerService.GetSolarPointAsync(lon, lat, date);

      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Successfully retrieved solar data for Lon: {Lon}, Lat: {Lat}, Date: {Date}",
          lon, lat, date.ToString("yyyy-MM-dd"));
      }

      return Ok(feature);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while calling NASA API for Lon: {Lon}, Lat: {Lat}, Date: {Date}",
        lon, lat, date.ToString("yyyy-MM-dd"));

      return Problem("Failed to retrieve data from NASA. Check server logs for details.");
    }
  }

  [HttpGet("region")]
  public async Task GetLargeRegion(
    [FromQuery] double lonMin, [FromQuery] double lonMax,
    [FromQuery] double latMin, [FromQuery] double latMax, [FromQuery] DateTime date)
  {
    var clientDisconnectedToken = HttpContext.RequestAborted;

    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation(
        "Fetching solar data for LonMin: {LonMin}, LonMax: {LonMax}, LatMin: {LatMin}, LatMax: {LatMax}, Date: {Date}",
        lonMin, lonMax, latMin, latMax, date.ToString("yyyy-MM-dd"));
    }

    Response.ContentType = "text/event-stream";
    Response.Headers.Append("Cache-Control", "no-cache");
    Response.Headers.Append("Connection", "keep-alive");

    const double step = 10.0;
    var concurrencySemaphore = new SemaphoreSlim(1, 1);
    var tasks = new List<Task>();

    var startLon = Math.Floor(lonMin / 10) * 10;
    var endLon = Math.Ceiling(lonMax / 10) * 10;
    var startLat = Math.Floor(latMin / 10) * 10;
    var endLat = Math.Ceiling(latMax / 10) * 10;

    for (var lon = startLon; lon < endLon; lon += step)
    {
      for (var lat = startLat; lat < endLat; lat += step)
      {
        var currentLon = ((lon + 180) % 360 + 360) % 360 - 180;
        var currentLat = lat;

        logger.LogDebug(
          "Fetching chunk for lonMin: {lonMin}, lonMax: {lonMax}, latMin: {LatMin}, latMax: {LatMax}, Date: {Date}",
          currentLon, currentLon + step, currentLat, currentLat + step, date.ToString("yyyy-MM-dd")
        );

        tasks.Add(Task.Run(async () =>
        {
          if (clientDisconnectedToken.IsCancellationRequested) return;

          await concurrencySemaphore.WaitAsync(clientDisconnectedToken);
          try
          {
            if (!nasaPowerService.IsRegionCached(currentLon, currentLon + step, currentLat, currentLat + step, date))
            {
              await Task.Delay(500, clientDisconnectedToken);
            }

            var result = await GetStandardizedChunk(currentLon, currentLon + step, currentLat, currentLat + step, date);

            if (result is OkObjectResult { Value: { } value })
            {
              var jsonChunk = JsonSerializer.Serialize(value);
              await Response.WriteAsync($"data: {jsonChunk}\n\n", clientDisconnectedToken);
              await Response.Body.FlushAsync(clientDisconnectedToken);
            }
          }
          catch (OperationCanceledException)
          {
            logger.LogWarning("Request was cancelled.");
          }
          finally
          {
            concurrencySemaphore.Release();
          }
        }, clientDisconnectedToken));
      }
    }

    await Task.WhenAll(tasks);

    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation(
        "Successfully retrieved solar data for LonMin: {LonMin}, LonMax: {LonMax}, LatMin: {LatMin}, LatMax: {LatMax}, Date: {Date}",
        lonMin, lonMax, latMin, latMax, date.ToString("yyyy-MM-dd"));
    }
  }

  // GET request for solar region data
  [HttpGet("chunk")]
  private async Task<IActionResult> GetStandardizedChunk(double lonMin, double lonMax, double latMin, double latMax,
    DateTime date)
  {
    try
    {
      var features = await nasaPowerService.GetNasaSolarRegionAsync(lonMin, lonMax, latMin, latMax, date);

      return Ok(features);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Error occurred while calling NASA API for for LonMin: {LonMin}, LonMax: {LonMax}, LatMin: {LatMin}, LatMax: {LatMax}, Date: {Date}",
        lonMin, lonMax, latMin, latMax, date.ToString("yyyy-MM-dd"));

      return Problem("Failed to retrieve data from NASA. Check server logs for details.");
    }
  }
}
