using System.Text.Json.Serialization;

namespace VFZ.CxO.MigrationTool.Application.Models.Neo;

// Minimal projection of the NEO/R18 TMF638 service export - only the fields required for migration are mapped.
public class NeoService
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("serviceCharacteristic")]
    public List<NeoCharacteristic> ServiceCharacteristic { get; set; } = [];
}
