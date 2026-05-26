namespace ClimateDashboard.Shared;

using Microsoft.Extensions.DependencyInjection;
using Services;

public static class DependencyInjection
{
  public static IServiceCollection AddSharedServices(this IServiceCollection services)
  {
    services.AddHttpClient<INasaPowerService, NasaPowerService>(client =>
    {
      client.BaseAddress = new Uri("https://power.larc.nasa.gov/");
    });
    return services;
  }
}
