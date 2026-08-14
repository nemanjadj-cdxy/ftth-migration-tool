using TmfApiClients.ResourceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using CatalogResourceSpecification = TmfApiClients.ResourceCatalogManagement.v4.ResourceSpecification;

namespace VFZ.CxO.MigrationTool.Application.Transformers;

// Maps a NEO XGSPON export service into the CxO ROM_FTTHL2UserIntent resource.
public static class L2UserIntentResourceTransformer
{
    public static LogicalResource Transform(
        NeoService xgspon,
        string resourceId,
        CatalogResourceSpecification specification
    )
    {
        var serialNumber = xgspon.GetCharacteristic("serialNumber") ?? "";
        var fiberName = xgspon.GetCharacteristic("fiberName") ?? "";

        var resource = new LogicalResource
        {
            Id = resourceId,
            Name = $"{ROMFTTHL2UserIntent.Name}_{serialNumber}",
            ResourceSpecification = new ResourceSpecificationRef
            {
                Id = specification.Id!,
                Name = specification.Name,
                Version = specification.Version,
                ReferredType = "LogicalResourceSpecification",
            },
            ResourceStatus = string.IsNullOrEmpty(fiberName)
                ? ResourceStatusType.Reserved
                : ResourceStatusType.Available,
        };

        var proxy = resource.ToEntityProxy<ROMFTTHL2UserIntentEntityProxy>(specification);
        proxy.SerialNumber = serialNumber;
        proxy.SpeedProfile = xgspon.GetCharacteristic("speedProfile") ?? "";
        proxy.InternetStatus = xgspon.GetCharacteristic("internetStatus") ?? "normal";
        proxy.IsFoc = xgspon.GetCharacteristic("isFoc") ?? "no";
        proxy.ProvisioningState = string.IsNullOrEmpty(fiberName)
            ? "preprovisioned"
            : "provisioned";
        proxy.Operation = "\"\"";
        proxy.ApplyDefaultCharacteristicValues();

        return resource;
    }
}
