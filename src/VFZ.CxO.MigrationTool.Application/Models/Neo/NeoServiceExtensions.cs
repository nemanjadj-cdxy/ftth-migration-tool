using System.Text.Json;

namespace VFZ.CxO.MigrationTool.Application.Models.Neo;

public static class NeoServiceExtensions
{
    public static string? GetCharacteristic(this NeoService service, string name)
    {
        var characteristic = service.ServiceCharacteristic.FirstOrDefault(c =>
            string.Equals(c.Name, name, StringComparison.Ordinal));

        if (characteristic is null)
        {
            return null;
        }

        return characteristic.Value.ValueKind switch
        {
            JsonValueKind.String => characteristic.Value.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => characteristic.Value.GetRawText(),
        };
    }
}
