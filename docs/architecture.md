# DecisionDiagramSharp Architecture Design Document

## 0. Document Information

| Item | Description |
|---|---|
| Document name | DecisionDiagramSharp Architecture Design Document |
| Target | DecisionDiagramSharp |
| Purpose | Define the architectural direction for a C#/.NET decision diagram library supporting BDD, ZDD, MTBDD, and ZMTBDD |
| Intended path | `docs/architecture.md` |
| Initial target versions | v0.1 to v0.8 |
| Recommended target for library projects | C# / `netstandard2.0` |
| Recommended platform for development, tests, CLI, samples, and benchmarks | .NET 8 LTS or later |
| Recommended license | Apache-2.0 |
| Distribution | GitHub / NuGet |

Notes:

- `DecisionDiagramSharp.Core`, `DecisionDiagramSharp.Diagnostics`, and `DecisionDiagramSharp.Export` should initially target `netstandard2.0`.
- Executable applications such as the CLI, samples, benchmarks, and a future WPF viewer should target `.NET 8` or later.
- If performance optimization or newer BCL APIs become necessary later, multi-targeting with `netstandard2.0;net8.0` may be introduced.

---

## 1. Introduction

### 1.1 Background

`DecisionDiagramSharp` is a modern C#/.NET library for working with decision diagrams such as BDD, ZDD, MTBDD, and ZMTBDD.

The initial primary targets are:

- **BDD: Binary Decision Diagram**
  - Represents Boolean functions, logical conditions, feature flags, build conditions, and conditional compilation expressions.

- **ZDD: Zero-suppressed Decision Diagram**
  - Represents sparse set families, include path sets, modification candidate sets, impact sets, and graph path families.

Numeric decision diagram targets include:

- **MTBDD: Multi-Terminal Binary Decision Diagram**
  - Represents integer-valued Boolean-domain functions and provides the direct-function baseline for numeric experiments.

- **ZMTBDD: Zero-suppressed MTBDD**
  - Represents sparse numeric functions with many zero-valued regions and provides the zero-suppressed numeric representation.

MDD and ADD / weighted DD are out of the current roadmap. They should not drive abstractions unless the project owner explicitly reopens that direction.

The project may begin as a personal or experimental project, but the design quality should not be compromised. It should grow into an OSS candidate that is easy to learn, easy to experiment with, and suitable for practical applications.

---

## 2. Architectural Principles

### 2.1 Core Principles

The design principles of `DecisionDiagramSharp` are as follows.

1. **Keep Core pure**
   - Core must not contain file I/O, CLI parsing, Graphviz process execution, CSV/Markdown/AsciiDoc formatting, or code-analysis domain logic.
   - Core focuses only on decision diagram construction, operations, traversal, validation, and statistics.

2. **Do not over-unify BDD, ZDD, MTBDD, and ZMTBDD diagrams**
   - Each decision diagram type has different terminal semantics, reduction rules, and operation semantics.
   - Commonization should be limited to variable identifiers, variable tables, ordering, statistics, validation conventions, and diagnostic concepts.

3. **Make public APIs user-friendly**
   - Provide not only low-level manager APIs but also fluent APIs, operator overloads, and extension methods.
   - A beginner should be able to construct, visualize, and export a BDD/ZDD within a few minutes.

4. **Treat diagnostics, visualization, and export as first-class features**
   - DOT, CSV, Markdown, AsciiDoc, truth tables, and set-family output should be available from early versions.
   - These features are essential for debugging, learning, documentation, review, and OSS adoption.

5. **Treat documentation and samples as part of the product**
   - README, tutorials, design notes, samples, and developer guides should be maintained from the beginning.
   - Samples should build and run in CI.

6. **Do not block future extension**
   - Complement edges, dynamic variable reordering, numeric diagram refinements, and optional native backends should remain possible.
   - Initial implementation should prioritize simplicity and correctness over advanced optimization.

7. **Prioritize consumer compatibility for library projects**
   - Core, Diagnostics, and Export should initially target `netstandard2.0`.
   - This allows use from .NET 8+ applications and from .NET Framework 4.7.2 / 4.8 WPF, WinForms, and enterprise applications.
   - CLI, samples, benchmarks, and development tools may target `.NET 8` or later.

8. **Do not introduce new .NET-only APIs into Core too early**
   - Core hot paths should use BCL APIs available on `netstandard2.0`.
   - Initial Core implementation should not depend on APIs such as `FrozenDictionary`, `CollectionsMarshal`, or `PriorityQueue`.
   - If necessary later, such APIs may be introduced behind a `net8.0` target with conditional compilation.

---

## 3. Context Viewpoint

### 3.1 System Boundary

`DecisionDiagramSharp` is a library consumed by C#/.NET applications.

```text
+-----------------------------------------------------------+
|                    User Application                       |
|                                                           |
|  - Console App                                            |
|  - GUI Tool                                               |
|  - Code Analysis Tool                                     |
|  - Test Utility                                           |
|  - Documentation Generator                                |
+----------------------------+------------------------------+
                             |
                             v
+-----------------------------------------------------------+
|                  DecisionDiagramSharp                     |
|                                                           |
|  Core / Diagnostics / Export / CodeAnalysis / CLI         |
+----------------------------+------------------------------+
                             |
       +---------------------+---------------------+
       |                                           |
       v                                           v
+--------------+                         +-------------------+
| File Outputs |                         | External Tools    |
| CSV / MD     |                         | Graphviz dot      |
| AsciiDoc     |                         | NuGet / GitHub    |
| DOT          |                         | CI / Benchmark    |
+--------------+                         +-------------------+
```

### 3.2 Users

| User type | Purpose |
|---|---|
| Learners | Try basic BDD/ZDD concepts |
| OSS developers | Use decision diagram operations in C# |
| Researchers and experimenters | Experiment with BDD/ZDD/MTBDD/ZMTBDD algorithms |
| Legacy-code maintainers | Analyze include dependencies, conditional compilation, and impact sets |
| Tool developers | Integrate the library into static analysis or visualization tools |
| AI-assisted developers | Extend features, tests, and documentation using AI support |

### 3.3 External Systems

| External system | Relationship |
|---|---|
| GitHub | Source control, Issues, Pull Requests, Actions, Pages |
| NuGet | Package distribution |
| Graphviz | Rendering DOT files to images |
| MSBuild / sourceDependencies | Future input source for include dependency information |
| clang / IWYU / clangd | Future input source for C/C++ analysis information |
| Markdown / AsciiDoc viewers | Viewing documentation and reports |
| CI services | Build, test, formatting validation |
| BenchmarkDotNet | Performance measurement |

### 3.4 Out of Scope for Initial Versions

The following are out of scope for the initial versions:

- Full CUDD compatibility
- Commercial EDA-grade formal verification engine
- Custom C/C++ parser
- Graphviz image generation inside Core
- Advanced optimization for very large BDDs
- Dynamic variable reordering
- Complement edges
- Parallel BDD/ZDD operations
- MDD / ADD implementation
- .NET 8-specific Core optimization in the initial stage
- `net8.0-windows` or WPF dependency in Core
- Direct use of Windows-specific APIs from Core

MDD and ADD are not planned roadmap extensions at this time. The other items may be considered as future extensions.

---

## 4. Functional Viewpoint

### 4.1 Functional Decomposition

```text
DecisionDiagramSharp
|
+-- Core
|   +-- Common
|   +-- BDD
|   +-- ZDD
|   +-- MTBDD
|   +-- ZMTBDD
|   +-- Manager
|
+-- Diagnostics
|   +-- DOT Export
|   +-- Truth Table
|   +-- Node Dump
|   +-- Set Family Dump
|   +-- Statistics
|
+-- Export
|   +-- CSV
|   +-- Markdown
|   +-- AsciiDoc
|
+-- CodeAnalysis
|   +-- Include Graph
|   +-- Include Path Family
|   +-- Header Contamination
|   +-- Conditional Impact future
|
+-- CLI
    +-- Diagram command
    +-- Export command
    +-- CodeAnalysis command future
```

### 4.2 Core Features

#### 4.2.1 Common Features

- `VariableId`
- `VariableTable`
- `VariableOrdering`
- `DiagramStatistics`
- `DecisionDiagramOptions`
- Manager ownership validation
- Node count limits
- Operation cache statistics
- Validation APIs

#### 4.2.2 BDD Features

BDD represents Boolean functions.

Initial features:

- `True` / `False`
- `Var`
- `Not`
- `And`
- `Or`
- `Xor`
- `Ite`
- `Evaluate`
- `IsSatisfiable`
- `Implies`
- `Equivalent`
- `Restrict`
- `Exists`
- `ForAll`
- `CountModels`
- `EnumerateModels`

User-friendly API target:

```csharp
var dd = new DecisionDiagramManager();

var A = dd.Bdd.Var("A");
var B = dd.Bdd.Var("B");
var C = dd.Bdd.Var("C");

var f = (A & !B) | C;

Console.WriteLine(f.ToMarkdownTruthTable());
File.WriteAllText("f.dot", f.ToDot());
```

#### 4.2.3 ZDD Features

ZDD represents sparse set families.

Initial features:

- `Empty`
- `Base`
- `Var`
- `MakeSet`
- `MakeFamily`
- `Union`
- `Intersect`
- `Difference`
- `Subset0`
- `Subset1`
- `Change`
- `Containing`
- `NotContaining`
- `CountSets`
- `ContainsSet`
- `EnumerateSets`

User-friendly API target:

```csharp
var dd = new DecisionDiagramManager();

var paths = dd.Zdd.SetFamily<string>()
    .AddSet(["A.cpp -> A.h", "A.h -> Common.h", "Common.h -> Windows.h"])
    .AddSet(["B.cpp -> B.h", "B.h -> LegacyBase.h", "LegacyBase.h -> Windows.h"]);

var viaWindows = paths.Containing("Common.h -> Windows.h");

Console.WriteLine(viaWindows.ToMarkdown());
File.WriteAllText("paths.dot", viaWindows.ToDot());
```

#### 4.2.4 MTBDD and ZMTBDD Features

MTBDD and ZMTBDD extend the library from Boolean and set-family diagrams into integer-valued Boolean-domain functions.

The numeric diagram work is intentionally staged:

| Diagram | Role | Priority |
|---|---|---:|
| MTBDD | Direct integer-valued function representation | High |
| ZMTBDD | Zero-suppressed sparse numeric representation | High |

MTBDD represents:

```text
f : {0,1}^n -> Z
```

Initial numeric diagram features:

- typed handles and manager-owned values
- integer terminal interning
- reduced ordered construction
- bounded construction from explicit truth-table rows
- `Evaluate`
- diagram statistics
- DOT and node-table diagnostics
- construction and evaluation benchmarks for MTBDD and ZMTBDD
- API polish, documentation, and tests that make numeric diagrams as approachable as BDD and ZDD

### 4.3 Diagnostics Features

| Feature | Target | Purpose |
|---|---|---|
| DOT output | BDD/ZDD/MTBDD/ZMTBDD | Structure visualization |
| Truth table | BDD | Learning, validation, explanation |
| Model enumeration | BDD | Show satisfying examples |
| Set-family preview | ZDD | Show part of a large set family |
| Integer function table | MTBDD/ZMTBDD | Validate numeric semantics |
| Node table dump | BDD/ZDD/MTBDD/ZMTBDD | Implementation debugging |
| Statistics | BDD/ZDD/MTBDD/ZMTBDD | Performance and scale inspection |

### 4.4 Export Features

Supported formats:

- CSV
- Markdown
- AsciiDoc
- DOT

DOT is a graph format and should be treated separately from CSV/Markdown/AsciiDoc table output.

| Target | CSV | Markdown | AsciiDoc | DOT |
|---|---:|---:|---:|---:|
| BDD truth table | Yes | Yes | Yes | No |
| BDD model list | Yes | Yes | Yes | No |
| BDD node list | Yes | Yes | Yes | Yes |
| ZDD set-family list | Yes | Yes | Yes | No |
| ZDD node list | Yes | Yes | Yes | Yes |
| MTBDD value table | Yes | Yes | Yes | No |
| MTBDD/ZMTBDD node list | Yes | Yes | Yes | Yes |
| Statistics | Yes | Yes | Yes | No |
| Include analysis report | Yes | Yes | Yes | No |

### 4.5 CodeAnalysis Features

The initial CodeAnalysis layer should start small as a ZDD application sample.

Main features:

- Include edge definition
- Include path definition
- Conversion of include path families to ZDDs
- Path families reaching a target header
- Contamination cause ranking
- Cut candidate ranking

Future features:

- Conditional include edges using BDDs
- Build configuration constraints
- Conditional impact analysis
- Minimal CI configuration candidates
- Header contamination reports

---

## 5. Information Viewpoint

### 5.1 Main Information Model

```text
VariableTable
  +-- VariableId
  +-- VariableName

BDD
  +-- Bdd handle
  +-- BddNode table
  +-- Bdd unique table
  +-- Bdd operation cache

ZDD
  +-- Zdd handle
  +-- ZddNode table
  +-- Zdd unique table
  +-- Zdd operation cache

MTBDD
  +-- Mtbdd handle
  +-- terminal value table
  +-- MtbddNode table
  +-- Mtbdd unique table

ZMTBDD
  +-- Zmtbdd handle
  +-- zero terminal
  +-- terminal value table
  +-- ZmtbddNode table
  +-- Zmtbdd unique table

Diagnostics
  +-- TruthTable
  +-- SetFamilyTable
  +-- NodeTable
  +-- DiagramStatistics

Export
  +-- TableModel

CodeAnalysis
  +-- IncludeEdge
  +-- IncludePath
  +-- IncludePathFamily
  +-- HeaderContaminationReport
```

### 5.2 VariableId

```csharp
public readonly record struct VariableId(int Value);
```

Responsibilities:

- Identify BDD variables
- Identify ZDD set elements
- Identify MTBDD/ZMTBDD Boolean-domain variables
- Variable name mapping is owned by `VariableTable`

### 5.3 VariableTable

```csharp
public sealed class VariableTable
{
    public VariableId GetOrAdd(string name);
    public string GetName(VariableId id);
    public int Count { get; }
}
```

Responsibilities:

- Generate IDs from variable names
- Resolve display names from IDs
- Provide a shared variable namespace for BDD/ZDD/MTBDD/ZMTBDD diagrams where appropriate

Notes:

- Leave room for a future `VariableKind` if cross-family naming becomes ambiguous.
- Be careful about accidental mixing of BDD variables, ZDD elements, and MTBDD/ZMTBDD variables.

### 5.4 BDD Information Model

#### Terminals

```text
0 = False
1 = True
```

#### Node

```csharp
internal readonly record struct BddNode(
    int Variable,
    int Low,
    int High
);
```

#### Reduction Rule

```text
If Low == High, remove the node and return Low.
```

#### Unique Table

Key:

```csharp
internal readonly record struct BddKey(
    int Variable,
    int Low,
    int High
);
```

Meaning:

- Nodes with identical `(Variable, Low, High)` are shared.
- This supports canonicity.

### 5.5 ZDD Information Model

#### Terminals

```text
0 = Empty = {}
1 = Base  = {{}}
```

#### Node

```csharp
internal readonly record struct ZddNode(
    int Variable,
    int Low,
    int High
);
```

#### Reduction Rule

```text
If High == Empty, remove the node and return Low.
```

#### Unique Table

Key:

```csharp
internal readonly record struct ZddKey(
    int Variable,
    int Low,
    int High
);
```

### 5.6 Difference Between BDD and ZDD Terminals

| Structure | Terminal 0 | Terminal 1 |
|---|---|---|
| BDD | False | True |
| ZDD | Empty `{}` | Base `{{}}` |

This difference must be reflected in API names.

BDD:

```csharp
BddManager.False
BddManager.True
```

ZDD:

```csharp
ZddManager.Empty
ZddManager.Base
```

### 5.7 MTBDD/ZMTBDD Information Model

MTBDD and ZMTBDD diagrams operate over Boolean-domain variables but use integer terminals.

#### MTBDD Terminal

```text
terminal = integer value
```

MTBDD terminal values are part of the represented function. The zero terminal means the numeric value `0`.

#### ZMTBDD Terminal

```text
zero terminal = numeric zero
```

ZMTBDD uses zero suppression for sparse numeric functions. The zero terminal remains the ordinary numeric value `0`.

### 5.8 Intermediate Table Model

The Export layer should not directly format BDD/ZDD internals. Diagnostics should convert diagrams into intermediate models, and Export should format those models.

```csharp
public sealed class TableModel
{
    public string? Title { get; }
    public IReadOnlyList<string> Columns { get; }
    public IReadOnlyList<TableRow> Rows { get; }
}

public sealed class TableRow
{
    public IReadOnlyList<string> Cells { get; }
}
```

Specialized models:

- `TruthTable`
- `SetFamilyTable`
- `NodeTable`
- `StatisticsTable`

### 5.9 Include Analysis Information Model

```csharp
public readonly record struct IncludeEdge(string From, string To);

public sealed class IncludePath
{
    public IReadOnlyList<IncludeEdge> Edges { get; }
}

public sealed class HeaderContaminationReport
{
    public string TargetHeader { get; init; }
    public long TotalPaths { get; init; }
    public int AffectedTranslationUnits { get; init; }
    public IReadOnlyList<HeaderCauseRow> Causes { get; init; }
}
```

ZDD mapping:

```text
ZDD variable = IncludeEdge
ZDD set      = IncludePath
ZDD family   = All IncludePath values reaching the target header
```

---

## 6. Concurrency Viewpoint

### 6.1 Basic Policy

In the initial implementation, `BddManager` and `ZddManager` are not thread-safe.

Reasons:

- Node tables
- Unique tables
- Operation caches
- Statistics
- Validation state

These are mutable internal states.

### 6.2 Thread-Safety Policy

| Target | Thread-safety |
|---|---|
| `DecisionDiagramManager` | Not thread-safe |
| `BddManager` | Not thread-safe |
| `ZddManager` | Not thread-safe |
| `Bdd` handle | Immutable value; safe to read, but manager-dependent |
| `Zdd` handle | Immutable value; safe to read, but manager-dependent |
| Exporter | Thread-safe if stateless |
| Diagnostics builder | Stateless implementation recommended |
| TableModel | Thread-safe if immutable |

### 6.3 Recommended Usage

Use one manager per thread.

```csharp
Parallel.ForEach(inputs, input =>
{
    var dd = new DecisionDiagramManager();
    // thread-local operations
});
```

Concurrent operations on the same manager are prohibited in initial versions.

### 6.4 Future Concurrency Options

Future work may include:

- Parallel traversal of read-only diagrams
- Parallel export processing
- Parallel CodeAnalysis over multiple targets
- Parallel BDD/ZDD backend
- Immutable snapshots
- Optional locked manager mode

### 6.5 Operation Cache and Concurrency

Operation caches are stored inside managers. The initial implementation should not use lock-free or lock-based shared caches.

Future extension may include:

```csharp
public enum ThreadSafetyMode
{
    None,
    CoarseLock,
    ReadOnlySnapshots
}
```

---

## 7. Development Viewpoint

### 7.1 Development Environment

Recommended:

- .NET 8 LTS SDK or later
- C# 12 or later
- Visual Studio / VS Code / Rider
- MSTest for unit tests
- BenchmarkDotNet
- GitHub Actions
- NuGet

Target framework policy:

| Target | TargetFramework | Reason |
|---|---|---|
| `DecisionDiagramSharp.Core` | `netstandard2.0` | Broad consumer compatibility |
| `DecisionDiagramSharp.Diagnostics` | `netstandard2.0` | Usable from WPF, WinForms, CLI, and tests |
| `DecisionDiagramSharp.Export` | `netstandard2.0` | CSV/Markdown/AsciiDoc export should remain broadly compatible |
| `DecisionDiagramSharp.CodeAnalysis` | Initially `netstandard2.0`, optionally `net8.0` | Depends on import sources and external tool integration |
| `DecisionDiagramSharp.Cli` | `net8.0` | Executable tool using modern .NET |
| `DecisionDiagramSharp.Benchmarks` | `net8.0` | Benchmark on modern runtime |
| `DecisionDiagramSharp.Sample.*` | `net8.0` | Simple modern examples |
| Future WPF viewer | `net8.0-windows` | WPF requires a Windows-specific TFM |

Basic policy:

- Library projects start as `netstandard2.0` only.
- Tests, samples, benchmarks, and CLI use `.NET 8` or later.
- If newer BCL APIs are required for optimization, Core may later become `netstandard2.0;net8.0`.

### 7.2 Solution Structure

Initial full target structure:

```text
DecisionDiagramSharp.slnx
|
+-- src/
|   +-- DecisionDiagramSharp.Core
|   +-- DecisionDiagramSharp.Diagnostics
|   +-- DecisionDiagramSharp.Export
|   +-- DecisionDiagramSharp.CodeAnalysis
|   +-- DecisionDiagramSharp.Cli
|
+-- tests/
|   +-- DecisionDiagramSharp.Core.Tests
|   +-- DecisionDiagramSharp.Diagnostics.Tests
|   +-- DecisionDiagramSharp.Export.Tests
|   +-- DecisionDiagramSharp.CodeAnalysis.Tests
|
+-- benchmarks/
|   +-- DecisionDiagramSharp.Benchmarks
|
+-- samples/
    +-- Bdd.*
    +-- Zdd.*
    +-- Export.*
    +-- CodeAnalysis.*
```

For v0.1, start with a minimal structure:

```text
DecisionDiagramSharp.slnx
|
+-- src/
|   +-- DecisionDiagramSharp.Core
|
+-- tests/
|   +-- DecisionDiagramSharp.Core.Tests
|
+-- benchmarks/
|   +-- DecisionDiagramSharp.Benchmarks
|
+-- samples/
    +-- DecisionDiagramSharp.Sample.ZddBasic
```

Add Diagnostics, Export, CodeAnalysis, and CLI from late v0.1 to v0.3.

### 7.3 Dependency Rules

```text
Core
  depends on: BCL only

Diagnostics
  depends on: Core

Export
  depends on: Diagnostics or shared table models

CodeAnalysis
  depends on: Core, Diagnostics, Export if needed

Cli
  depends on: Core, Diagnostics, Export, CodeAnalysis
```

Forbidden:

```text
Core -> Diagnostics
Core -> Export
Core -> CodeAnalysis
Core -> Cli
```

### 7.4 Public API Policy

Public:

- `DecisionDiagramManager`
- `BddManager`
- `ZddManager`
- `Bdd`
- `Zdd`
- `VariableId`
- `VariableTable`
- Options
- Diagnostics/Export extension methods

Not public:

- Node objects
- Unique tables
- Operation caches
- Internal keys
- Manager internal storage

### 7.5 Coding Rules

Avoid in Core hot paths:

- LINQ
- Unnecessary class allocation
- Tuple allocation
- String operations
- File I/O
- Reflection
- Global mutable state

Prefer:

- `readonly struct`
- Internal `int` node IDs
- Struct keys
- `Dictionary<TKey, TValue>` with struct keys
- `List<T>` or array-backed storage
- Explicit operation caches
- Clear exception types
- XML documentation comments

#### 7.5.1 `netstandard2.0` Compatibility Rules

Core, Diagnostics, and Export should initially use APIs available on `netstandard2.0`.

Avoid:

- `CollectionsMarshal`
- `FrozenDictionary`
- `FrozenSet`
- `PriorityQueue`
- `DateOnly`
- `TimeOnly`
- OS-specific APIs
- WPF / WinForms types
- `System.CommandLine` in Core
- Graphviz process execution
- `Process.Start` in Core

Prefer:

- `List<T>`
- `Dictionary<TKey, TValue>`
- `HashSet<T>`
- `StringBuilder`
- `readonly struct`
- `readonly record struct`
- Explicit `IEquatable<T>` where useful
- Allocation-conscious cache keys
- BCL APIs available on `netstandard2.0`

C# 12 syntax may be used, but BCL compatibility must be checked. Distinguish between language syntax and runtime/BCL availability.

### 7.6 Testing Policy

#### MSTest Unit Tests

Unit test projects use MSTest.

BDD:

- Terminals
- Variables
- `Not`
- `And`
- `Or`
- `Xor`
- `Ite`
- `Implies`
- `Equivalent`
- `Restrict`
- `Exists`
- `Evaluate`
- `CountModels`

ZDD:

- `Empty` / `Base`
- `MakeSet`
- `MakeFamily`
- `Union`
- `Intersect`
- `Difference`
- `Subset0`
- `Subset1`
- `Change`
- `Containing`
- `CountSets`
- `EnumerateSets`
- `ContainsSet`

#### Property-Based / Random Tests

ZDD:

- Compare with a naive `HashSet<SortedSet<int>>` implementation.

BDD:

- Compare with truth-table evaluation for small variable counts.

#### Golden Tests

- DOT
- CSV
- Markdown
- AsciiDoc

#### Benchmarks

- ZDD `MakeFamily`
- ZDD `Union`
- ZDD `Difference`
- BDD `Ite`
- BDD truth table
- Export formatting

### 7.7 Documentation Policy

Documentation is maintained in the repository.

```text
docs/
  getting-started.md
  architecture.md
  backlog.md
  done-policy.md
  concepts/
  tutorials/
  api-guides/
  design/
  contributing/
```

Samples are built and run in CI.

### 7.8 AI-Assisted Development

`AGENTS.md` defines rules for AI contributors and automation tools.

Examples:

```text
- Public APIs require XML documentation comments.
- Add tests for every new operation.
- Core hot paths must not use LINQ.
- Exporters must use intermediate table models.
- Core must not depend on Diagnostics, Export, CodeAnalysis, or CLI.
- Samples must build in CI.
- Non-trivial tasks require a task table, completion definition, verification method, and evidence.
```

---

## 8. Physical Viewpoint

### 8.1 Distribution

| Artifact | Content |
|---|---|
| GitHub repository | Source, Issues, PRs, Actions, docs |
| NuGet package | Library package |
| GitHub Releases | Versioned artifacts |
| GitHub Pages / DocFX future | API docs and tutorials |
| CLI binary future | Command-line tool |

### 8.2 NuGet Package

Initial recommendation: use a single package.

```text
DecisionDiagramSharp
```

The initial package target is `netstandard2.0`.

```text
DecisionDiagramSharp
  lib/netstandard2.0/DecisionDiagramSharp.dll
```

Reasons:

- Usable from .NET 8+ applications.
- Usable from .NET Framework 4.7.2 / 4.8 WPF, WinForms, and enterprise applications.
- Keeps Core OS-independent and UI-independent.
- Fits legacy code-analysis use cases.
- Simplifies adoption for NuGet consumers.

If newer BCL APIs or performance optimizations are needed later, multi-targeting may be introduced:

```text
DecisionDiagramSharp
  lib/netstandard2.0/DecisionDiagramSharp.dll
  lib/net8.0/DecisionDiagramSharp.dll
```

Even then, `netstandard2.0` remains the compatibility baseline.

Future package split may be considered:

```text
DecisionDiagramSharp.Core
DecisionDiagramSharp.Diagnostics
DecisionDiagramSharp.Export
DecisionDiagramSharp.CodeAnalysis
```

Do not split too early. Use a single package initially and separate responsibilities by namespaces and internal project boundaries.

### 8.3 csproj Metadata Example

Example for `DecisionDiagramSharp.Core` or a single package:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

    <AssemblyName>DecisionDiagramSharp.Core</AssemblyName>
    <RootNamespace>DecisionDiagramSharp</RootNamespace>

    <PackageId>DecisionDiagramSharp</PackageId>
    <Title>DecisionDiagramSharp</Title>
    <Description>
      A modern C#/.NET library for BDD, ZDD, MTBDD, ZMTBDD,
      and decision diagrams:
      symbolic Boolean logic, sparse set families, numeric correction functions, truth tables,
      DOT/CSV/Markdown/AsciiDoc export.
    </Description>
    <Authors>Yuta Urano</Authors>
    <RepositoryUrl>https://github.com/YOUR_ACCOUNT/DecisionDiagramSharp</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageProjectUrl>https://github.com/YOUR_ACCOUNT/DecisionDiagramSharp</PackageProjectUrl>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageTags>
      bdd;zdd;mtbdd;zmtbdd;
      decision-diagrams;binary-decision-diagram;
      zero-suppressed-decision-diagram;multi-terminal-bdd;csharp;dotnet;
      symbolic-computation;numeric-correction;calibration-table;
      approximate-computing;truth-table;graphviz;asciidoc;static-analysis
    </PackageTags>
  </PropertyGroup>

</Project>
```

Future `net8.0` optimization:

```xml
<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
```

Conditional implementation example:

```csharp
#if NET8_0_OR_GREATER
// net8.0 or later optimized implementation
#else
// netstandard2.0-compatible implementation
#endif
```

### 8.4 License

Recommended:

```text
Apache-2.0
```

Reasons:

- Permissive license
- Commercial-use friendly
- Explicit patent grant
- Comfortable for enterprise users of algorithmic OSS

Alternative:

```text
MIT
```

### 8.5 CI/CD

GitHub Actions should run:

```text
on push / pull_request:
  dotnet restore
  dotnet build
  dotnet test
  dotnet format --verify-no-changes
```

Optional:

```text
workflow_dispatch:
  benchmark
  package
  publish dry-run
```

### 8.6 External Tools

| Tool | Purpose |
|---|---|
| Graphviz | Render DOT to SVG/PNG |
| BenchmarkDotNet | Measure performance |
| DocFX future | Generate API documentation |
| NuGet CLI / dotnet pack | Create packages |
| GitHub Actions | CI/CD |

External tool policy:

- Core must not execute Graphviz.
- CLI may target `.NET 8` or later.
- A WPF viewer, if added, must live in a separate project such as `DecisionDiagramSharp.Wpf`.
- Core, Diagnostics, and Export should remain `netstandard2.0` and not depend on Windows-specific APIs.

---

## 9. Logical Viewpoint

### 9.1 Layer Structure

```text
+--------------------------------------------------+
|                    CLI / Apps                    |
+--------------------------------------------------+
|                 CodeAnalysis                     |
+--------------------------------------------------+
|        Diagnostics              Export           |
+--------------------------------------------------+
|                       Core                       |
|  Common / BDD / ZDD / MTBDD/ZMTBDD / Manager     |
+--------------------------------------------------+
|                    .NET BCL                      |
+--------------------------------------------------+
```

### 9.1.1 TargetFramework Boundary

Logical layers and target framework boundaries are separated.

```text
+--------------------------------------------------+
|              WPF Viewer future                   |
|              net8.0-windows                      |
+--------------------------------------------------+
|                    CLI / Apps                    |
|                    net8.0                        |
+--------------------------------------------------+
|                 CodeAnalysis                     |
|          netstandard2.0 or net8.0                |
+--------------------------------------------------+
|        Diagnostics              Export           |
|              netstandard2.0                      |
+--------------------------------------------------+
|                       Core                       |
|  Common / BDD / ZDD / MTBDD/ZMTBDD / Manager     |
|              netstandard2.0                      |
+--------------------------------------------------+
|                    .NET BCL                      |
+--------------------------------------------------+
```

Keeping Core on `netstandard2.0` allows broad usage from:

- .NET 8+ console, CLI, and GUI applications
- .NET Framework 4.7.2 / 4.8 WPF applications
- .NET Framework 4.7.2 / 4.8 WinForms applications
- Test utilities
- Documentation generators
- Legacy code-analysis support tools

WPF and CLI-specific functionality belongs in upper layers, not Core.

### 9.2 Core Internal Structure

```text
DecisionDiagramManager
  +-- VariableTable
  +-- BddManager
  |   +-- BDD node table
  |   +-- BDD unique table
  |   +-- BDD operation cache
  |
  +-- ZddManager
      +-- ZDD node table
      +-- ZDD unique table
      +-- ZDD operation cache
```

### 9.3 Dependency Direction

```text
Core <- Diagnostics <- Export
Core <- CodeAnalysis
Core <- CLI
Diagnostics <- CLI
Export <- CLI
CodeAnalysis <- CLI
```

Strict rule:

```text
Core must not depend on Diagnostics, Export, CodeAnalysis, or CLI.
```

---

## 10. Operational Viewpoint

### 10.1 Versioning

Use Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

During `0.x`, API changes are allowed. Public API changes must be recorded in CHANGELOG.

### 10.2 Release Units

Recommended roadmap:

| Version | Content |
|---|---|
| v0.1 | ZDD Foundation |
| v0.2 | BDD Foundation |
| v0.3 | Unified Developer Experience |
| v0.4 | MTBDD Baseline |
| v0.5 | ZMTBDD Baseline |
| v0.6 | Existing Diagram Refinement and Test Coverage |
| v0.7 | Usability, Samples, and CodeAnalysis Preparation |
| v0.8+ | CodeAnalysis / Conditional Analysis |

MDD and ADD / weighted DD are out of the current roadmap.

### 10.3 Compatibility Policy

- `0.x` may include API changes.
- `1.0` and later should avoid breaking public API changes.
- Internal implementation may change freely.
- Node tables and unique tables must not be public.
- `netstandard2.0` is the baseline for library consumer compatibility.
- For .NET Framework consumers, .NET Framework 4.7.2 or later is expected; .NET Framework 4.8 / 4.8.1 is recommended.
- New applications should use .NET 8 or later.
- If a `net8.0` target is added later, the `netstandard2.0` target should normally remain supported.

### 10.4 Monitoring Targets

This is a library, so runtime operation monitoring is not required. Track:

- CI success rate
- Test count
- Benchmark trends
- NuGet downloads
- Issue/PR backlog
- README sample health
- Documentation freshness

---

## 11. Security and Safety Viewpoint

### 11.1 Combinatorial Explosion Controls

Decision diagrams can represent huge combinatorial spaces, and enumeration can explode.

Safety controls:

- Truth table variable limit
- Model enumeration limit
- Set-family enumeration limit
- DOT output node limit
- `MaxNodeCount` option
- Explicit exceptions

### 11.2 Exception Design

```csharp
public class DiagramException : Exception { }

public sealed class DiagramManagerMismatchException : DiagramException { }
public sealed class DiagramSizeLimitExceededException : DiagramException { }
public sealed class DiagramEnumerationLimitExceededException : DiagramException { }
public sealed class InvalidVariableOrderingException : DiagramException { }
```

Error messages should be actionable.

### 11.3 File I/O

Core performs no file I/O.

File saving belongs in Export/CLI layers.

Core should remain a `netstandard2.0` library with broad consumer compatibility. It must not contain file I/O, external process execution, or OS-specific APIs.

File saving, Graphviz execution, WPF display, and CLI argument handling belong to upper layers.

### 11.4 External Processes

Graphviz execution is not part of Core.

If necessary, it should be implemented in CLI or a dedicated optional integration.

---

## 12. Quality Attribute Scenarios

### 12.1 Maintainability

**Scenario:** Add a new ZDD operation `RemoveSupersets`.

Expected:

- Implementation is localized to `ZddManager` or `ZddOperations`.
- BDD is unaffected.
- Naive set-family comparison tests can be added.
- Export layer does not need changes.

### 12.2 Extensibility

**Scenario:** Improve MTBDD and ZMTBDD construction APIs.

Expected:

- Numeric diagram changes stay localized to `MtbddManager`, `ZmtbddManager`, diagnostics, export, and tests.
- Existing BDD/ZDD public APIs remain compatible.
- Core remains free of file I/O and external process dependencies.
- Benchmarks can compare MTBDD and ZMTBDD construction and evaluation on the same generated functions.

### 12.3 Readability

**Scenario:** A new contributor wants to understand the ZDD reduction rule.

Expected:

- `docs/design/zdd-reduction-rules.md` exists.
- The responsibility of `ZddManager.MakeNode` is clear.
- Test names express specifications.

### 12.4 Performance

**Scenario:** Build a ZDD from 100,000 sets.

Expected:

- Operation caches are used.
- BenchmarkDotNet tracks performance.
- Core does not rely on LINQ or excessive class allocation.

### 12.5 Usability

**Scenario:** A beginner wants to print a truth table for `A && !B`.

Expected:

- README Quick Start runs successfully.
- `ToMarkdownTruthTable()` displays the table.
- `ToDot()` exports the structure.

---

## 13. Major Design Decisions

### 13.1 Use `netstandard2.0` for Library Projects

DecisionDiagramSharp library projects should initially target `netstandard2.0`.

Reasons:

- Usable from .NET 8+ applications.
- Usable from .NET Framework 4.7.2 / 4.8 WPF, WinForms, and enterprise applications.
- Fits legacy code-analysis use cases.
- Keeps Core OS-independent and UI-independent.
- Broadens the NuGet consumer base.
- The initial BDD/ZDD implementation only needs BCL APIs available on `netstandard2.0`.

Development, tests, samples, benchmarks, and CLI should use `.NET 8` or later.

Reasons:

- Better development experience.
- Good fit with MSTest, BenchmarkDotNet, and GitHub Actions.
- CLI and samples can use modern .NET convenience APIs.
- This balances library compatibility with development productivity.

Future performance optimization may introduce:

```xml
<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
```

Even then, `netstandard2.0` should remain the compatibility baseline.

### 13.2 Separate Core and Export

Reasons:

- Keep Core mathematically and algorithmically clean.
- Adding output formats does not affect Core.
- Tests are easier to isolate.

### 13.3 Use Separate Managers for BDD and ZDD

Reasons:

- Terminal semantics and reduction rules differ.
- Forced common abstraction reduces readability.
- Future optimization can be performed independently.

### 13.4 Provide High-Level APIs

Reasons:

- Friendly for BDD/ZDD beginners.
- Shorter samples.
- Easier adoption.

### 13.5 Support DOT/CSV/Markdown/AsciiDoc Early

Reasons:

- Necessary for learning, debugging, review, and documentation.
- Improves OSS presentation.
- Improves sample quality.

---

## 14. Initial Roadmap

### 14.0 TargetFramework Roadmap

| Version | TargetFramework policy |
|---|---|
| v0.1 | Core is `netstandard2.0` only. Tests/Samples/Benchmarks are `net8.0` |
| v0.2 | BDD is added while Core remains `netstandard2.0` |
| v0.3 | Unified developer experience is added while library projects remain `netstandard2.0` |
| v0.4 | MTBDD baseline is added while Core remains `netstandard2.0` |
| v0.5 | ZMTBDD baseline is added while Core remains `netstandard2.0` |
| v0.6 | Existing diagram refinement keeps Core `netstandard2.0` |
| v0.7 | Usability and sample work keeps library projects `netstandard2.0` |
| v0.8+ | CodeAnalysis and optional multi-targeting may be considered if needed |

### v0.1: ZDD Foundation

- `ZddManager`
- `Zdd`
- `Empty` / `Base`
- `MakeSet`
- `MakeFamily`
- `Union`
- `Intersect`
- `Difference`
- `Subset0`
- `Subset1`
- `Containing`
- `CountSets`
- `EnumerateSets`
- DOT export
- CSV / Markdown / AsciiDoc export
- ZDD set-family sample
- Fukashigi-style counting sample
- Unit tests
- Naive comparison tests
- Golden export tests
- Benchmark skeleton

### v0.2: BDD Foundation

- `BddManager`
- `Bdd`
- `True` / `False`
- `Var`
- `Not`
- `And`
- `Or`
- `Xor`
- `Ite`
- `Evaluate`
- `IsSatisfiable`
- `Implies`
- `Equivalent`
- Truth table
- Model enumeration
- DOT export
- CSV / Markdown / AsciiDoc export
- Feature flag sample

### v0.3: Unified Developer Experience

- `DecisionDiagramManager`
- `VariableTable`
- High-level APIs
- Extension methods
- README improvements
- NuGet metadata
- Documentation skeleton

### v0.4: MTBDD Baseline

- `MtbddManager`
- `Mtbdd`
- integer terminals
- terminal interning
- reduced ordered construction
- bounded truth-table construction
- `Evaluate`
- DOT output
- node/value table diagnostics
- statistics
- truth-table comparison tests
- construction/evaluation benchmarks

### v0.5: ZMTBDD Baseline

- ZMTBDD design note
- zero terminal semantics
- zero-suppressed reduction rule
- sparse numeric construction
- `Evaluate`
- DOT output
- node/value table diagnostics
- statistics
- naive sparse numeric comparison tests
- benchmarks against direct MTBDD

### v0.6: Existing Diagram Refinement and Test Coverage

- BDD/ZDD operation test refactoring with clearer purpose comments and shared naive-model helpers
- v0.1 and v0.2 coverage review, with method-level gaps closed where practical
- MTBDD/ZMTBDD edge-case tests for construction, evaluation, ownership, validation, and limits
- diagnostics and export golden-test stabilization for BDD, ZDD, MTBDD, and ZMTBDD
- benchmark cleanup for BDD `Ite`, ZDD family operations, MTBDD construction/evaluation, and ZMTBDD construction/evaluation
- documentation updates that describe the current supported diagram set without speculative future types

### v0.7: Usability, Samples, and CodeAnalysis Preparation

- friendlier high-level construction APIs for BDD, ZDD, MTBDD, and ZMTBDD
- fluent or extension-based helpers where they improve discoverability without hiding manager APIs
- additional samples for truth-table export, set-family workflows, and numeric diagram evaluation
- getting-started and API guide refresh based on the refined public surface
- CLI and CodeAnalysis preparation that does not add dependencies to Core
- NuGet metadata, README, and docs polish for a cleaner preview package story

### v0.8+: CodeAnalysis Prototype

- `IncludeEdge`
- `IncludePath`
- Include path ZDD
- Header contamination ranking
- Markdown/CSV/AsciiDoc report

### v0.9+: Conditional Analysis

- BDD build conditions
- Conditional include edges
- Feasible path filtering
- Configuration-aware impact analysis

### Out of Current Roadmap: MDD / ADD

- MDD and ADD / weighted DD are not planned.
- Do not add speculative abstractions solely for MDD/ADD.
- Reopen only by explicit project-owner decision.

---

## 15. Summary

`DecisionDiagramSharp` is designed not merely as a BDD/ZDD implementation, but as a decision diagram foundation that provides the following:

```text
Core:
  Mathematically clear and robust BDD/ZDD/MTBDD/ZMTBDD foundations

Diagnostics:
  Visualization, truth tables, and set-family display

Export:
  CSV / Markdown / AsciiDoc / DOT

CodeAnalysis:
  Include paths, conditional compilation, and impact-set analysis

Docs/Samples:
  Learnable, runnable, and extensible OSS assets
```

The most important principle is:

> Keep Core pure, keep the public API friendly, and treat Diagnostics and Export as first-class features from the beginning.

In addition, the library projects use `netstandard2.0` as the baseline target to ensure broad consumer compatibility, including .NET Framework 4.8-era WPF/WinForms applications. Development, tests, CLI, samples, and benchmarks use .NET 8 LTS or later to balance compatibility with development efficiency.

---

## 16. Test-Driven Development and Coverage Policy

DecisionDiagramSharp uses evidence-based completion management. Non-trivial implementation work must define tasks, completion criteria, verification methods, coverage targets, and completion evidence before being marked complete.

### 16.1 TDD Requirement

For behavior-changing Core work, contributors and AI agents must follow test-driven development unless the task is documentation-only, scaffolding-only, or exploratory design.

Required workflow:

1. Define expected behavior and completion criteria.
2. Add or update tests before production implementation where practical.
3. Confirm that the new or updated test fails for the expected reason.
4. Implement the minimum production code required to pass.
5. Run the relevant test suite.
6. Refactor while keeping tests green.
7. Record failing-test evidence, passing-test evidence, and coverage evidence.

BDD and ZDD semantic operations require test-first development.

### 16.2 Coverage Targets

Each implementation task must include a coverage target in its completion definition or explicitly state `N/A` with rationale.

| Work Area | Minimum Coverage Target |
|---|---|
| Core BDD/ZDD behavior | Line >= 90%; branch >= 85% for changed production code |
| Core validation and exception paths | Line >= 90%; branch >= 85% for changed production code |
| Diagnostics / Export | Line >= 85%; branch >= 75% for changed production code, plus golden tests where applicable |
| CodeAnalysis | Line >= 85%; branch >= 75% for changed production code |
| CLI / Samples | Coverage may be N/A, but build and run evidence is required |
| Documentation-only tasks | Coverage is N/A with documentation review evidence |
| Benchmarks | Coverage is N/A, but benchmark build/run evidence is required |

A task must not be marked complete if it fails its stated coverage target unless the exception is documented with rationale and follow-up work.

### 16.3 Required Task Table Columns

Persistent backlog and task execution tables must include the following columns:

| Column | Meaning |
|---|---|
| ID | Stable task identifier |
| Parent | Parent task ID if nested |
| Task | Specific implementation task |
| Completion Definition | Explicit Definition of Done |
| Verification Method | How completion is verified |
| Test First? | Whether TDD is required or explicitly N/A with rationale |
| Failing Test Evidence | Evidence that the new or updated test failed before implementation, or N/A rationale |
| Passing Test Evidence | Evidence that the relevant tests passed after implementation |
| Coverage Target | Required line/branch target or explicit N/A rationale |
| Coverage Evidence | Actual measured coverage result or N/A evidence |
| Status | Todo / InProgress / Blocked / Done |
| Evidence | Concrete evidence of completion |
