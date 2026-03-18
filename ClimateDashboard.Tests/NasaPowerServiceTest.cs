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
  public async Task GetSolarIntensityAsync_ReturnsCorrectValue_WhenApiSucceeds()
  {
    // Arrange
    var mockHandler = new Mock<HttpMessageHandler>();

    // We simulate the NASA JSON response structure
    var fakeResponse = new NasaResponse(
      new NasaProperties(new Dictionary<string, Dictionary<string, double>>
      {
        ["ALLSKY_SFC_SW_DWN"] = new() { ["20230101"] = 5.5 }
      })
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
    var result = await service.GetSolarIntensityAsync(0, 0);

    // Assert
    result.Should().Be(5.5);
  }
}
