# DecisionDiagramSharp

Modern C#/.NET library for decision diagrams, including BDD (Binary Decision Diagram), ZDD (Zero-suppressed Decision Diagram), MTBDD (Multi-Terminal Binary Decision Diagram), and ZMTBDD (Zero-suppressed MTBDD).

Current foundations are BDD, ZDD, MTBDD, and ZMTBDD. The next roadmap targets refine the existing implementation, usability, tests, diagnostics/export behavior, samples, and packaging readiness. MDD and ADD / weighted DD are not currently planned.

## Install

NuGet packaging is planned before the first packaged preview release.

## BDD Quick Start

```csharp
using DecisionDiagramSharp;
using System.Collections.Generic;

var dd = new DecisionDiagramManager();
var a = dd.Bdd.GetOrAddVariable("A");
var b = dd.Bdd.GetOrAddVariable("B");

var f = dd.Bdd.Var(a) & !dd.Bdd.Var(b);

Console.WriteLine(dd.Bdd.CountModels(f));
Console.WriteLine(dd.Bdd.Evaluate(
    f,
    new Dictionary<VariableId, bool>
    {
        { a, true },
        { b, false }
    }));
```

## ZDD Quick Start

```csharp
using DecisionDiagramSharp;

var dd = new DecisionDiagramManager();
var a = dd.Zdd.GetOrAddVariable("A");
var b = dd.Zdd.GetOrAddVariable("B");

var family = dd.Zdd.MakeFamily(
    new[]
    {
        new[] { a },
        new[] { a, b }
    });

var containingA = dd.Zdd.Containing(family, a);
Console.WriteLine(dd.Zdd.CountSets(containingA));
```

## MTBDD Quick Start

```csharp
using DecisionDiagramSharp;
using System.Collections.Generic;

var dd = new DecisionDiagramManager();
var a = dd.Mtbdd.GetOrAddVariable("A");
var b = dd.Mtbdd.GetOrAddVariable("B");

// Row index uses variable IDs as bits: A is bit 0, B is bit 1.
var scores = dd.Mtbdd.Create(new[] { 10, 20, 30, 40 });

Console.WriteLine(dd.Mtbdd.Evaluate(
    scores,
    new Dictionary<VariableId, bool>
    {
        { a, true },
        { b, false }
    }));
```

## ZMTBDD Quick Start

```csharp
using DecisionDiagramSharp;
using System.Collections.Generic;

var dd = new DecisionDiagramManager();
var a = dd.Zmtbdd.GetOrAddVariable("A");

// High zero branches are suppressed; true A evaluates to the numeric zero.
var sparseScores = dd.Zmtbdd.Create(new[] { 7, 0 });

Console.WriteLine(dd.Zmtbdd.Evaluate(
    sparseScores,
    new Dictionary<VariableId, bool> { { a, true } }));
```

## DOT Example

```csharp
using DecisionDiagramSharp.Diagnostics;

var dot = family.ToDot();
Console.WriteLine(dot);
```

## CSV / Markdown / AsciiDoc Example

```csharp
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

var nodeTable = family.ToNodeTable();
var csv = CsvTableExporter.Export(nodeTable);
var markdown = family.ToMarkdownSetFamily();
var asciidoc = AsciiDocTableExporter.Export(nodeTable);
```

## Samples

- `samples/Bdd.FeatureFlags`
- `samples/Zdd.SetFamilies`
- `samples/Zdd.FukashigiCounting`
- `samples/Export.AllFormats`

## Docs

- `docs/architecture.md`
- `docs/backlog.md`
- `docs/done-policy.md`
- `docs/getting-started.md`
- `docs/api-guides/high-level-api.md`
- `docs/design/mtbdd-baseline.md`
- `docs/design/zmtbdd-baseline.md`
- `docs/v0.1-execution-plan.md`
- `docs/v0.2-execution-plan.md`
- `docs/v0.3-execution-plan.md`
- `docs/v0.4-execution-plan.md`
- `docs/v0.5-execution-plan.md`
- `docs/v0.6-execution-plan.md`
- `docs/v0.7-execution-plan.md`
- `docs/concepts/bdd.md`
- `docs/concepts/zdd.md`
