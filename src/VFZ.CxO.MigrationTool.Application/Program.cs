using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using Serilog;
using VFZ.CxO.MigrationTool.Application.Endpoints;
using VFZ.CxO.MigrationTool.Application.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProjectSpecific(builder.Configuration);

builder.Services.AddOpenApi();

builder.Host.UseSerilog(
    (context, configuration) => configuration.ReadFrom.Configuration(context.Configuration)
);

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapMigrationEndpoints();
app.MapHealthChecks("/health");

await app.RunAsync();
