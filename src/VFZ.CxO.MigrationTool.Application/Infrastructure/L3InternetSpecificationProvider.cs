using TmfApiClients.ResourceCatalogManagement.v4;
using TmfApiClients.ServiceCatalogManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.FTTH.Domain.Models.Services;

namespace VFZ.CxO.MigrationTool.Application.Infrastructure;

public sealed record L3InternetSpecifications(
    ServiceSpecification L3Internet,
    ServiceSpecification L2OffNet,
    ResourceSpecification Subscriber
);

// Fetches and caches the catalog specifications needed to migrate L3Internet services once per process.
public class L3InternetSpecificationProvider(
    IResourceCatalogManagement4ApiClient resourceCatalogClient,
    IServiceCatalogManagement4ApiClient serviceCatalogClient,
    L2OffNetSpecificationProvider l2OffNetSpecificationProvider
)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private L3InternetSpecifications? _specifications;

    public async Task<L3InternetSpecifications> GetAsync(CancellationToken cancellationToken)
    {
        if (_specifications is not null)
        {
            return _specifications;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _specifications ??= new L3InternetSpecifications(
                await serviceCatalogClient.GetServiceSpecificationAsync(NAASSvcL3Internet.Id, cancellationToken),
                await l2OffNetSpecificationProvider.GetAsync(cancellationToken),
                await resourceCatalogClient.GetResourceSpecificationAsync(ROMFTTHSubscriber.Id, cancellationToken)
            );

            return _specifications;
        }
        finally
        {
            _lock.Release();
        }
    }
}
