# v0.1 Release Notes (Draft)

## Summary

v0.1 delivers a ZDD foundation with diagnostics, table export, samples, and benchmark skeleton.

## Included

- Core (`netstandard2.0`)
  - `VariableId`
  - `VariableTable`
  - ZDD terminals (`Empty`, `Base`)
  - `Zdd` typed handle
  - `ZddManager`
  - `MakeSet`, `MakeFamily`
  - `Union`, `Intersect`, `Difference`
  - `Subset0`, `Subset1`, `Containing`, `NotContaining`, `Change`
  - `ContainsSet`, `CountSets`, bounded `EnumerateSets`
  - validation API and size/enumeration exceptions
- Diagnostics (`netstandard2.0`)
  - ZDD DOT output
  - node table model
  - set-family table model
  - statistics table model
- Export (`netstandard2.0`)
  - CSV exporter
  - Markdown exporter
  - AsciiDoc exporter
- Samples (`net8.0`)
  - `Zdd.SetFamilies`
  - `Zdd.FukashigiCounting`
  - `Export.AllFormats`
- Benchmarks (`net8.0`)
  - `MakeFamily`
  - `Union`
  - `Difference`

## Verification Snapshot

- `dotnet build DecisionDiagramSharp.slnx`: success (0 warnings, 0 errors)
- `dotnet test DecisionDiagramSharp.slnx`: success (Core 18, Diagnostics 2, Export 3 tests passed)
- sample runs: success
- benchmark smoke run: success

Coverage artifacts:

- `tests/DecisionDiagramSharp.Core.Tests/TestResults/core-coverage.xml`
- `tests/DecisionDiagramSharp.Diagnostics.Tests/TestResults/diagnostics-coverage.xml`
- `tests/DecisionDiagramSharp.Export.Tests/TestResults/export-coverage.xml`

## Known Gaps

- BDD and CodeAnalysis are intentionally out of v0.1 scope.
