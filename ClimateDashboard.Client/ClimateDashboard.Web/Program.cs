using ClimateDashboard.Shared.Services;
using ClimateDashboard.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveWebAssemblyComponents();

// Register NasaPowerService
builder.Services.AddHttpClient<NasaPowerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseWebAssemblyDebugging();
}
else
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/api/solar/point",
  async (double latitude, double longitude, DateTime date, NasaPowerService nasaService, ILogger<Program> logger) =>
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation("Fetching solar data for Lat: {Lat}, Lon: {Lon}, Date: {Date}",
        latitude, longitude, date.ToString("yyyy-MM-dd"));
    }

    try
    {
      var intensity = await nasaService.GetSolarPointAsync(latitude, longitude, date);
      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Successfully retrieved intensity: {Intensity}", intensity);
      }

      return Results.Ok(intensity);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while calling NASA API for Date: {Date}", date);

      return Results.Problem("Failed to retrieve data from NASA. Check server logs for details.");
    }
  });

app.MapGet("/api/solar/region",
  async (double latMin, double latMax, double lonMin, double lonMax, DateTime date,
    NasaPowerService nasaService, ILogger<Program> logger) =>
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation(
        "Fetching solar region data for LatMin: {LatMin}, LatMax: {LatMax}, LonMin: {LonMin}, LonMax{LonMax}, Date: {Date}",
        latMin, latMax, lonMin, lonMax, date.ToString("yyyy-MM-dd"));
    }

    try
    {
      var features = await nasaService.GetNasaSolarRegionAsync(latMin, latMax, lonMin, lonMax, date);
      if (logger.IsEnabled(LogLevel.Information))
      {
        logger.LogInformation("Successfully retrieved features: {Features}", features);
      }

      return Results.Ok(features);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while calling NASA API for Date: {Date}", date);

      return Results.Problem("Failed to retrieve data from NASA. Check server logs for details.");
    }
  });

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveWebAssemblyRenderMode()
  .AddAdditionalAssemblies(typeof(ClimateDashboard.UI._Imports).Assembly);

app.Run();
