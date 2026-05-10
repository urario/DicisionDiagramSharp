# CodeAnalysis Boundary Design

## Scope

This document defines the data model boundary for the planned `DecisionDiagramSharp.CodeAnalysis`
project. No CodeAnalysis code is implemented in v0.7. This note exists to lock the design
before implementation so that the dependency direction and data types are agreed upon.

---

## Data Types

### `IncludeEdge`

An immutable value type representing a single `#include` relationship between two source files.

```csharp
namespace DecisionDiagramSharp.CodeAnalysis;

public readonly struct IncludeEdge : IEquatable<IncludeEdge>
{
    public string From { get; }
    public string To   { get; }
}
```

- `From` and `To` are file paths (relative or absolute; normalized by the caller).
- Value equality is based on `(From, To)`.
- An `IncludeEdge` is intended to be used as a ZDD variable value: one variable per unique edge.

### `IncludePath`

An ordered, immutable sequence of `IncludeEdge` values representing a single include chain
from a source file to a downstream header.

```csharp
namespace DecisionDiagramSharp.CodeAnalysis;

public sealed class IncludePath
{
    public IReadOnlyList<IncludeEdge> Edges { get; }
    public string Source => Edges[0].From;
    public string Target => Edges[Edges.Count - 1].To;
}
```

- An `IncludePath` with zero edges is invalid and must be rejected at construction time.
- Order is significant: `A -> B -> C` and `C -> B -> A` are different paths.

---

## ZDD Variable Mapping

To encode a set of include paths as a ZDD family:

1. Enumerate all unique `IncludeEdge` values across all paths in the graph.
2. Register each unique `IncludeEdge` as a ZDD variable via `ZddManager.GetOrAddVariable`.
   Use a stable string key such as `$"{edge.From} -> {edge.To}"`.
3. Each `IncludePath` is one set: the ZDD set contains the `VariableId` values for every
   `IncludeEdge` in that path.
4. Build the ZDD family from the set of all include paths using `ZddManager.MakeFamily`.

The resulting ZDD family supports standard ZDD operations:
- `Containing(family, edgeVar)` — paths that use a specific include edge.
- `Union`, `Intersect`, `Difference` — combine or compare path sets.
- `CountSets` — number of distinct include paths.

---

## Allowed Dependency Direction

```
Core <- CodeAnalysis
```

`DecisionDiagramSharp.CodeAnalysis` may reference `DecisionDiagramSharp.Core`.

Forbidden:

```
Core -> CodeAnalysis
```

`Core` must not reference `CodeAnalysis`. The `Core` purity rule (§2.1 of `AGENTS.md`) applies.

`CodeAnalysis` does not depend on `Diagnostics` or `Export`. If code-analysis results need
to be formatted, the caller composes `CodeAnalysis` output with `Diagnostics`/`Export` helpers
at the application layer.

---

## Project Location

When implemented, the project will live at:

```
src/DecisionDiagramSharp.CodeAnalysis/DecisionDiagramSharp.CodeAnalysis.csproj
```

Target framework: `netstandard2.0` (same as `Core`, `Diagnostics`, and `Export`).

---

## v0.7 Scope Note

No `DecisionDiagramSharp.CodeAnalysis` project is created in v0.7.
This document is design-only. Implementation is tracked in backlog tasks CA-001 through CA-005.
