# v0.2 Release Notes (Draft)

## Summary

v0.2 delivers the BDD foundation alongside the existing ZDD foundation.

## Included

- Core (`netstandard2.0`)
  - `Bdd` typed handle
  - `BddManager`
  - BDD terminals (`False`, `True`)
  - `Var`, `Not`, `And`, `Or`, `Xor`, `Ite`
  - `Evaluate`, `IsSatisfiable`
  - `Implies`, `Equivalent`
  - `Restrict`, `Exists`, `ForAll`
  - `CountModels`, bounded `EnumerateModels`
  - BDD validation, manager ownership checks, and size/enumeration limits
- Diagnostics (`netstandard2.0`)
  - BDD DOT output
  - node table
  - truth table
  - model table
  - statistics table
- Export (`netstandard2.0`)
  - BDD diagnostics are exportable through CSV, Markdown, and AsciiDoc table exporters
- Samples (`net8.0`)
  - `Bdd.FeatureFlags`
- Benchmarks (`net8.0`)
  - BDD `Ite`
  - BDD truth-table generation

## Verification Snapshot

- `dotnet build DecisionDiagramSharp.slnx -v minimal`: success (0 warnings, 0 errors)
- `dotnet test DecisionDiagramSharp.slnx -v minimal`: success (Core 29, Diagnostics 6, Export 4 tests passed)
- `dotnet run --project samples/Bdd.FeatureFlags/Bdd.FeatureFlags.csproj`: success
- `dotnet run -c Release --project benchmarks/DecisionDiagramSharp.Benchmarks/DecisionDiagramSharp.Benchmarks.csproj -- --list flat`: listed BDD and ZDD benchmarks

Coverage artifacts:

- `tests/DecisionDiagramSharp.Core.Tests/TestResults/core-coverage.xml`: Core Line 99.73%, Branch 96.83%, Method 100%
- `tests/DecisionDiagramSharp.Diagnostics.Tests/TestResults/diagnostics-coverage.xml`: Diagnostics Line 99.13%, Branch 100%
- `tests/DecisionDiagramSharp.Export.Tests/TestResults/export-coverage.xml`: Export Line 100%, Branch 91.66%, Method 100%

## Known Gaps

- Unified `DecisionDiagramManager` remains v0.3 scope.
- MTBDD and ZMTBDD remain future scope after v0.3.
- CodeAnalysis remains future scope while existing diagram foundations are refined unless reprioritized.
- Complement edges and dynamic variable reordering are intentionally not implemented.
