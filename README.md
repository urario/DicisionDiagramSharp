# DecisionDiagramSharp

Modern C#/.NET library for decision diagrams, including BDD (Binary Decision Diagram) and ZDD (Zero-suppressed Decision Diagram) foundations and extensible toward MDD.

## Install

NuGet packaging is planned before the first packaged preview release.

## BDD Quick Start

```csharp
using DecisionDiagramSharp;
using System.Collections.Generic;

var manager = new BddManager();
var a = manager.GetOrAddVariable("A");
var b = manager.GetOrAddVariable("B");

var f = manager.And(manager.Var(a), manager.Not(manager.Var(b)));

Console.WriteLine(manager.CountModels(f));
Console.WriteLine(manager.Evaluate(
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

var manager = new ZddManager();
var a = manager.GetOrAddVariable("A");
var b = manager.GetOrAddVariable("B");

var family = manager.MakeFamily(
    new[]
    {
        new[] { a },
        new[] { a, b }
    });

var containingA = manager.Containing(family, a);
Console.WriteLine(manager.CountSets(containingA));
```

## DOT Example

```csharp
using DecisionDiagramSharp.Diagnostics;

var dot = ZddDiagnostics.ToDot(manager, family);
Console.WriteLine(dot);
```

## CSV / Markdown / AsciiDoc Example

```csharp
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

var nodeTable = ZddDiagnostics.BuildNodeTable(manager, family);
var csv = CsvTableExporter.Export(nodeTable);
var markdown = MarkdownTableExporter.Export(nodeTable);
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
- `docs/v0.1-execution-plan.md`
- `docs/v0.2-execution-plan.md`
- `docs/concepts/bdd.md`
- `docs/concepts/zdd.md`
