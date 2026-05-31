namespace ClimateDashboard.Shared.Services;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Models;

public interface INasaPowerService
{
  Task<SolarPoint> GetSolarPointAsync(double longitude, double latitude, DateTime date);

  Task<List<SolarPoint>> GetNasaSolarRegionAsync(double longitudeMin, double longitudeMax, double latitudeMin,
    double latitudeMax, DateTime date);

  bool IsRegionCached(double longitudeMin, double longitudeMax, double latitudeMin, double latitudeMax, DateTime date);
}

internal class NasaPowerService(
  HttpClient httpClient,
  IMemoryCache cache
) : INasaPowerService
{
  private const string ApiKey = "c9v2ZeKv6h2kaI08hIOYQnAPRcXM1IQCRyR9nbKp";

  public async Task<SolarPoint> GetSolarPointAsync(double longitude, double latitude, DateTime date)
  {
    // Convert datetime to proper format
    var formattedDate = date.ToString("yyyyMMdd");

    // HTTP request for single-point daily downward solar irradiance
    var url = $"https://power.larc.nasa.gov/api/temporal/daily/point" +
              $"?longitude={longitude}&latitude={latitude}" +
              $"&parameters=ALLSKY_SFC_SW_DWN&community=RE&format=JSON" +
              $"&start={formattedDate}&end={formattedDate}" +
              $"&header=true" +
              $"&api_key={ApiKey}";

    // Case-insensitive matching helps prevent 500 errors during deserialization
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var response = await httpClient.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
      var errorContent = await response.Content.ReadAsStringAsync();
      throw new Exception($"NASA API Error: {response.StatusCode} - {errorContent}");
    }

    var data = await response.Content.ReadFromJsonAsync<NasaSolarPointResponse>(options);

    if (data?.Properties == null) return new SolarPoint { Date = formattedDate };

    return new SolarPoint
    {
      Longitude = data.Geometry.Coordinates[0],
      Latitude = data.Geometry.Coordinates[1],
      Elevation = data.Geometry.Coordinates[2],
      Intensity = data.Properties.Parameter["ALLSKY_SFC_SW_DWN"].Values.FirstOrDefault(),
      Date = formattedDate
    };
  }

  public async Task<List<SolarPoint>> GetNasaSolarRegionAsync(double longitudeMin, double longitudeMax,
    double latitudeMin, double latitudeMax, DateTime date)
  {
    if (latitudeMin > latitudeMax || longitudeMin > longitudeMax)
    {
      throw new Exception(
        $"Invalid coordinate ranges provided: {longitudeMin}, {latitudeMin} to {longitudeMax}, {latitudeMax}");
    }

    var cacheKey = $"solar_{longitudeMin}_{longitudeMax}_{latitudeMin}_{latitudeMax}_{date:yyyyMMdd}";

    if (cache.TryGetValue(cacheKey, out List<SolarPoint>? cachedFeatures) && cachedFeatures != null)
    {
      return cachedFeatures;
    }

    // Convert datetime to proper format
    var formattedDate = date.ToString("yyyyMMdd");

    // HTTP request for region daily downward solar irradiance
    var url = $"https://power.larc.nasa.gov/api/temporal/daily/regional" +
              $"?start={formattedDate}&end={formattedDate}" +
              $"&longitude-min={longitudeMin}&longitude-max={longitudeMax}" +
              $"&latitude-min={latitudeMin}&latitude-max={latitudeMax}" +
              $"&community=ag&parameters=ALLSKY_SFC_SW_DWN" +
              $"&header=true" +
              $"&api_key={ApiKey}";

    // Case-insensitive matching helps prevent 500 errors during deserialization
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var response = await httpClient.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
      var errorContent = await response.Content.ReadAsStringAsync();
      throw new Exception($"NASA API Error: {response.StatusCode} - {errorContent}");
    }

    var data = await response.Content.ReadFromJsonAsync<NasaSolarRegionResponse>(options);

    if (data?.Features == null) return [];

    // NASA's daily regional solar data returns coordinates paired with
    var freshFeatures = data.Features.Select(feature => new SolarPoint
    {
      Longitude = feature.Geometry.Coordinates[0],
      Latitude = feature.Geometry.Coordinates[1],
      Elevation = feature.Geometry.Coordinates[2],
      Intensity = feature.Properties.Parameter["ALLSKY_SFC_SW_DWN"].Values.FirstOrDefault(),
      Date = formattedDate
    }).ToList();

    cache.Set(cacheKey, freshFeatures, TimeSpan.FromHours(24));

    return freshFeatures;
  }

  public bool IsRegionCached(double longitudeMin, double longitudeMax, double latitudeMin, double latitudeMax,
    DateTime date)
  {
    var cacheKey = $"solar_{longitudeMin}_{longitudeMax}_{latitudeMin}_{latitudeMax}_{date:yyyyMMdd}";

    return cache.TryGetValue(cacheKey, out _);
  }
}
