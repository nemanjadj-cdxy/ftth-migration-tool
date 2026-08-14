using TmfApiClients.ResourceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using CatalogResourceSpecification = TmfApiClients.ResourceCatalogManagement.v4.ResourceSpecification;

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
            ResourceStatus = ResourceStatusType.Available,
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

    // Maps a NEO L3Internet export service + its L2OffNet lookup entry into the CxO ROM_FTTHSubscriber resource.
    public static LogicalResource TransformFromL3Internet(
        NeoService l3Source,
        string resourceId,
        L2OffNetLookupEntry l2OffNet,
        CatalogResourceSpecification specification
    )
    {
        var resource = new LogicalResource
        {
            Id = resourceId,
            Name = $"{ROMFTTHSubscriber.Name}_{l2OffNet.UserId}",
            ResourceSpecification = new ResourceSpecificationRef
            {
                Id = specification.Id!,
                Name = specification.Name,
                Version = specification.Version,
                ReferredType = "LogicalResourceSpecification",
            },
            ResourceStatus = ResourceStatusType.Available,
        };

        var proxy = resource.ToEntityProxy<ROMFTTHSubscriberEntityProxy>(specification);
        proxy.FirstBoot = l3Source.GetCharacteristic("firstBoot") ?? "";
        proxy.FocPrefix = "fmc_";
        proxy.InternetStatus = l3Source.GetCharacteristic("internetStatus") ?? "normal";
        proxy.IsFoc = l3Source.GetCharacteristic("isFcc") ?? "no";
        proxy.NetworkOwner = l2OffNet.NetworkOwner;
        proxy.ServiceGroupId = l3Source.GetCharacteristic("serviceGroupId") ?? "";
        proxy.SpeedProfile = l3Source.GetCharacteristic("speedProfile") ?? "";
        proxy.UserId = l2OffNet.UserId;
        proxy.ApplyDefaultCharacteristicValues();

        return resource;
    }
}
