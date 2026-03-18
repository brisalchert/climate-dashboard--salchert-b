namespace ClimateDashboard.Shared.Services;

using System.Net.Http.Json;
using System.Text.Json;
using Models;

public class NasaPowerService(HttpClient httpClient)
{
  public async Task<double> GetSolarIntensityAsync(double latitude, double longitude)
  {
    // UPDATED PATH: added /temporal/daily/point
    var url = $"https://power.larc.nasa.gov/api/temporal/daily/point?latitude={latitude}&longitude={longitude}&parameters=ALLSKY_SFC_SW_DWN&community=RE&format=JSON&start=20230101&end=20230101";

    // Case-insensitive matching helps prevent 500 errors during deserialization
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    var response = await httpClient.GetAsync(url);
    if (!response.IsSuccessStatusCode)
    {
      var errorContent = await response.Content.ReadAsStringAsync();
      throw new Exception($"NASA API Error: {response.StatusCode} - {errorContent}");
    }

    var data = await response.Content.ReadFromJsonAsync<NasaResponse>(options);

    // NASA's daily data returns an object where the key is the date (e.g., "20230101")
    var intensity = data?.Properties.Parameter["ALLSKY_SFC_SW_DWN"].Values.FirstOrDefault();
    return intensity ?? 0.0;
  }
}
