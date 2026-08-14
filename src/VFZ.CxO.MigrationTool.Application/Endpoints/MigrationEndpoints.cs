using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VFZ.CxO.MigrationTool.Application.Migration;

namespace VFZ.CxO.MigrationTool.Application.Endpoints;

public static class MigrationEndpoints
{
    public static IEndpointRouteBuilder MapMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/migration").WithTags("Migration");

        group
            .MapPost(
                "/xgspon/import",
                async (
                    HttpRequest request,
                    [FromQuery] string? sourcePath,
                    [FromQuery] bool? dryRun,
                    XgsponMigrationRunner runner,
                    CancellationToken cancellationToken
                ) =>
                {
                    var file = request.HasFormContentType ? (await request.ReadFormAsync(cancellationToken)).Files["file"] : null;

                    if (file is null && string.IsNullOrWhiteSpace(sourcePath))
                    {
                        return Results.BadRequest(
                            "Provide either a 'file' upload (multipart/form-data) or a 'sourcePath' query parameter."
                        );
                    }

                    string? tempFile = null;
                    string path;

                    if (file is not null)
                    {
                        tempFile = Path.Combine(Path.GetTempPath(), $"neo-xgspon-{Guid.NewGuid():N}.json");
                        await using var stream = File.Create(tempFile);
                        await file.CopyToAsync(stream, cancellationToken);
                        path = tempFile;
                    }
                    else
                    {
                        path = sourcePath!;
                    }

                    if (!File.Exists(path))
                    {
                        return Results.NotFound($"Source file not found: {path}");
                    }

                    try
                    {
                        var summary = await runner.RunAsync(path, dryRun, cancellationToken);
                        return Results.Ok(summary);
                    }
                    finally
                    {
                        if (tempFile is not null)
                        {
                            File.Delete(tempFile);
                        }
                    }
                }
            )
            .WithName("ImportXgsponExport")
            .WithSummary(
                "Imports a NEO XGSPON export file (upload or server-local path) and bulk imports the resulting service + resources into CxO."
            )
            .Produces<MigrationSummary>()
            .DisableAntiforgery();

        group
            .MapPost(
                "/xgspon/cleanup",
                async (
                    [FromQuery] int? batchSize,
                    [FromQuery] int? maxConcurrency,
                    [FromQuery] bool? dryRun,
                    XgsponCleanupRunner runner,
                    CancellationToken cancellationToken
                ) =>
                {
                    // Defaults to a dry run - pass ?dryRun=false to actually delete.
                    var summary = await runner.RunAsync(
                        batchSize ?? 100,
                        dryRun ?? true,
                        maxConcurrency ?? 20,
                        cancellationToken
                    );
                    return Results.Ok(summary);
                }
            )
            .WithName("CleanupXgsponInventory")
            .WithSummary(
                "Pages through the XGSPON service and its 3 resources in the CxO inventory and deletes them (dry run by default)."
            )
            .Produces<CleanupSummary>();

        return app;
    }
}
