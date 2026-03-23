namespace ClimateDashboard.Shared.Models;

using System.Text.Json.Serialization;

public record NasaSolarPointResponse(
  [property: JsonPropertyName("properties")]
  NasaSolarPointProperties Properties
);

public record NasaSolarPointProperties(
  [property: JsonPropertyName("parameter")]
  Dictionary<string, Dictionary<string, double>> Parameter
);

public record NasaSolarRegionResponse(
  [property: JsonPropertyName("features")]
  List<NasaSolarRegionFeature> Features
);

public record NasaSolarRegionFeature(
  [property: JsonPropertyName("geometry")]
  NasaSolarRegionGeometry Geometry,
  [property: JsonPropertyName("properties")]
  NasaSolarPointProperties Properties
);

public record NasaSolarRegionGeometry(
  [property: JsonPropertyName("coordinates")]
  double[] Coordinates
);

public class SolarPoint
{
  public double Latitude { get; init; }
  public double Longitude { get; init; }
  public double Elevation { get; init; }
  public double Intensity { get; init; }
}
