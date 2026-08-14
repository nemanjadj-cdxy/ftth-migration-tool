using System.Text.Json;
using System.Text.Json.Serialization;

namespace VFZ.CxO.MigrationTool.Application.Models.Neo;

public class NeoCharacteristic
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("valueType")]
    public string? ValueType { get; set; }

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}
