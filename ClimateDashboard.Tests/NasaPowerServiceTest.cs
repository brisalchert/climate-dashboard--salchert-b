namespace ClimateDashboard.Tests;

using System.Net;
using System.Net.Http.Json;
using Shared.Services;
using Shared.Models;
using FluentAssertions;
using Moq;
using Moq.Protected;

public class NasaPowerServiceTests
{
  [Fact]
  public async Task GetSolarIntensityAsync_ReturnsFullObject_WhenApiSucceeds()
  {
    // Arrange
    var mockHandler = new Mock<HttpMessageHandler>();

    // We simulate the NASA JSON response structure
    var fakeResponse = new NasaSolarPointResponse(
      new NasaSolarProperties(new Dictionary<string, Dictionary<string, double>>
      {
        ["ALLSKY_SFC_SW_DWN"] = new() { ["20230101"] = 5.5 }
      }),
      new NasaSolarGeometry([-89.5, 40.5, 180.0])
    );

    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()
      )
      .ReturnsAsync(new HttpResponseMessage
      {
        StatusCode = HttpStatusCode.OK,
        Content = JsonContent.Create(fakeResponse)
      });

    var httpClient = new HttpClient(mockHandler.Object);
    var service = new NasaPowerService(httpClient);

    // Act
    var result = await service.GetSolarPointAsync(40.5, -89.5, new DateTime(2023, 1, 1));

    // Assert
    result.Should().NotBeNull();
    result.Intensity.Should().Be(5.5);
    result.Latitude.Should().Be(40.5);
    result.Longitude.Should().Be(-89.5);
    result.Elevation.Should().Be(180.0);
  }
}
