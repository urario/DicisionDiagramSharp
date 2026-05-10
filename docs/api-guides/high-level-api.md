# High-Level API Guide

v0.3 adds a friendlier surface over the existing manager-level APIs. The low-level managers remain available for algorithmic work.

## Unified Manager

`DecisionDiagramManager` is a facade:

```csharp
var dd = new DecisionDiagramManager();

var bddManager = dd.Bdd;
var zddManager = dd.Zdd;
```

The facade does not merge BDD and ZDD semantics. It only makes the entry point easier to discover.

## BDD Operators

BDD values support Boolean-style operators:

```csharp
var a = dd.Bdd.Var("A");
var b = dd.Bdd.Var("B");

var expression = (a & !b) | (a ^ b);
```

The operators preserve manager ownership checks. Combining values from different `BddManager` instances throws `DiagramManagerMismatchException`.

## Diagnostics Extensions

Add:

```csharp
using DecisionDiagramSharp.Diagnostics;
```

Then call:

```csharp
var dot = expression.ToDot();
var truthTable = expression.ToTruthTable();
var models = expression.ToModelTable();
var statistics = expression.ToStatisticsTable();
```

ZDD values support:

```csharp
var dot = family.ToDot();
var sets = family.ToSetFamilyTable();
var statistics = family.ToStatisticsTable();
```

## Export Extensions

Add a reference to `DecisionDiagramSharp.Export`, then call:

```csharp
var markdownTruthTable = expression.ToMarkdownTruthTable();
var csvTruthTable = expression.ToCsvTruthTable();
var asciidocTruthTable = expression.ToAsciiDocTruthTable();
var markdownModels = expression.ToMarkdownModels();
var markdownSetFamily = family.ToMarkdownSetFamily();
var csvSetFamily = family.ToCsvSetFamily();
var asciidocSetFamily = family.ToAsciiDocSetFamily();
```

Export helpers are intentionally outside Core so Core can stay compatible with `netstandard2.0` and independent from formatting concerns.
