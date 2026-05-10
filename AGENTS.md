# AGENTS.md

This file defines instructions for AI coding agents, human contributors using AI assistance, and automation tools working on **DecisionDiagramSharp**.

The goal is to keep the project robust, maintainable, extensible, readable, well-tested, and friendly to users and contributors.

---

## 1. Project Summary

**DecisionDiagramSharp** is a modern C#/.NET library for decision diagrams.

Initial focus:

- **BDD**: Binary Decision Diagrams for Boolean functions and symbolic conditions
- **ZDD**: Zero-suppressed Decision Diagrams for sparse set families

Numeric decision diagram scope:

- **MTBDD**: Multi-Terminal Binary Decision Diagrams for integer-valued Boolean-domain functions
- **ZMTBDD**: Zero-suppressed MTBDDs for sparse numeric functions with many zero-valued regions

Other planned scope:

- symbolic code-analysis utilities, after the existing diagram refinement roadmap unless explicitly reprioritized

Out of current plan:

- **MDD**: Multi-valued Decision Diagrams
- **ADD / weighted decision diagrams**

MDD and ADD are not roadmap targets unless the project owner explicitly reopens them. Keep designs general enough that they do not make those families impossible, but do not add speculative MDD/ADD abstractions.

The project is intended to be:

- educational
- practical
- well-documented
- suitable for NuGet distribution
- easy to extend
- internally clean enough to support future optimized backends

---

## 2. Core Architectural Rules

### 2.1 Keep Core Pure

`DecisionDiagramSharp.Core` must not depend on:

- file I/O
- CLI argument parsing
- Graphviz process execution
- CSV / Markdown / AsciiDoc formatting
- MSBuild
- source-code parsing
- include dependency analysis
- GitHub or NuGet APIs
- UI frameworks

Core may contain:

- diagram handles
- managers
- node tables
- unique tables
- operation caches
- variable tables
- variable ordering
- construction APIs
- decision diagram operations
- traversal
- validation
- statistics

### 2.2 Respect Dependency Direction

Allowed dependency direction:

```text
Core <- Diagnostics <- Export
Core <- CodeAnalysis
Core <- Cli
Diagnostics <- Cli
Export <- Cli
CodeAnalysis <- Cli
```

Forbidden:

```text
Core -> Diagnostics
Core -> Export
Core -> CodeAnalysis
Core -> Cli
```

If a change introduces one of the forbidden dependencies, do not proceed. Propose an alternative design.

### 2.3 Do Not Over-Abstract BDD and ZDD

BDD and ZDD are similar structurally but different semantically.

Do not force them into a single generic operation model.

Commonize only:

- variable identifiers
- variable tables
- diagram statistics
- diagnostics concepts
- export table models
- validation conventions

Keep separate:

- terminal semantics
- reduction rules
- operation names
- manager internals
- unique tables
- apply caches
- traversal internals where semantics differ

### 2.4 Target Framework Policy

The library core should prioritize consumer compatibility.

Initial policy:

```text
DecisionDiagramSharp.Core         netstandard2.0
DecisionDiagramSharp.Diagnostics  netstandard2.0
DecisionDiagramSharp.Export       netstandard2.0
DecisionDiagramSharp.CodeAnalysis netstandard2.0 or net8.0 depending on dependencies
DecisionDiagramSharp.Cli          net8.0
Tests                             net8.0
Benchmarks                        net8.0
Samples                           net8.0
Future WPF viewer                 net8.0-windows
```

Rules:

- Core must remain compatible with `netstandard2.0`.
- Core must not depend on WPF, WinForms, Windows-only APIs, CLI frameworks, Graphviz execution, or file-system-specific workflows.
- Do not use .NET 6/7/8-only APIs in Core unless the project has explicitly introduced multi-targeting.
- If multi-targeting is introduced, the `netstandard2.0` implementation must remain correct and tested.
- Use `.NET 8` for test projects, samples, benchmarks, and command-line tools.
- WPF integration, if added, must live in a separate project such as `DecisionDiagramSharp.Wpf`.

---

## 3. Diagram Semantics

### 3.1 BDD

BDD represents Boolean functions.

Terminals:

```text
0 = False
1 = True
```

Reduction rule:

```text
If Low == High, remove the node and return Low.
```

Initial implementation policy:

- Use straightforward reduced ordered BDD.
- Do not add complement edges in the first implementation unless explicitly requested.
- Keep the public API stable enough that complement edges can be added internally later.

Primary operations:

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

### 3.2 ZDD

ZDD represents sparse set families.

Terminals:

```text
0 = Empty = {}
1 = Base  = {{}}
```

Reduction rule:

```text
If High == Empty, remove the node and return Low.
```

Primary operations:

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

Important distinction:

- `Subset1(f, x)` selects sets containing `x` and removes `x`.
- `Containing(f, x)` selects sets containing `x` while preserving `x`.

Do not conflate these operations.

### 3.3 MTBDD

MTBDD represents total functions:

```text
f : {0,1}^n -> T
```

where terminals are values such as integers, labels, or other immutable value-domain elements.

Initial MTBDD implementation priority:

- integer terminals first
- reduced ordered MTBDD
- separate `Mtbdd` handle and `MtbddManager`
- terminal interning / unique terminal table
- non-terminal unique table
- construction from bounded truth tables or explicit assignments
- `Evaluate`
- statistics and diagnostics
- comparison against naive truth-table models in tests

Do not fold MTBDD into BDD or ZDD managers. MTBDD has different terminal semantics and should keep its own manager internals.

### 3.4 ZMTBDD

ZMTBDD is a zero-suppressed MTBDD variant for sparse numeric functions.

Reduction policy to design and test before implementation:

```text
If High is the zero terminal, remove the node and return Low.
```

The zero terminal represents an actual numeric zero in the represented function.

### 3.5 MDD and ADD Out of Plan

MDD and ADD / weighted DD are currently out of plan.

Do not implement MDD or ADD unless specifically requested by the project owner. When designing APIs, avoid choices that would make them impossible, but do not add speculative complexity for them.

---

## 4. Public API Guidelines

### 4.1 Typed Handles

Use separate handle types.

```csharp
public readonly struct Bdd : IEquatable<Bdd> { ... }
public readonly struct Zdd : IEquatable<Zdd> { ... }
```

Do not represent public BDD/ZDD values as raw `int`.

Handles must prevent accidental mixing of BDD and ZDD values.

### 4.2 Manager Ownership

A diagram value belongs to exactly one manager.

Any operation combining operands must validate that all operands belong to the same manager.

Throw a specific exception on mismatch.

Recommended exception:

```csharp
DiagramManagerMismatchException
```

Error messages should explain what happened and how to fix it.

Bad:

```text
Invalid operation.
```

Good:

```text
The two BDD operands belong to different BddManager instances. BDD values can only be combined when they are created by the same manager.
```

### 4.3 Friendly High-Level API

The library should be easy to use.

BDD example target:

```csharp
var dd = new DecisionDiagramManager();

var A = dd.Bdd.Var("A");
var B = dd.Bdd.Var("B");

var f = A & !B;

Console.WriteLine(f.ToMarkdownTruthTable());
File.WriteAllText("f.dot", f.ToDot());
```

ZDD example target:

```csharp
var dd = new DecisionDiagramManager();

var family = dd.Zdd.SetFamily<string>()
    .AddSet(["A.h -> Common.h", "Common.h -> Windows.h"])
    .AddSet(["B.h -> LegacyBase.h", "LegacyBase.h -> Windows.h"]);

Console.WriteLine(family.Containing("Common.h -> Windows.h").ToMarkdown());
```

When adding features, prefer APIs that are:

- explicit
- type-safe
- discoverable in IDE autocomplete
- documented with XML comments
- usable in samples

### 4.4 Low-Level API Must Remain Available

High-level APIs must not hide the existence of manager-level APIs.

Keep manager APIs suitable for algorithmic work.

---

## 5. Internal Implementation Guidelines

### 5.1 Core Hot Path Rules

In Core hot paths, avoid:

- LINQ
- tuple allocations
- class-per-node representation
- unnecessary string operations
- reflection
- file I/O
- global mutable state
- avoidable heap allocations inside recursive apply operations

Prefer:

- `int` node IDs internally
- `readonly struct` node records
- `readonly struct` cache keys
- `Dictionary<TKey, TValue>` with struct keys
- `List<T>` or array-backed storage
- explicit operation caches
- deterministic construction
- clear validation boundaries

When targeting `netstandard2.0`, do not use APIs that are unavailable on `netstandard2.0` in Core hot paths.

Avoid:

- `CollectionsMarshal`
- `FrozenDictionary`
- `FrozenSet`
- `PriorityQueue`
- `DateOnly`
- `TimeOnly`
- WPF / WinForms APIs
- OS-specific APIs

Prefer:

- `List<T>`
- `Dictionary<TKey, TValue>`
- `HashSet<T>`
- `StringBuilder`
- `readonly struct`
- struct cache keys
- explicit validation

### 5.2 Node Types Are Internal

Do not expose node structures publicly.

Examples:

```csharp
internal readonly record struct BddNode(int Variable, int Low, int High);
internal readonly record struct ZddNode(int Variable, int Low, int High);
```

Public APIs should expose handles and views, not mutable internal nodes.

### 5.3 Unique Table Is Mandatory

All node creation must go through a canonical `MakeNode` path.

BDD `MakeNode` must apply:

```text
if low == high return low
```

ZDD `MakeNode` must apply:

```text
if high == Empty return low
```

Then use the unique table.

Do not manually append nodes from operation code.

### 5.4 Operation Cache

Operations such as BDD `Ite` and ZDD `Union` / `Intersect` / `Difference` should use operation caches where practical.

Cache keys must be deterministic and allocation-conscious.

For commutative operations, normalize operands if it improves cache hit rate.

### 5.5 Validation

Managers should provide a `Validate()` method.

Validation should check:

- terminal consistency
- node ID ranges
- variable ordering consistency
- unique table consistency
- reduction rule consistency
- reachable nodes if validating a root

---

## 6. Diagnostics and Export Rules

### 6.1 Diagnostics Are First-Class

Diagnostics are not optional extras.

Initial implementation should include:

- DOT output
- node table dump
- BDD truth table
- BDD model enumeration
- ZDD set-family enumeration
- statistics

### 6.2 Core Must Not Format CSV / Markdown / AsciiDoc

Formatting belongs in `DecisionDiagramSharp.Export`.

Diagnostics should build intermediate models.

Exporters should convert intermediate models to text.

Recommended flow:

```text
Diagram -> Diagnostics Model -> TableModel -> CSV / Markdown / AsciiDoc
```

### 6.3 DOT Is Separate

DOT is graph output, not table output.

DOT belongs in Diagnostics.

Do not run Graphviz from Core.

If Graphviz execution is needed, place it in CLI or a dedicated optional integration.

### 6.4 Export Formats

Initial supported formats:

- CSV
- Markdown
- AsciiDoc
- DOT

For table formats, use dedicated exporters:

- `CsvTableExporter`
- `MarkdownTableExporter`
- `AsciiDocTableExporter`

Do not duplicate diagram traversal logic in each exporter.

### 6.5 Bounded Enumeration

Truth tables and set enumeration can explode.

Default options must include safety limits.

Examples:

```csharp
TruthTableOptions.MaxVariables = 16;
TruthTableOptions.MaxRows = 65536;

SetEnumerationOptions.MaxSets = 1000;
ModelEnumerationOptions.MaxModels = 1000;
```

If limits are exceeded, throw an actionable exception.

Recommended exception:

```csharp
DiagramEnumerationLimitExceededException
```

---

## 7. Code Analysis Layer Rules

`DecisionDiagramSharp.CodeAnalysis` is optional and must not pollute Core.

Initial code-analysis focus:

- include graph
- include edge
- include path
- include path family as ZDD
- header contamination ranking

Preferred ZDD mapping:

```text
ZDD variable = include edge
ZDD set      = one include path
ZDD family   = all include paths to a target header
```

Future BDD + ZDD mapping:

```text
BDD = build condition / macro condition
ZDD = path structure
```

Do not implement a full C/C++ parser in this project unless explicitly requested.

Prefer consuming outputs from:

- MSVC `/sourceDependencies`
- clang tooling
- IWYU
- clangd
- existing include graph tools

---

## 8. Documentation Rules

### 8.1 Public APIs Require XML Documentation

Every public type, method, property, and exception should have XML documentation comments.

The comments must explain:

- what the API does
- important semantic details
- limitations or explosion risks
- ownership/manager rules where relevant

### 8.2 README Must Stay Beginner-Friendly

The README should include:

- one-sentence project description
- BDD quick start
- ZDD quick start
- DOT export example
- CSV/Markdown/AsciiDoc export example
- installation via NuGet
- links to docs and samples

### 8.3 Docs Are Part of the Product

Maintain docs under:

```text
docs/
  getting-started.md
  architecture.md
  concepts/
  tutorials/
  api-guides/
  design/
  contributing/
```

### 8.4 Samples Must Build

Every sample project should be included in CI build checks.

Samples should be small, readable, and focused.

Recommended samples:

- `Bdd.BasicLogic`
- `Bdd.TruthTableExport`
- `Bdd.FeatureFlags`
- `Zdd.SetFamilies`
- `Zdd.FukashigiCounting`
- `Zdd.GraphPathCounting`
- `Zdd.IncludePathFamilies`
- `Export.AllFormats`
- `CodeAnalysis.HeaderContamination`

### 8.5 AI-Generated Documentation Requires Review

AI may draft documentation, but technical claims must be reviewed.

Do not add unsupported performance claims.

Do not claim compatibility or algorithmic guarantees not covered by tests.

---

## 9. Testing Rules

### 9.1 Tests Are Required for Behavior Changes

Every new operation must include tests.

At minimum:

- MSTest unit tests
- edge-case tests
- invalid input tests where applicable

### 9.1.1 Test-Driven Development Is Required for Core Behavior

For behavior-changing Core work, agents MUST follow TDD unless the task is explicitly marked as documentation-only, scaffolding-only, or exploratory design.

Required order:

1. Define behavior and completion criteria.
2. Add or update tests first.
3. Confirm the new or updated test fails for the expected reason.
4. Implement the smallest production change that makes the test pass.
5. Run the relevant test suite.
6. Refactor while preserving green tests.
7. Record failing-test evidence, passing-test evidence, and coverage evidence in the task table.

BDD, ZDD, MTBDD, and ZMTBDD semantic operations MUST use test-first development. Do not mark such tasks as `Done` without test evidence.

### 9.1.2 Coverage Requirements

Every non-trivial implementation task MUST declare a coverage target in its task table.

For v0.3 and later implementation tasks, changed production code must reach 100% method coverage. This is a quality gate for meaningful unit tests, not a license to write shallow tests that merely execute lines. Coverage evidence must be reviewed at method/function granularity so that no changed function remains at 0% coverage.

Default minimum targets:

| Work Area | Minimum Coverage Target |
|---|---|
| Core BDD/ZDD/MTBDD/ZMTBDD behavior | Method = 100% for changed production code; line >= 90%; branch >= 85% |
| Core validation and exception paths | Method = 100% for changed production code; line >= 90%; branch >= 85% |
| Diagnostics / Export | Method = 100% for changed production code; line >= 85%; branch >= 75%, plus golden tests where applicable |
| CodeAnalysis | Method = 100% for changed production code; line >= 85%; branch >= 75% |
| CLI / Samples | Coverage may be N/A, but build and run evidence is required |
| Documentation-only tasks | Coverage is N/A with review evidence |
| Benchmarks | Coverage is N/A, but benchmark build/run evidence is required |

Coverage evidence MUST be concrete. Acceptable evidence includes coverage report filenames, measured line/branch percentages, or CI coverage output.

A task MUST NOT be marked `Done` if the stated coverage target is not met, unless the exception is documented with rationale and follow-up work.

### 9.1.3 Unit Test Quality

For v0.3 and later implementation tasks:

- Each test method must state its purpose at the beginning of the test body with an English comment such as `// Purpose: ...`.
- Unit tests must follow Arrange / Act / Assert structure where practical.
- Tests that only trace a function call without asserting meaningful behavior are not acceptable.
- Edge cases, invalid inputs, ownership mismatches, and formatting stability should be covered where relevant.

### 9.2 ZDD Tests

For ZDD operations, compare against a naive set-family implementation for small randomized cases.

Naive model:

```text
HashSet<SortedSet<int>>
```

Required comparison targets:

- `Union`
- `Intersect`
- `Difference`
- `Subset0`
- `Subset1`
- `Containing`
- `CountSets`
- `EnumerateSets`
- `ContainsSet`

### 9.3 BDD Tests

For BDD operations, compare against truth-table evaluation for small variable counts.

Required comparison targets:

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

### 9.4 MTBDD/ZMTBDD Tests

For MTBDD and ZMTBDD operations, compare against naive integer truth-table models for small variable counts.

Required comparison targets:

- MTBDD construction and `Evaluate`
- ZMTBDD construction, zero-suppression behavior, and `Evaluate`

### 9.5 Golden Tests

Use golden tests for stable text outputs.

Targets:

- DOT
- CSV
- Markdown
- AsciiDoc

If output format intentionally changes, update golden files and document the reason.

### 9.6 Benchmark Skeleton

Performance-sensitive operations should have BenchmarkDotNet coverage.

Initial benchmark targets:

- ZDD `MakeFamily`
- ZDD `Union`
- ZDD `Difference`
- BDD `Ite`
- BDD truth-table generation
- MTBDD construction
- ZMTBDD construction
- exporter formatting

Do not micro-optimize before correctness tests are solid.

---

## 10. Error Handling Rules

Use specific exceptions.

Recommended hierarchy:

```csharp
public class DiagramException : Exception { }

public sealed class DiagramManagerMismatchException : DiagramException { }
public sealed class DiagramSizeLimitExceededException : DiagramException { }
public sealed class DiagramEnumerationLimitExceededException : DiagramException { }
public sealed class InvalidVariableOrderingException : DiagramException { }
```

Error messages must be actionable.

Include:

- what failed
- why it failed
- how the user can proceed

---

## 11. Performance Rules

### 11.1 Correctness First, Obvious Pitfalls Avoided

Initial implementation should prioritize correctness and clarity.

However, do not introduce obvious performance problems in Core.

Avoid:

- class node objects
- per-call LINQ in recursive operations
- repeated sorting in hot paths
- repeated variable-name lookup in hot paths
- unbounded enumeration by default

### 11.2 Measure Before Major Optimization

Before introducing complex optimizations, add or update benchmarks.

Do not add complement edges, dynamic reordering, or native backends without:

- design note
- tests
- benchmark comparison
- migration plan

---

## 12. NuGet and Packaging Rules

Recommended package ID:

```text
DecisionDiagramSharp
```

Initial package structure may be single-package.

Future split is allowed:

```text
DecisionDiagramSharp.Core
DecisionDiagramSharp.Diagnostics
DecisionDiagramSharp.Export
DecisionDiagramSharp.CodeAnalysis
```

Recommended license:

```text
Apache-2.0
```

Package metadata should include:

- BDD
- ZDD
- MTBDD
- ZMTBDD
- decision diagrams
- binary decision diagram
- zero-suppressed decision diagram
- multi-terminal binary decision diagram
- C#
- .NET
- symbolic computation
- numeric correction
- calibration table
- approximate arithmetic
- truth table
- Graphviz
- AsciiDoc
- static analysis

Use `PackageLicenseExpression`, not deprecated license URL metadata.

---

## 13. GitHub Discoverability Rules

Repository description should include:

```text
Modern C#/.NET library for BDD, ZDD, MTBDD, ZMTBDD and decision diagrams: symbolic Boolean logic, sparse set families, numeric correction functions, truth tables, DOT/CSV/Markdown/AsciiDoc export, and code-analysis examples.
```

Recommended topics:

```text
bdd
zdd
mtbdd
zmtbdd
decision-diagrams
binary-decision-diagram
zero-suppressed-decision-diagram
multi-terminal-bdd
csharp
dotnet
symbolic-computation
boolean-algebra
logic
numeric-correction
calibration-table
approximate-computing
set-family
graph-algorithms
combinatorics
truth-table
graphviz
asciidoc
static-analysis
code-analysis
legacy-code
formal-methods
```

README should include both abbreviations and full names:

- BDD / Binary Decision Diagram
- ZDD / Zero-suppressed Decision Diagram
- MTBDD / Multi-Terminal Binary Decision Diagram
- ZMTBDD / Zero-suppressed Multi-Terminal Binary Decision Diagram

---

## 14. Pull Request Checklist

Before submitting a PR, verify:

- [ ] The change follows the architecture dependency rules.
- [ ] Core does not depend on Diagnostics, Export, CodeAnalysis, or CLI.
- [ ] Core remains compatible with `netstandard2.0` unless multi-targeting has been explicitly introduced.
- [ ] Core does not use WPF, WinForms, Windows-only APIs, Graphviz execution, or CLI framework APIs.
- [ ] Public APIs have XML documentation comments.
- [ ] Manager ownership validation is preserved.
- [ ] BDD/ZDD semantics are not conflated.
- [ ] New behavior has MSTest unit tests.
- [ ] ZDD operations are tested against naive set-family behavior where practical.
- [ ] BDD operations are tested against truth-table behavior where practical.
- [ ] Export output changes include golden test updates.
- [ ] Samples still build.
- [ ] README or docs are updated for user-facing changes.
- [ ] Benchmarks are added or updated for performance-sensitive changes.

---

## 15. Common Tasks for AI Agents

### 15.1 Add a New ZDD Operation

Steps:

1. Define semantics in comments or docs.
2. Add manager method.
3. Implement using internal node IDs.
4. Use unique table only through `MakeNode`.
5. Add operation cache if recursive.
6. Add MSTest unit tests.
7. Add naive set-family comparison tests.
8. Add documentation and example if public.
9. Add benchmark if performance-sensitive.

### 15.2 Add a New BDD Operation

Steps:

1. Define Boolean semantics.
2. Prefer implementing via `Ite` unless a direct implementation is justified.
3. Add manager method.
4. Validate manager ownership.
5. Add truth-table comparison tests.
6. Add docs and samples if public.

### 15.3 Add a New Export Format

Steps:

1. Do not traverse diagrams directly from the exporter.
2. Reuse intermediate table models.
3. Add exporter options.
4. Add golden tests.
5. Add sample under `samples/Export.*`.
6. Update README/export docs.

### 15.4 Add a Code Analysis Feature

Steps:

1. Keep it outside Core.
2. Model the domain explicitly.
3. Convert domain structures into BDD/ZDD inputs.
4. Keep parsing/import separate from symbolic analysis.
5. Add sample and report export.
6. Add tests using small synthetic graphs.

### 15.5 Add an MTBDD or ZMTBDD Feature

Steps:

1. Define exact integer-function semantics before implementation.
2. Keep MTBDD and ZMTBDD managers or construction APIs semantically separate where reduction rules or terminal meanings differ.
3. Add tests first against a naive truth-table model.
4. Validate manager ownership.
5. Ensure all node creation goes through the canonical `MakeNode` path.
6. Add diagnostics, statistics, and bounded table output where user-facing.
7. Add comparison benchmarks when the operation affects MTBDD or ZMTBDD performance claims.
8. Document whether the task is baseline infrastructure, usability refinement, diagnostics/export work, or benchmark work.

---

## 16. Do Not Do These Things

Do not:

- expose internal node structures publicly
- mix BDD and ZDD handle types
- bypass `MakeNode`
- add file I/O to Core
- add Graphviz process execution to Core
- implement a C/C++ parser inside Core
- create unbounded enumeration APIs as the default
- use vague exceptions
- add public APIs without XML docs
- add behavior without tests
- over-optimize before measuring
- make unsupported claims in documentation
- copy copyrighted lecture slides or diagrams into the repository
- implement MDD or ADD / weighted DD without an explicit project-owner decision reopening that roadmap
- introduce .NET 6/7/8-only APIs into Core while it is `netstandard2.0` only
- add WPF or WinForms dependencies to Core
- make Core target `net8.0-windows`

For educational samples inspired by external materials, create original examples and cite the inspiration in documentation.

---

## 17. External References Policy

External references may be cited in docs, for example:

- BDD
- ZDD
- CUDD
- Minato-style ZDD applications
- Fukashigi-style combinatorial counting
- Graphviz
- NuGet packaging

Do not copy large portions of external documents.

Use original examples and original diagrams unless the license explicitly allows reuse.

---

## 18. Initial Milestones

### v0.1: ZDD Foundation

Must include:

- ZDD core operations
- DOT output
- CSV / Markdown / AsciiDoc set-family output
- node dump
- statistics
- tests against naive set families
- golden export tests
- ZDD set-family sample
- Fukashigi counting sample

### v0.2: BDD Foundation

Must include:

- BDD core operations
- truth table
- model enumeration
- DOT output
- CSV / Markdown / AsciiDoc truth-table output
- feature flag sample
- truth-table comparison tests

### v0.3: Unified Developer Experience

Must include:

- `DecisionDiagramManager`
- friendly APIs
- extension methods
- README improvements
- NuGet metadata
- documentation skeleton

### v0.4: MTBDD Baseline

Must include:

- integer-valued MTBDD core
- typed MTBDD handle and manager
- reduced ordered construction
- evaluation
- diagnostics/statistics
- truth-table comparison tests
- construction/evaluation benchmarks

### v0.5: ZMTBDD Baseline

Must include:

- ZMTBDD semantics and reduction-rule design note
- typed ZMTBDD handle or explicitly separated construction API
- zero-suppressed construction and evaluation
- comparison tests against naive sparse numeric functions
- diagnostics/statistics
- benchmarks against MTBDD

### v0.6: Existing Diagram Refinement and Test Coverage

Must include:

- BDD/ZDD test helper refactoring
- v0.1/v0.2 coverage review and meaningful gap closure
- MTBDD/ZMTBDD edge-case tests
- diagnostics/export golden test stabilization
- benchmark cleanup for supported diagram families
- documentation updates for the current supported diagram set

### v0.7: Usability, Samples, and CodeAnalysis Preparation

Must include:

- high-level API refinement for BDD, ZDD, MTBDD, and ZMTBDD
- discoverable construction and export helpers
- refreshed BDD/ZDD/MTBDD/ZMTBDD samples
- getting-started and API guide refresh
- CodeAnalysis boundary design outside Core
- NuGet metadata and README polish

---

## 19. Guiding Statement

When in doubt, follow this rule:

> Keep the core mathematically clean, keep the public API friendly, and keep diagnostics, export, tests, documentation, and samples as first-class parts of the project.

Additionally, keep the library body broadly consumable by preserving `netstandard2.0` compatibility unless there is an explicit, tested, documented multi-targeting decision.
