using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TmfApiClients;
using VFZ.CxO.MigrationTool.Application.Configuration;
using VFZ.CxO.MigrationTool.Application.Infrastructure;
using VFZ.CxO.MigrationTool.Application.Migration;

namespace VFZ.CxO.MigrationTool.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectSpecific(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<MigrationOptions>()
            .Bind(configuration.GetSection(MigrationOptions.SectionKey));

        services.AddTmfApiClients(configuration);

        services.AddSingleton<NeoJsonLoader>();
        services.AddSingleton<XgsponSpecificationProvider>();
        services.AddSingleton<XgsponMigrationRunner>();
        services.AddSingleton<CleanupRunner>();
        services.AddSingleton<L2OffNetSpecificationProvider>();
        services.AddSingleton<L2OffNetMigrationRunner>();
        services.AddSingleton<L3InternetSpecificationProvider>();
        services.AddSingleton<L3InternetMigrationRunner>();

        return services;
    }
}
