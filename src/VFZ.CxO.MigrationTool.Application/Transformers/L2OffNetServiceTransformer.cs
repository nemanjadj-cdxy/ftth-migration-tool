using TmfApiClients.ServiceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Services;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using CatalogServiceSpecification = TmfApiClients.ServiceCatalogManagement.v4.ServiceSpecification;

namespace VFZ.CxO.MigrationTool.Application.Transformers;

// Maps a NEO L2FTTHOffNet export service into the CxO NaaSSvc-L2FTTHOffNet service (no resources).
public static class L2OffNetServiceTransformer
{
    public static Service Transform(NeoService source, CatalogServiceSpecification specification)
    {
        var service = new Service
        {
            Id = source.Id,
            State = ServiceStateType.Active,
            Name = NaaSSvcL2FTTHOffNet.Name,
            ServiceSpecification = new ServiceSpecificationRef
            {
                Id = specification.Id!,
                Name = specification.Name,
                Version = specification.Version,
            },
            ServiceRelationship = [],
        };

        var proxy = service.ToEntityProxy<NaaSSvcL2FTTHOffNetEntityProxy>(specification);
        proxy.NetworkOwner =
            source.GetCharacteristic("networkOwner_output_attr") ?? source.GetCharacteristic("networkOwner") ?? "";
        proxy.SourceSystem = SourceSystemMapper.Normalize(source.GetCharacteristic("sourceSystem"));
        proxy.TenantId = source.GetCharacteristic("tenantId") ?? "";
        proxy.TcpId = source.GetCharacteristic("tcpId") ?? "";
        proxy.UserId = source.GetCharacteristic("userId_output_attr") ?? source.GetCharacteristic("userId") ?? "";
        proxy.ApplyDefaultCharacteristicValues();

        return service;
    }
}
