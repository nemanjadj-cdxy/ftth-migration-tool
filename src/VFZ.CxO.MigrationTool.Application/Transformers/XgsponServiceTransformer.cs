using TmfApiClients.ServiceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.FTTH.Domain.Models.Services;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using CatalogServiceSpecification = TmfApiClients.ServiceCatalogManagement.v4.ServiceSpecification;

namespace VFZ.CxO.MigrationTool.Application.Transformers;

// Maps a NEO XGSPON export service into the CxO NaaSSVC XGSPON service, linked to its 3 supporting resources.
public static class XgsponServiceTransformer
{
    public static Service Transform(
        NeoService source,
        XgsponResourceIds resourceIds,
        CatalogServiceSpecification specification
    )
    {
        var serialNumber = source.GetCharacteristic("serialNumber") ?? "";

        var service = new Service
        {
            Id = source.Id,
            State = ServiceStateType.Active,
            Name = $"{NaaSSVCXGSPON.Name}",
            ServiceSpecification = new ServiceSpecificationRef
            {
                Id = specification.Id!,
                Name = specification.Name,
                Version = specification.Version,
            },
            ServiceRelationship = [],
            SupportingResource =
            [
                new ResourceRef
                {
                    Id = resourceIds.OntIntentId,
                    Name = $"{ROMFTTHOntIntent.Name}_{serialNumber}",
                    ReferredType = "LogicalResource",
                },
                new ResourceRef
                {
                    Id = resourceIds.L2UserIntentId,
                    Name = $"{ROMFTTHL2UserIntent.Name}_{serialNumber}",
                    ReferredType = "LogicalResource",
                },
                new ResourceRef
                {
                    Id = resourceIds.SubscriberId,
                    Name = $"{ROMFTTHSubscriber.Name}_{serialNumber}",
                    ReferredType = "LogicalResource",
                },
            ],
        };

        var proxy = service.ToEntityProxy<NaaSSVCXGSPONEntityProxy>(specification);
        proxy.SerialNumber = serialNumber;
        proxy.FiberName = source.GetCharacteristic("fiberName") ?? "";
        proxy.InternetStatus = source.GetCharacteristic("internetStatus") ?? "normal";
        proxy.IsFoc = source.GetCharacteristic("isFoc") ?? "no";
        proxy.Operation = source.GetCharacteristic("operation") ?? "\"\"";
        proxy.RegId = source.GetCharacteristic("regId") ?? "";
        proxy.SourceSystem = SourceSystemMapper.Normalize(source.GetCharacteristic("sourceSystem"));
        proxy.SpeedProfile = source.GetCharacteristic("speedProfile") ?? "";
        proxy.TenantId = source.GetCharacteristic("tenantId") ?? "";

        return service;
    }
}
