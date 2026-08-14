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

public sealed record MigrationSummary(
    int ServicesLoaded,
    bool DryRun,
    bool ResourceImportSuccess,
    int ResourceCount,
    int ResourceRowsAffected,
    bool ServiceImportSuccess,
    int ServiceCount,
    int ServiceRowsAffected
);

// Reads a NEO XGSPON export file and bulk imports the resulting service + resources into CxO.
public class XgsponMigrationRunner(
    NeoJsonLoader loader,
    XgsponSpecificationProvider specificationProvider,
    IResourceInventoryManagement4ApiClient resourceClient,
    IServiceInventoryManagement4ApiClient serviceClient,
    IOptions<MigrationOptions> options,
    ILogger<XgsponMigrationRunner> logger
)
{
    public async Task<MigrationSummary> RunAsync(
        string sourcePath,
        bool? dryRunOverride,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Loading XGSPON export from {SourcePath}", sourcePath);

        var neoServices = new List<NeoService>();
        await foreach (var neoService in loader.LoadAsync(sourcePath, cancellationToken))
        {
            neoServices.Add(neoService);
        }

        logger.LogInformation("Loaded {Count} XGSPON services from source file", neoServices.Count);

        var dryRun = dryRunOverride ?? options.Value.DryRun;

        if (neoServices.Count == 0 || dryRun)
        {
            if (dryRun)
            {
                logger.LogInformation(
                    "Dry run enabled - {Count} services would produce {ResourceCount} resources; skipping bulk import calls",
                    neoServices.Count,
                    neoServices.Count * 3
                );
            }

            return new MigrationSummary(neoServices.Count, dryRun, false, 0, 0, false, 0, 0);
        }

        var specifications = await specificationProvider.GetAsync(cancellationToken);
        var resourceIdsByServiceId = neoServices.ToDictionary(s => s.Id, _ => XgsponResourceIds.Generate());

        var resourceResponse = await resourceClient.BulkImportResourcesAsync(
            BuildResourcesAsync(neoServices, resourceIdsByServiceId, specifications, cancellationToken),
            cancellationToken
        );

        logger.LogInformation(
            "Resource bulk import completed: success={Success}, resourceCount={ResourceCount}, rowsAffected={RowsAffected}",
            resourceResponse.Success,
            resourceResponse.ResourceCount,
            resourceResponse.RowsAffected
        );

        var serviceResponse = await serviceClient.BulkImportServicesAsync(
            BuildServicesAsync(neoServices, resourceIdsByServiceId, specifications, cancellationToken),
            cancellationToken
        );

        logger.LogInformation(
            "Service bulk import completed: success={Success}, serviceCount={ServiceCount}, rowsAffected={RowsAffected}",
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
            serviceResponse.Success,
            serviceResponse.ServiceCount,
            serviceResponse.RowsAffected
        );
    }

    private static async IAsyncEnumerable<Resource> BuildResourcesAsync(
        List<NeoService> neoServices,
        Dictionary<string, XgsponResourceIds> resourceIdsByServiceId,
        XgsponSpecifications specifications,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (var neoService in neoServices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ids = resourceIdsByServiceId[neoService.Id];

            yield return OntIntentResourceTransformer.Transform(neoService, ids.OntIntentId, specifications.OntIntent);
            yield return L2UserIntentResourceTransformer.Transform(
                neoService,
                ids.L2UserIntentId,
                specifications.L2UserIntent
            );
            yield return FtthSubscriberResourceTransformer.TransformFromXgspon(
                neoService,
                ids.SubscriberId,
                specifications.Subscriber
            );

            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Service> BuildServicesAsync(
        List<NeoService> neoServices,
        Dictionary<string, XgsponResourceIds> resourceIdsByServiceId,
        XgsponSpecifications specifications,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (var neoService in neoServices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ids = resourceIdsByServiceId[neoService.Id];

            yield return XgsponServiceTransformer.Transform(neoService, ids, specifications.Xgspon);

            await Task.Yield();
        }
    }
}
