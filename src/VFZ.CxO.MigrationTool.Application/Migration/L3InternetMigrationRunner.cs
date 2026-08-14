using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TmfApiClients.ResourceInventoryManagement.v4;
using TmfApiClients.ServiceInventoryManagement.v4;
using VFZ.CxO.MigrationTool.Application.Configuration;
using VFZ.CxO.MigrationTool.Application.Infrastructure;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using VFZ.CxO.MigrationTool.Application.Transformers;

namespace VFZ.CxO.MigrationTool.Application.Migration;

// Reads a NEO L3Internet export file + its L2FTTHOffNet export (used only to build the
// networkOwner/userId lookup and embed the full L2OffNet service in serviceRelationship)
// and bulk imports the resulting services + 1 ROM_FTTHSubscriber resource per service into CxO.
public class L3InternetMigrationRunner(
    NeoJsonLoader loader,
    L3InternetSpecificationProvider specificationProvider,
    IResourceInventoryManagement4ApiClient resourceClient,
    IServiceInventoryManagement4ApiClient serviceClient,
    IOptions<MigrationOptions> options,
    ILogger<L3InternetMigrationRunner> logger
)
{
    public async Task<MigrationSummary> RunAsync(
        string l3SourcePath,
        string l2OffNetSourcePath,
        bool? dryRunOverride,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Loading L2FTTHOffNet lookup export from {SourcePath}", l2OffNetSourcePath);

        var l2OffNetServices = new List<NeoService>();
        await foreach (var neoService in loader.LoadAsync(l2OffNetSourcePath, cancellationToken))
        {
            l2OffNetServices.Add(neoService);
        }

        var l2OffNetById = l2OffNetServices.ToDictionary(s => s.Id);
        var lookup = L2OffNetLookup.Build(l2OffNetServices);

        logger.LogInformation("Loading L3Internet export from {SourcePath}", l3SourcePath);

        var neoServices = new List<NeoService>();
        await foreach (var neoService in loader.LoadAsync(l3SourcePath, cancellationToken))
        {
            neoServices.Add(neoService);
        }

        logger.LogInformation("Loaded {Count} L3Internet services from source file", neoServices.Count);

        var dryRun = dryRunOverride ?? options.Value.DryRun;

        if (neoServices.Count == 0 || dryRun)
        {
            if (dryRun)
            {
                logger.LogInformation(
                    "Dry run enabled - {Count} L3Internet services ({L2Count} L2OffNet services) would produce {ResourceCount} resources; skipping bulk import calls",
                    neoServices.Count,
                    l2OffNetServices.Count,
                    neoServices.Count
                );
            }

            return new MigrationSummary(neoServices.Count, dryRun, false, 0, 0, false, 0, 0);
        }

        var specifications = await specificationProvider.GetAsync(cancellationToken);
        var subscriberIdsByServiceId = neoServices.ToDictionary(s => s.Id, _ => Guid.NewGuid().ToString());

        var resourceResponse = await resourceClient.BulkImportResourcesAsync(
            BuildResourcesAsync(neoServices, subscriberIdsByServiceId, lookup, specifications, cancellationToken),
            cancellationToken
        );

        logger.LogInformation(
            "Resource bulk import completed: success={Success}, resourceCount={ResourceCount}, rowsAffected={RowsAffected}",
            resourceResponse.Success,
            resourceResponse.ResourceCount,
            resourceResponse.RowsAffected
        );

        // L2OffNet must exist in the inventory before L3Internet's "depends-on" serviceRelationship
        // can reference it, so it needs its own bulk import batch ahead of L3Internet's.
        var l2ServiceResponse = await serviceClient.BulkImportServicesAsync(
            BuildL2OffNetServicesAsync(l2OffNetServices, specifications.L2OffNet, cancellationToken),
            cancellationToken
        );

        logger.LogInformation(
            "L2OffNet service bulk import completed: success={Success}, serviceCount={ServiceCount}, rowsAffected={RowsAffected}",
            l2ServiceResponse.Success,
            l2ServiceResponse.ServiceCount,
            l2ServiceResponse.RowsAffected
        );

        var serviceResponse = await serviceClient.BulkImportServicesAsync(
            BuildServicesAsync(neoServices, subscriberIdsByServiceId, lookup, l2OffNetById, specifications, cancellationToken),
            cancellationToken
        );

        logger.LogInformation(
            "L3Internet service bulk import completed: success={Success}, serviceCount={ServiceCount}, rowsAffected={RowsAffected}",
            serviceResponse.Success,
            serviceResponse.ServiceCount,
            serviceResponse.RowsAffected
        );

        return new MigrationSummary(
            neoServices.Count,
            false,
            resourceResponse.Success,
            resourceResponse.ResourceCount,
            resourceResponse.RowsAffected,
            l2ServiceResponse.Success && serviceResponse.Success,
            l2ServiceResponse.ServiceCount + serviceResponse.ServiceCount,
            l2ServiceResponse.RowsAffected + serviceResponse.RowsAffected
        );
    }

    private static async IAsyncEnumerable<Service> BuildL2OffNetServicesAsync(
        List<NeoService> l2OffNetServices,
        TmfApiClients.ServiceCatalogManagement.v4.ServiceSpecification specification,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (var neoService in l2OffNetServices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Transformers.L2OffNetServiceTransformer.Transform(neoService, specification);
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Resource> BuildResourcesAsync(
        List<NeoService> neoServices,
        Dictionary<string, string> subscriberIdsByServiceId,
        Dictionary<string, L2OffNetLookupEntry> lookup,
        L3InternetSpecifications specifications,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (var neoService in neoServices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var l2ServiceId = neoService.GetCharacteristic("L2ServiceId") ?? "";
            var l2OffNet = lookup.GetValueOrDefault(l2ServiceId, new L2OffNetLookupEntry("", ""));

            yield return FtthSubscriberResourceTransformer.TransformFromL3Internet(
                neoService,
                subscriberIdsByServiceId[neoService.Id],
                l2OffNet,
                specifications.Subscriber
            );

            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Service> BuildServicesAsync(
        List<NeoService> neoServices,
        Dictionary<string, string> subscriberIdsByServiceId,
        Dictionary<string, L2OffNetLookupEntry> lookup,
        Dictionary<string, NeoService> l2OffNetById,
        L3InternetSpecifications specifications,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (var neoService in neoServices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var l2ServiceId = neoService.GetCharacteristic("L2ServiceId") ?? "";
            var l2OffNet = lookup.GetValueOrDefault(l2ServiceId, new L2OffNetLookupEntry("", ""));

            var l2OffNetService = l2OffNetById.TryGetValue(l2ServiceId, out var l2OffNetNeoService)
                ? Transformers.L2OffNetServiceTransformer.Transform(l2OffNetNeoService, specifications.L2OffNet)
                : new Service { Id = l2ServiceId, State = ServiceStateType.Active };

            yield return L3InternetServiceTransformer.Transform(
                neoService,
                subscriberIdsByServiceId[neoService.Id],
                l2OffNet,
                l2OffNetService,
                specifications.L3Internet
            );

            await Task.Yield();
        }
    }
}
