using ClimateDashboard.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Controller Support
builder.Services.AddControllers();

// Register NasaPowerService
builder.Services.AddHttpClient<NasaPowerService>();

// Add CORS Policy
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowReactApp", policy =>
  {
    policy.WithOrigins("http://localhost:5173") // Vite's default port
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

var app = builder.Build();

// --- Middleware Pipeline ---

if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage(); // Shows detailed error JSON in development
}
else
{
  app.UseHsts();
}

app.UseHttpsRedirection();

// Use the CORS policy before mapping controllers
app.UseCors("AllowReactApp");

app.UseRouting();

// Map the Controllers
app.MapControllers();

// Final Fallback
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
