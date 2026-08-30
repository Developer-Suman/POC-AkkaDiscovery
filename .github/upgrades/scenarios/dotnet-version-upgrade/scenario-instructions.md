# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Strategy
**Selected**: All-At-Once
**Rationale**: 4 projects, all on net10.0, all SDK-style, no incompatible packages, and no API migration issues.

### Execution Constraints
- Apply project and package alignment changes in one atomic pass across all projects.
- Run dependency restore after project/package updates.
- Validate full solution build after the atomic pass and fix all warnings/errors.
- Run tests only after build succeeds for the full solution.

## Source Control
- **Source Branch**: 1-works-on-akkadiscovery-on-k8s
- **Working Branch**: dotnet-version-upgrade
- **Commit Strategy**: Single Commit at End
- **Branch Sync**: Auto (Merge)

## User Preferences
### Technical Preferences
- Add `.NETCoreApp,Version=v6.0` support via `TargetFrameworks` and re-run NuGet restore.
- Clear all `obj`/`bin` folders before running NuGet restore.
