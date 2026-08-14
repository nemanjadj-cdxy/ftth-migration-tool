using TmfApiClients.ResourceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using CatalogResourceSpecification = TmfApiClients.ResourceCatalogManagement.v4.ResourceSpecification;

namespace VFZ.CxO.MigrationTool.Application.Transformers;

// Maps a NEO XGSPON export service into the CxO ROM_FTTHOntIntent resource.
public static class OntIntentResourceTransformer
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
            Name = $"{ROMFTTHOntIntent.Name}_{serialNumber}",
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

        var proxy = resource.ToEntityProxy<ROMFTTHOntIntentEntityProxy>(specification);
        proxy.SerialNumber = serialNumber;
        proxy.FiberName = fiberName;
        proxy.IsFoc = xgspon.GetCharacteristic("isFoc") ?? "no";
        proxy.RegId = xgspon.GetCharacteristic("regId") ?? "";
        proxy.ProvisioningState = string.IsNullOrEmpty(fiberName)
            ? "preprovisioned"
            : "provisioned";
        proxy.ApplyDefaultCharacteristicValues();

        return resource;
    }
}
