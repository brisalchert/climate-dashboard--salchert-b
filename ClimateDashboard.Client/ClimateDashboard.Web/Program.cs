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

app.MapGet("/api/data", async (double latitude, double longitude, NasaPowerService nasaService) =>
{
  // The Web project runs the service, which calls NASA's absolute URL
  var intensity = await nasaService.GetSolarIntensityAsync(latitude, longitude);
  return Results.Ok(intensity);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveWebAssemblyRenderMode()
  .AddAdditionalAssemblies(typeof(ClimateDashboard.UI._Imports).Assembly);

app.Run();
