# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/VFZ.CxO.MigrationTool.Application/VFZ.CxO.MigrationTool.Application.csproj", "src/VFZ.CxO.MigrationTool.Application/"]
RUN --mount=type=secret,id=nuget_config,target=/root/.nuget/NuGet/NuGet.Config \
    dotnet restore "src/VFZ.CxO.MigrationTool.Application/VFZ.CxO.MigrationTool.Application.csproj" \
    --configfile /root/.nuget/NuGet/NuGet.Config
COPY ["src/VFZ.CxO.MigrationTool.Application/", "src/VFZ.CxO.MigrationTool.Application/"]
WORKDIR "/src/src/VFZ.CxO.MigrationTool.Application"
RUN dotnet build "./VFZ.CxO.MigrationTool.Application.csproj" -c "$BUILD_CONFIGURATION" -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VFZ.CxO.MigrationTool.Application.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VFZ.CxO.MigrationTool.Application.dll"]
