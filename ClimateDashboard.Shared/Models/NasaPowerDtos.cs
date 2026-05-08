namespace ClimateDashboard.Shared.Models;

using System.Text.Json.Serialization;

public record NasaSolarPointResponse(
  [property: JsonPropertyName("properties")]
  NasaSolarProperties Properties,
  [property: JsonPropertyName("geometry")]
  NasaSolarGeometry Geometry
);

public record NasaSolarProperties(
  [property: JsonPropertyName("parameter")]
  Dictionary<string, Dictionary<string, double>> Parameter
);

public record NasaSolarRegionResponse(
  [property: JsonPropertyName("features")]
  List<NasaSolarRegionFeature> Features
);

public record NasaSolarRegionFeature(
  [property: JsonPropertyName("geometry")]
  NasaSolarGeometry Geometry,
  [property: JsonPropertyName("properties")]
  NasaSolarProperties Properties
);

public record NasaSolarGeometry(
  [property: JsonPropertyName("coordinates")]
  List<double> Coordinates
);
