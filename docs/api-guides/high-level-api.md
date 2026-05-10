# High-Level API Guide

This guide describes the friendly surface over the manager-level APIs.
Low-level managers remain available for algorithmic work.

---

## Unified Manager

`DecisionDiagramManager` is a facade that owns one manager per diagram family:

```csharp
var dd = new DecisionDiagramManager();

var bddManager   = dd.Bdd;
var zddManager   = dd.Zdd;
var mtbddManager = dd.Mtbdd;
var zmtbddManager = dd.Zmtbdd;
```

The facade does not merge semantics across families. It only makes the entry point easier to discover.

---

## BDD — String-Name Helpers

### `BddManager.Var(string name)`

Resolves or creates a variable by name and returns its canonical BDD node.
Equivalent to `manager.Var(manager.GetOrAddVariable(name))`.

```csharp
var dd = new DecisionDiagramManager();
var a = dd.Bdd.Var("A");
var b = dd.Bdd.Var("B");

var expression = (a & !b) | (a ^ b);
```

### `BddManager.Evaluate(Bdd value, IReadOnlyDictionary<string, bool> assignment)`

Evaluates a BDD with a string-keyed assignment dictionary.
Variable names are resolved to `VariableId` internally.
Throws `ArgumentException` for unknown variable names.

```csharp
bool result = dd.Bdd.Evaluate(
    expression,
    new Dictionary<string, bool>
    {
        { "A", true },
        { "B", false }
    });
```

### BDD Operators

`Bdd` values support Boolean-style operators. They preserve manager ownership checks:

```csharp
var f = (a & !b) | (a ^ b);
```

Combining values from different `BddManager` instances throws `DiagramManagerMismatchException`.

---

## ZDD — String-Name Helpers

### `ZddManager.MakeSet(IEnumerable<string> names)`

Constructs a ZDD for a single set whose members are identified by variable name.
Variable names are resolved via `GetOrAddVariable` internally.

```csharp
var family = dd.Zdd.MakeSet(new[] { "A", "B" });
```

### `ZddManager.MakeFamily(IEnumerable<IEnumerable<string>> sets)`

Constructs a ZDD for a family of sets, each member identified by variable name.

```csharp
var family = dd.Zdd.MakeFamily(new[]
{
    new[] { "A" },
    new[] { "A", "B" }
});
```

---

## BDD Diagnostics Extensions

Add `using DecisionDiagramSharp.Diagnostics;` then call:

```csharp
string dot             = expression.ToDot();
TableModel nodeTable   = expression.ToNodeTable();
TableModel truthTable  = expression.ToTruthTable();
TableModel modelTable  = expression.ToModelTable();
TableModel statistics  = expression.ToStatisticsTable();
```

---

## ZDD Diagnostics Extensions

```csharp
string dot             = family.ToDot();
TableModel nodeTable   = family.ToNodeTable();
TableModel setFamily   = family.ToSetFamilyTable();
TableModel statistics  = family.ToStatisticsTable();
```

---

## MTBDD Diagnostics Extensions

```csharp
string dot             = mtbdd.ToDot();
TableModel nodeTable   = mtbdd.ToNodeTable();
TableModel valueTable  = mtbdd.ToValueTable();
TableModel statistics  = mtbdd.ToStatisticsTable();
```

---

## ZMTBDD Diagnostics Extensions

```csharp
string dot             = zmtbdd.ToDot();
TableModel nodeTable   = zmtbdd.ToNodeTable();
TableModel valueTable  = zmtbdd.ToValueTable();
TableModel statistics  = zmtbdd.ToStatisticsTable();
```

---

## Export Extensions

Add references to both `DecisionDiagramSharp.Diagnostics` and `DecisionDiagramSharp.Export`, then call:

**BDD:**

```csharp
string markdownTruth  = expression.ToMarkdownTruthTable();
string csvTruth       = expression.ToCsvTruthTable();
string asciidocTruth  = expression.ToAsciiDocTruthTable();
string markdownModels = expression.ToMarkdownModels();
```

**ZDD:**

```csharp
string markdownSets   = family.ToMarkdownSetFamily();
string csvSets        = family.ToCsvSetFamily();
string asciidocSets   = family.ToAsciiDocSetFamily();
```

**MTBDD:**

```csharp
string markdownValues = mtbdd.ToMarkdownValueTable();
string csvValues      = mtbdd.ToCsvValueTable();
string asciidocValues = mtbdd.ToAsciiDocValueTable();
```

**ZMTBDD:**

```csharp
string markdownValues = zmtbdd.ToMarkdownValueTable();
string csvValues      = zmtbdd.ToCsvValueTable();
string asciidocValues = zmtbdd.ToAsciiDocValueTable();
```

Export helpers are intentionally outside Core so Core can stay compatible with `netstandard2.0`
and independent from formatting concerns.

---

## Dependency Direction

```
Core <- Diagnostics <- Export
```

`Core` never depends on `Diagnostics` or `Export`.
All helpers shown above live in `Diagnostics` or `Export`.
