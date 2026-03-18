using ClimateDashboard.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS services and define a policy
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowSpecificOrigin",
    policy =>
    {
      policy.WithOrigins("http://localhost:7125")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<NasaPowerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// Enable the CORS policy
app.UseCors("AllowSpecificOrigin");

// Other middleware (HTTPS redirection, etc.)
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapGet("/api/data", async (double latitude, double longitude, NasaPowerService nasaService) =>
{
  // The API project runs the service, which calls NASA's absolute URL
  var intensity = await nasaService.GetSolarIntensityAsync(latitude, longitude);
  return Results.Ok(intensity);
});

app.Run();
