# 01-align-framework-and-restore: Align project TFMs/dependencies and run restore

Verify every project in the solution remains aligned to `net10.0`, then apply any needed project-file updates so no project or dependency workflow requires older TFMs (including stray multi-targeting or stale package metadata). Re-run restore across the solution after alignment.

This task covers project file updates, dependency reference consistency checks, and restore validation for the full solution.

## Research Findings

### Projects Affected
- `BatchProcessor/BatchProcessor.csproj` — verified `TargetFramework` is already `net10.0`.
- `EditorService/EditorService.csproj` — verified `TargetFramework` is already `net10.0`.
- `BatchPortal/BatchPortal.Server/BatchPortal.Server.csproj` — verified `TargetFramework` is already `net10.0`.
- `BatchPortal/batchportal.client/batchportal.client.esproj` — verified `TargetFrameworks` is already `net10.0`.

### Assessment Signals
- All 4 projects are SDK-style and already on `net10.0`.
- No package compatibility issues, API incidents, or upgrade blockers were reported.
- No `net6.0` or `.NETCoreApp,Version=v6.0` references were found in project files.

### Files to Modify
- No project file updates required based on current repository state.
- Validation actions required: restore and build/test verification.

### Dependencies & Risks
- Dependency set is already compatible with `net10.0`.
- Primary risk is stale local NuGet/assets state; mitigate by re-running restore and validating full build.

**Done when**: All projects are configured for net10.0 as intended, restore completes successfully for the solution, and no project requests net6.0 targeting.
