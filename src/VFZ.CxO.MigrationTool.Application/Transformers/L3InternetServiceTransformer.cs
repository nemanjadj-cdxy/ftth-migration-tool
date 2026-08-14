using TmfApiClients.ServiceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.FTTH.Domain.Models.Services;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using CatalogServiceSpecification = TmfApiClients.ServiceCatalogManagement.v4.ServiceSpecification;

namespace VFZ.CxO.MigrationTool.Application.Transformers;

// Maps a NEO L3Internet export service into the CxO NAASSvc-L3Internet service: depends-on its
// L2OffNet service (embedded in full) and linked to its 1 ROM_FTTHSubscriber supporting resource.
public static class L3InternetServiceTransformer
{
    public static Service Transform(
        NeoService source,
        string subscriberResourceId,
        L2OffNetLookupEntry l2OffNet,
        Service l2OffNetService,
        CatalogServiceSpecification specification
    )
    {
        var service = new Service
        {
            Id = source.Id,
            State = ServiceStateType.Active,
            Name = NAASSvcL3Internet.Name,
            ServiceSpecification = new ServiceSpecificationRef
            {
                Id = specification.Id!,
                Name = specification.Name,
                Version = specification.Version,
            },
            ServiceRelationship =
            [
                new ServiceRelationship
                {
                    RelationshipType = "depends-on",
                    Service = ToServiceRefOrValue(l2OffNetService),
                },
            ],
            SupportingResource =
            [
                new ResourceRef
                {
                    Id = subscriberResourceId,
                    Name = $"{ROMFTTHSubscriber.Name}_{l2OffNet.UserId}",
                    ReferredType = "LogicalResource",
                },
            ],
        };

        var proxy = service.ToEntityProxy<NAASSvcL3InternetEntityProxy>(specification);
        proxy.FirstBoot = source.GetCharacteristic("firstBoot") ?? "";
        proxy.IsFcc = source.GetCharacteristic("isFcc") ?? "no";
        proxy.InternetStatus = source.GetCharacteristic("internetStatus") ?? "normal";
        proxy.L2ServiceId = source.GetCharacteristic("L2ServiceId") ?? "";
        proxy.NetworkOwner = l2OffNet.NetworkOwner;
        proxy.UserId = l2OffNet.UserId;
        proxy.ServiceGroupId = source.GetCharacteristic("serviceGroupId") ?? "";
        proxy.SourceSystem = SourceSystemMapper.Normalize(source.GetCharacteristic("sourceSystem"));
        proxy.SpeedProfile = source.GetCharacteristic("speedProfile") ?? "";
        proxy.TenantId = source.GetCharacteristic("tenantId") ?? "";

        return service;
    }

    private static ServiceRefOrValue ToServiceRefOrValue(Service service) =>
        new()
        {
            Id = service.Id,
            Name = service.Name,
            ServiceCharacteristic = service.ServiceCharacteristic,
            ServiceSpecification = service.ServiceSpecification,
            State = service.State,
        };
}
