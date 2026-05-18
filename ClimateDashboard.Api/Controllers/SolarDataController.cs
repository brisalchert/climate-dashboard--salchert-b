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
  public async Task<IActionResult> GetRegionData(double lonMin, double lonMax, double latMin, double latMax,
    DateTime date)
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation(
        "Fetching solar region data for LonMin: {LonMin}, LonMax{LonMax}, LatMin: {LatMin}, LatMax: {LatMax}, Date: {Date}",
        lonMin, lonMax, latMin, latMax, date.ToString("yyyy-MM-dd"));
    }

    try
    {
      var features = await nasaPowerService.GetNasaSolarRegionAsync(lonMin, lonMax, latMin, latMax, date);
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
