using Microsoft.Extensions.Logging;
using TmfApiClients.ResourceInventoryManagement.v4;
using TmfApiClients.ServiceInventoryManagement.v4;
using VFZ.CxO.FTTH.Domain.Models.Resources;
using VFZ.CxO.FTTH.Domain.Models.Services;

namespace VFZ.CxO.MigrationTool.Application.Migration;

public sealed record CleanupSummary(
    bool DryRun,
    int ServicesFound,
    int ServicesDeleted,
    int ServicesFailed,
    int ResourcesFound,
    int ResourcesDeleted,
    int ResourcesFailed
);

// Pages through the XGSPON service + its 3 resource specifications and deletes them via the TMF clients.
public class CleanupRunner(
    IServiceInventoryManagement4ApiClient serviceClient,
    IResourceInventoryManagement4ApiClient resourceClient,
    ILogger<CleanupRunner> logger
)
{
    private static readonly string[] ResourceSpecificationIds =
    [
        ROMFTTHOntIntent.Id,
        ROMFTTHL2UserIntent.Id,
        ROMFTTHSubscriber.Id,
    ];

    private static readonly string[] ServiceSpecificationIds =
    [
        NaaSSVCXGSPON.Id,
        NaaSSvcL2FTTHOffNet.Id,
        NAASSvcL3Internet.Id,
    ];

    public async Task<CleanupSummary> RunAsync(
        int batchSize,
        bool dryRun,
        int maxConcurrency,
        CancellationToken cancellationToken
    )
    {
        var (servicesFound, servicesDeleted, servicesFailed) = await CleanupServicesAsync(
            batchSize,
            dryRun,
            maxConcurrency,
            cancellationToken
        );
        var (resourcesFound, resourcesDeleted, resourcesFailed) = await CleanupResourcesAsync(
            batchSize,
            dryRun,
            maxConcurrency,
            cancellationToken
        );

        return new CleanupSummary(
            dryRun,
            servicesFound,
            servicesDeleted,
            servicesFailed,
            resourcesFound,
            resourcesDeleted,
            resourcesFailed
        );
    }

    private async Task<(int Found, int Deleted, int Failed)> CleanupServicesAsync(
        int batchSize,
        bool dryRun,
        int maxConcurrency,
        CancellationToken cancellationToken
    )
    {
        var found = 0;
        var deleted = 0;
        var failed = 0;
        var offset = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await serviceClient.ListServicesAsync(
                new ListServicesQueryParams
                {
                    ServiceSpecificationIds = ServiceSpecificationIds,
                    Limit = batchSize,
                    Offset = offset,
                    Fields = ["id"],
                },
                cancellationToken
            );

            if (page.Items.Length == 0)
            {
                break;
            }

            found += page.Items.Length;
            logger.LogInformation(
                "Service cleanup batch: offset={Offset}, batchCount={BatchCount}, totalCount={TotalCount}, dryRun={DryRun}",
                offset,
                page.Items.Length,
                page.TotalCount,
                dryRun
            );

            if (dryRun)
            {
                offset += batchSize;
                if (offset >= page.TotalCount)
                {
                    break;
                }

                continue;
            }

            var (batchDeleted, batchFailed) = await DeleteBatchAsync(
                page.Items,
                item => item.Id!,
                (id, ct) => serviceClient.DeleteServiceAsync(id, ct),
                maxConcurrency,
                cancellationToken
            );
            deleted += batchDeleted;
            failed += batchFailed;

            // deleted items shift subsequent rows down, so the next page is fetched at the same offset
            if (page.Items.Length < batchSize)
            {
                break;
            }
        }

        return (found, deleted, failed);
    }

    private async Task<(int Found, int Deleted, int Failed)> CleanupResourcesAsync(
        int batchSize,
        bool dryRun,
        int maxConcurrency,
        CancellationToken cancellationToken
    )
    {
        var found = 0;
        var deleted = 0;
        var failed = 0;
        var offset = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await resourceClient.ListResourcesAsync(
                new ListResourcesQueryParams
                {
                    ResourceSpecIds = ResourceSpecificationIds,
                    Limit = batchSize,
                    Offset = offset,
                    Fields = ["id"],
                },
                cancellationToken
            );

            if (page.Items.Length == 0)
            {
                break;
            }

            found += page.Items.Length;
            logger.LogInformation(
                "Resource cleanup batch: offset={Offset}, batchCount={BatchCount}, totalCount={TotalCount}, dryRun={DryRun}",
                offset,
                page.Items.Length,
                page.TotalCount,
                dryRun
            );

            if (dryRun)
            {
                offset += batchSize;
                if (offset >= page.TotalCount)
                {
                    break;
                }

                continue;
            }

            var (batchDeleted, batchFailed) = await DeleteBatchAsync(
                page.Items,
                item => item.Id!,
                (id, ct) => resourceClient.DeleteResourceAsync(id, ct),
                maxConcurrency,
                cancellationToken
            );
            deleted += batchDeleted;
            failed += batchFailed;

            if (page.Items.Length < batchSize)
            {
                break;
            }
        }

        return (found, deleted, failed);
    }

    // Deletes a batch concurrently (bounded by maxConcurrency) instead of one request at a time.
    private async Task<(int Deleted, int Failed)> DeleteBatchAsync<T>(
        IReadOnlyList<T> items,
        Func<T, string> getId,
        Func<string, CancellationToken, Task> deleteAsync,
        int maxConcurrency,
        CancellationToken cancellationToken
    )
    {
        var deleted = 0;
        var failed = 0;
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = items.Select(async item =>
        {
            var id = getId(item);
            await gate.WaitAsync(cancellationToken);
            try
            {
                await deleteAsync(id, cancellationToken);
                Interlocked.Increment(ref deleted);
            }
            catch (Exception e)
            {
                Interlocked.Increment(ref failed);
                logger.LogWarning(e, "Failed to delete {Id}", id);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        return (deleted, failed);
    }
}
