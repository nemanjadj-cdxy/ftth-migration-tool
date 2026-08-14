# VFZ.CxO.MigrationTool

Migrates NEO (TMF R18) FTTH exports into the CxO TMF638 (Service Inventory) and TMF639 (Resource
Inventory) via bulk import. See `MIGRATION-PLAN.md` in the workspace root for the full mapping spec.

## Running locally

```
cd src/VFZ.CxO.MigrationTool.Application
dotnet run
```

The app starts as a web API (Scalar UI at `/scalar` in Development) and exposes:

```
POST /api/migration/xgspon/import
  - multipart/form-data upload: field "file" = NEO XGSPON export JSON
  - or ?sourcePath=<path readable by the running process>
  - optional ?dryRun=true to skip the bulk import calls
```

## Docker

```
docker compose up --build
```

Uses `appsettings.Development.json` (localhost TMF API endpoints) by default.
