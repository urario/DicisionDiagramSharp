# Getting Started

This guide shows the v0.3 developer experience for the verified BDD and ZDD foundations.

## Create a Manager

Use `DecisionDiagramManager` when you want one discoverable entry point:

```csharp
using DecisionDiagramSharp;
using System.Collections.Generic;

var dd = new DecisionDiagramManager();
```

The facade owns separate BDD and ZDD managers:

```csharp
var bdd = dd.Bdd;
var zdd = dd.Zdd;
```

BDD and ZDD values remain separate typed handles. They cannot be mixed.

## BDD Example

```csharp
using DecisionDiagramSharp;

var dd = new DecisionDiagramManager();

var a = dd.Bdd.Var("A");
var b = dd.Bdd.Var("B");

var expression = a & !b;
```

To evaluate the expression:

```csharp
var value = dd.Bdd.Evaluate(
    expression,
    new Dictionary<VariableId, bool>
    {
        { dd.Bdd.GetOrAddVariable("A"), true },
        { dd.Bdd.GetOrAddVariable("B"), false }
    });
```

## ZDD Example

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
```

## Diagnostics and Export

DOT helpers live in `DecisionDiagramSharp.Diagnostics`:

```csharp
using DecisionDiagramSharp.Diagnostics;

var dot = expression.ToDot();
```

Markdown export helpers live in `DecisionDiagramSharp.Export`:

```csharp
using DecisionDiagramSharp;

var markdown = expression.ToMarkdownTruthTable();
var sets = family.ToMarkdownSetFamily();
```

Core remains pure: it does not depend on diagnostics, export, file I/O, or Graphviz execution.
