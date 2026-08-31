# Upgrade Options — NucleusRuleServices

Assessment: 4 projects, all on net10.0, all SDK-style, no package/API compatibility issues.

## Strategy

### Upgrade Strategy
All projects already target modern .NET with no detected migration risks; an atomic pass is the simplest fit.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade or align all projects in one pass with full-solution validation at the end. |
| Top-Down | Upgrade entry-point apps first and temporarily multi-target shared libraries to keep incremental buildability. |
