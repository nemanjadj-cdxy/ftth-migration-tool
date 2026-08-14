using TmfApiClients.ResourceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using CatalogResourceSpecification = TmfApiClients.ResourceCatalogManagement.v4.ResourceSpecification;
using VFZ.CxO.MigrationTool.Application.Models.Neo;

namespace VFZ.CxO.MigrationTool.Application.Transformers;

// Maps a NEO XGSPON export service into the CxO ROM_FTTHSubscriber resource.
public static class FtthSubscriberResourceTransformer
{
    public static LogicalResource TransformFromXgspon(
        NeoService xgspon,
        string resourceId,
        CatalogResourceSpecification specification
    )
    {
        var serialNumber = xgspon.GetCharacteristic("serialNumber") ?? "";

        var resource = new LogicalResource
        {
            Id = resourceId,
            Name = $"{ROMFTTHSubscriber.Name}_{serialNumber}",
            ResourceSpecification = new ResourceSpecificationRef
            {
                Id = specification.Id!,
                Name = specification.Name,
                Version = specification.Version,
                ReferredType = "LogicalResourceSpecification",
            },
        };

        var proxy = resource.ToEntityProxy<ROMFTTHSubscriberEntityProxy>(specification);
        proxy.FirstBoot = "NONE";
        proxy.FocPrefix = "fmc_";
        proxy.InternetStatus = xgspon.GetCharacteristic("internetStatus") ?? "normal";
        proxy.IsFoc = xgspon.GetCharacteristic("isFoc") ?? "no";
        proxy.NetworkOwner = "xgspon";
        proxy.ServiceGroupId = "0";
        proxy.SpeedProfile = xgspon.GetCharacteristic("speedProfile") ?? "";
        proxy.UserId = serialNumber;
        proxy.ApplyDefaultCharacteristicValues();

        return resource;
    }
}
