namespace ClimateDashboard.Api.Services;

using Models;

public class NasaPowerService(HttpClient httpClient)
{
  public async Task<double> GetSolarIntensityAsync(double latitude, double longitude)
  {
    var url = $"https://power.larc.nasa.gov/api/data?latitude={latitude}&longitude={longitude}";
    var data = await httpClient.GetFromJsonAsync<NasaResponse>(url);

    var intensity = data?.Properties.Parameter["ALLSKY_SFC_SW_DWN"].Values.FirstOrDefault();

    return intensity ?? 0.0;
  }
}
