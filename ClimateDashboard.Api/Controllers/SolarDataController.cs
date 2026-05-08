namespace ClimateDashboard.Api.Controllers;

using Shared.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SolarDataController(
  NasaPowerService nasaPowerService,
  ILogger<SolarDataController> logger
) : ControllerBase
{
  // GET request for solar point data
  [HttpGet("point")]
  public async Task<IActionResult> GetPointData(double lat, double lon, DateTime date)
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation("Fetching solar data for Lat: {Lat}, Lon: {Lon}, Date: {Date}",
        lat, lon, date.ToString("yyyy-MM-dd"));
    }

    try
    {
      var feature = await nasaPowerService.GetSolarPointAsync(lat, lon, date);
      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Successfully retrieved feature: {Feature}", feature);
      }

      return Ok(feature);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while calling NASA API for Date: {Date}", date);

      return Problem("Failed to retrieve data from NASA. Check server logs for details.");
    }
  }

  // GET request for solar region data
  [HttpGet("region")]
  public async Task<IActionResult> GetRegionData(double latMin, double latMax, double lonMin, double lonMax,
    DateTime date)
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation(
        "Fetching solar region data for LatMin: {LatMin}, LatMax: {LatMax}, LonMin: {LonMin}, LonMax{LonMax}, Date: {Date}",
        latMin, latMax, lonMin, lonMax, date.ToString("yyyy-MM-dd"));
    }

    try
    {
      var features = await nasaPowerService.GetNasaSolarRegionAsync(latMin, latMax, lonMin, lonMax, date);
      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Successfully retrieved features: {Features}", features);
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
