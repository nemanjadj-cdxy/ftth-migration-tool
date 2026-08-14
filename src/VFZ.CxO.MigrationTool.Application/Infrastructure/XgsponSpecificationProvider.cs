using TmfApiClients.ResourceCatalogManagement.v4;
using TmfApiClients.ServiceCatalogManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.FTTH.Domain.Models.Services;

namespace VFZ.CxO.MigrationTool.Application.Infrastructure;

public sealed record XgsponSpecifications(
    ResourceSpecification OntIntent,
    ResourceSpecification L2UserIntent,
    ResourceSpecification Subscriber,
    ServiceSpecification Xgspon
);

// Fetches the catalog specifications needed by the entity proxies once per process and caches them.
public class XgsponSpecificationProvider(
    IResourceCatalogManagement4ApiClient resourceCatalogClient,
    IServiceCatalogManagement4ApiClient serviceCatalogClient
)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private XgsponSpecifications? _specifications;

    public async Task<XgsponSpecifications> GetAsync(CancellationToken cancellationToken)
    {
        if (_specifications is not null)
        {
            return _specifications;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _specifications ??= new XgsponSpecifications(
                await resourceCatalogClient.GetResourceSpecificationAsync(ROMFTTHOntIntent.Id, cancellationToken),
                await resourceCatalogClient.GetResourceSpecificationAsync(
                    ROMFTTHL2UserIntent.Id,
                    cancellationToken
                ),
                await resourceCatalogClient.GetResourceSpecificationAsync(
                    ROMFTTHSubscriber.Id,
                    cancellationToken
                ),
                await serviceCatalogClient.GetServiceSpecificationAsync(NaaSSVCXGSPON.Id, cancellationToken)
            );

            return _specifications;
        }
        finally
        {
            _lock.Release();
        }
    }
}
