using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TmfApiClients.ServiceCatalogManagement.v4;
using TmfApiClients.ServiceInventoryManagement.v4;
using VFZ.CxO.MigrationTool.Application.Configuration;
using VFZ.CxO.MigrationTool.Application.Infrastructure;
using VFZ.CxO.MigrationTool.Application.Models.Neo;
using VFZ.CxO.MigrationTool.Application.Transformers;

namespace VFZ.CxO.MigrationTool.Application.Migration;

// Reads a NEO L2FTTHOffNet export file and bulk imports the resulting services into CxO (no resources).
public class L2OffNetMigrationRunner(
    NeoJsonLoader loader,
    L2OffNetSpecificationProvider specificationProvider,
    IServiceInventoryManagement4ApiClient serviceClient,
    IOptions<MigrationOptions> options,
    ILogger<L2OffNetMigrationRunner> logger
)
{
    public async Task<MigrationSummary> RunAsync(
        string sourcePath,
        bool? dryRunOverride,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Loading L2FTTHOffNet export from {SourcePath}", sourcePath);

        var neoServices = new List<NeoService>();
        await foreach (var neoService in loader.LoadAsync(sourcePath, cancellationToken))
        {
            neoServices.Add(neoService);
        }

        logger.LogInformation("Loaded {Count} L2FTTHOffNet services from source file", neoServices.Count);

        var dryRun = dryRunOverride ?? options.Value.DryRun;

        if (neoServices.Count == 0 || dryRun)
        {
            if (dryRun)
            {
                logger.LogInformation(
                    "Dry run enabled - {Count} services would be imported; skipping bulk import call",
                    neoServices.Count
                );
            }

            return new MigrationSummary(neoServices.Count, dryRun, false, 0, 0, false, 0, 0);
        }

        var specification = await specificationProvider.GetAsync(cancellationToken);

        var serviceResponse = await serviceClient.BulkImportServicesAsync(
            BuildServicesAsync(neoServices, specification, cancellationToken),
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
            false,
            0,
            0,
            serviceResponse.Success,
            serviceResponse.ServiceCount,
            serviceResponse.RowsAffected
        );
    }

    private static async IAsyncEnumerable<Service> BuildServicesAsync(
        List<NeoService> neoServices,
        ServiceSpecification specification,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        foreach (var neoService in neoServices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return L2OffNetServiceTransformer.Transform(neoService, specification);
            await Task.Yield();
        }
    }
}
