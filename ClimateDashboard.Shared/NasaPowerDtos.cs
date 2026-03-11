namespace ClimateDashboard.Api.Models;

using System.Text.Json.Serialization;

public record NasaResponse(
    [property: JsonPropertyName("properties")]
    NasaProperties Properties
);

public record NasaProperties(
    [property: JsonPropertyName("parameter")]
    Dictionary<string, Dictionary<string, double>> Parameter
);
