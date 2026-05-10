# Getting Started

This guide shows the current developer experience for BDD, ZDD, MTBDD, and ZMTBDD.

## Create a Manager

Use `DecisionDiagramManager` as a single discoverable entry point:

```csharp
using DecisionDiagramSharp;

var dd = new DecisionDiagramManager();
```

The facade owns one manager per family:

```csharp
var bdd    = dd.Bdd;
var zdd    = dd.Zdd;
var mtbdd  = dd.Mtbdd;
var zmtbdd = dd.Zmtbdd;
```

Values from different families are separate typed handles and cannot be mixed.

---

## BDD Example

BDD values support Boolean operators and string-name variable creation:

```csharp
using DecisionDiagramSharp;
using DecisionDiagramSharp.Export;

var dd = new DecisionDiagramManager();

var a = dd.Bdd.Var("A");
var b = dd.Bdd.Var("B");

var expression = a & !b;

Console.WriteLine(dd.Bdd.CountModels(expression));
Console.WriteLine(expression.ToMarkdownTruthTable());
```

You can also evaluate with a string-keyed dictionary:

```csharp
bool result = dd.Bdd.Evaluate(
    expression,
    new Dictionary<string, bool> { { "A", true }, { "B", false } });
```

---

## ZDD Example

ZDD values support string-name set construction:

```csharp
using DecisionDiagramSharp;
using DecisionDiagramSharp.Export;

var dd = new DecisionDiagramManager();

var family = dd.Zdd.MakeFamily(new[]
{
    new[] { "A" },
    new[] { "A", "B" }
});

Console.WriteLine(dd.Zdd.CountSets(family));
Console.WriteLine(family.ToMarkdownSetFamily());
```

---

## MTBDD Example

An MTBDD encodes an integer-valued Boolean-domain function.
`Create` accepts a complete truth table in LSB-first variable ordering:

```csharp
using DecisionDiagramSharp;
using DecisionDiagramSharp.Export;
using System.Collections.Generic;

var dd = new DecisionDiagramManager();
var a = dd.Mtbdd.GetOrAddVariable("A");
var b = dd.Mtbdd.GetOrAddVariable("B");

// Indices: A=bit0, B=bit1: [0]=A=F,B=F  [1]=A=T,B=F  [2]=A=F,B=T  [3]=A=T,B=T
var scores = dd.Mtbdd.Create(new[] { 0, 10, 5, 25 });

int result = dd.Mtbdd.Evaluate(
    scores,
    new Dictionary<VariableId, bool> { { a, true }, { b, true } });

Console.WriteLine(result);
Console.WriteLine(scores.ToMarkdownValueTable());
```

---

## ZMTBDD Example

A ZMTBDD is zero-suppressed: branches leading only to zero are removed.
Useful for sparse numeric functions where most inputs map to zero:

```csharp
using DecisionDiagramSharp;
using DecisionDiagramSharp.Export;
using System.Collections.Generic;

var dd = new DecisionDiagramManager();
var a = dd.Zmtbdd.GetOrAddVariable("A");

// Only A=false maps to 7; A=true is suppressed (evaluates to zero).
var sparse = dd.Zmtbdd.Create(new[] { 7, 0 });

int result = dd.Zmtbdd.Evaluate(
    sparse,
    new Dictionary<VariableId, bool> { { a, true } });

Console.WriteLine(result);
Console.WriteLine(sparse.ToMarkdownValueTable());
```

---

## Diagnostics and Export

DOT helpers require `using DecisionDiagramSharp.Diagnostics;`:

```csharp
using DecisionDiagramSharp.Diagnostics;

var dot = expression.ToDot();       // BDD
var dot = family.ToDot();           // ZDD
var dot = scores.ToDot();           // MTBDD
var dot = sparse.ToDot();           // ZMTBDD
```

Markdown/CSV/AsciiDoc export helpers require `using DecisionDiagramSharp.Export;`:

```csharp
// BDD
string md   = expression.ToMarkdownTruthTable();
string csv  = expression.ToCsvTruthTable();
string ad   = expression.ToAsciiDocTruthTable();
string mods = expression.ToMarkdownModels();

// ZDD
string md   = family.ToMarkdownSetFamily();
string csv  = family.ToCsvSetFamily();

// MTBDD
string md   = scores.ToMarkdownValueTable();
string csv  = scores.ToCsvValueTable();

// ZMTBDD
string md   = sparse.ToMarkdownValueTable();
string csv  = sparse.ToCsvValueTable();
```

Core remains pure: it does not depend on `Diagnostics`, `Export`, file I/O, or Graphviz.
