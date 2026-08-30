# .NET Version Upgrade Plan

## Overview

**Target**: Standardize all projects and dependencies on .NET 10 and confirm clean restore/build state.
**Scope**: 4 projects (ASP.NET Core server, 2 worker services, 1 client project), all already on net10.0 with no assessment-reported incompatibilities.

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: Small modern solution, all SDK-style, no incompatible packages, and no API migration incidents.

## Tasks

### 01-align-framework-and-restore: Align project TFMs/dependencies and run restore

Verify every project in the solution remains aligned to `net10.0`, then apply any needed project-file updates so no project or dependency workflow requires older TFMs (including stray multi-targeting or stale package metadata). Re-run restore across the solution after alignment.

This task covers project file updates, dependency reference consistency checks, and restore validation for the full solution.

**Done when**: All projects are configured for net10.0 as intended, restore completes successfully for the solution, and no project requests net6.0 targeting.

---

### 02-final-build-validation: Validate full build and tests

Run full-solution build and test validation after restore/alignment work. Address any warnings or errors found in modified projects to ensure the upgraded baseline is clean and stable.

This task confirms the requested standardization is complete from both dependency-resolution and compilation/test perspectives.

**Done when**: Solution build succeeds without warnings/errors in modified projects and all runnable tests pass.
