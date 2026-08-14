using TmfApiClients.ServiceCatalogManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Services;

namespace VFZ.CxO.MigrationTool.Application.Infrastructure;

// Fetches and caches the NaaSSvc-L2FTTHOffNet catalog specification once per process.
public class L2OffNetSpecificationProvider(IServiceCatalogManagement4ApiClient serviceCatalogClient)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ServiceSpecification? _specification;

    public async Task<ServiceSpecification> GetAsync(CancellationToken cancellationToken)
    {
        if (_specification is not null)
        {
            return _specification;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _specification ??= await serviceCatalogClient.GetServiceSpecificationAsync(
                NaaSSvcL2FTTHOffNet.Id,
                cancellationToken
            );

            return _specification;
        }
        finally
        {
            _lock.Release();
        }
    }
}
