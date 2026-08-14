namespace VFZ.CxO.MigrationTool.Application.Models.Neo;

public sealed record L2OffNetLookupEntry(string NetworkOwner, string UserId);

// Builds an in-memory L2ServiceId -> { networkOwner, userId } index from a NEO L2FTTHOffNet export,
// used by the L3Internet transformer to cross-reference networkOwner/userId (per MIGRATION-PLAN.md).
public static class L2OffNetLookup
{
    public static Dictionary<string, L2OffNetLookupEntry> Build(IEnumerable<NeoService> l2OffNetServices) =>
        l2OffNetServices.ToDictionary(
            s => s.Id,
            s => new L2OffNetLookupEntry(
                s.GetCharacteristic("networkOwner_output_attr") ?? s.GetCharacteristic("networkOwner") ?? "",
                s.GetCharacteristic("userId_output_attr") ?? s.GetCharacteristic("userId") ?? ""
            )
        );
}
