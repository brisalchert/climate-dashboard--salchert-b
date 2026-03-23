namespace ClimateDashboard.Shared.Services;

using System.Net.Http.Json;
using System.Text.Json;
using Models;

public class NasaPowerService(HttpClient httpClient)
{
  public async Task<double> GetSolarPointAsync(double latitude, double longitude, DateTime date)
  {
    // Convert datetime to proper format
    var formattedDate = date.ToString("yyyyMMdd");

    // HTTP request for single-point daily downward solar irradiance
    var url = $"https://power.larc.nasa.gov/api/temporal/daily/point" +
              $"?latitude={latitude}&longitude={longitude}" +
              $"&parameters=ALLSKY_SFC_SW_DWN&community=RE&format=JSON" +
              $"&start={formattedDate}&end={formattedDate}";

    // Case-insensitive matching helps prevent 500 errors during deserialization
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    var response = await httpClient.GetAsync(url);
    if (!response.IsSuccessStatusCode)
    {
      var errorContent = await response.Content.ReadAsStringAsync();
      throw new Exception($"NASA API Error: {response.StatusCode} - {errorContent}");
    }

    var data = await response.Content.ReadFromJsonAsync<NasaSolarPointResponse>(options);

    // NASA's daily data returns an object where the key is the date (e.g., "20230101")
    var intensity = data?.Properties.Parameter["ALLSKY_SFC_SW_DWN"].Values.FirstOrDefault();
    return intensity ?? 0.0;
  }

  public async Task<List<SolarPoint>> GetNasaSolarRegionAsync(double latitudeMin,
    double latitudeMax, double longitudeMin, double longitudeMax, DateTime date)
  {
    if (latitudeMin > latitudeMax || longitudeMin > longitudeMax)
    {
      throw new Exception(
        $"Invalid coordinate ranges provided: {latitudeMin}, {longitudeMin} to {latitudeMax}, {longitudeMax}");
    }

    // Convert datetime to proper format
    var formattedDate = date.ToString("yyyyMMdd");

    // HTTP request for region daily downward solar irradiance
    var url = $"https://power.larc.nasa.gov/api/temporal/daily/regional" +
              $"?start={formattedDate}&end={formattedDate}" +
              $"&latitude-min={latitudeMin}&latitude-max={latitudeMax}" +
              $"&longitude-min={longitudeMin}&longitude-max={longitudeMax}" +
              $"&community=ag&parameters=ALLSKY_SFC_SW_DWN&header=true";

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
    return data.Features.Select(feature => new SolarPoint
    {
      Longitude = feature.Geometry.Coordinates[0],
      Latitude = feature.Geometry.Coordinates[1],
      Elevation = feature.Geometry.Coordinates[2],
      Intensity = feature.Properties.Parameter["ALLSKY_SFC_SW_DWN"].Values.FirstOrDefault()
    }).ToList();
  }
}
