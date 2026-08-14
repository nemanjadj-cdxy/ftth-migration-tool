using System.Runtime.CompilerServices;
using System.Text.Json;
using VFZ.CxO.MigrationTool.Application.Models.Neo;

namespace VFZ.CxO.MigrationTool.Application.Infrastructure;

public class NeoJsonLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async IAsyncEnumerable<NeoService> LoadAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);

        await foreach (
            var service in JsonSerializer.DeserializeAsyncEnumerable<NeoService>(stream, Options, cancellationToken)
        )
        {
            if (service is not null)
            {
                yield return service;
            }
        }
    }
}
