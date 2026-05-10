# Definition of Done Policy

## 1. Purpose

This document defines the mandatory task execution and completion verification policy for DecisionDiagramSharp.

The goals are:

- prevent ambiguous completion claims
- improve AI-assisted development quality
- make progress traceable
- support long-term backlog management
- provide evidence-based completion reporting
- reduce hidden incomplete work
- preserve project continuity

This policy applies to:

- human contributors
- AI coding agents
- automation workflows
- pull requests
- generated implementation plans

---

## 2. Mandatory Workflow

Before implementing any non-trivial change, contributors MUST:

1. Define tasks.
2. Define sub-tasks where necessary.
3. Define explicit completion criteria.
4. Define verification methods.
5. Track implementation status.
6. Record evidence for completion.

Work MUST NOT be considered complete without verification evidence.

---

## 2.1 Test-Driven Development Policy

Behavior-changing work MUST follow a test-driven workflow unless the task is documentation-only, pure scaffolding, exploratory design, or explicitly marked as an exception with rationale.

Required TDD workflow:

1. Define the expected behavior and observable acceptance criteria.
2. Add or update tests before production implementation where practical.
3. Confirm that at least one new or updated test fails for the expected reason.
4. Implement the minimum production code needed to pass the test.
5. Run the relevant test suite.
6. Refactor while keeping tests green.
7. Record failing-test evidence, passing-test evidence, and coverage evidence in the task table.

For Core BDD/ZDD/MTBDD/ZMTBDD operations, test-first development is REQUIRED. A Core behavior task MUST NOT be marked `Done` unless it has test evidence or a documented exception approved in the task table.

---

## 2.2 Coverage Policy

Completion definitions MUST include a concrete coverage target or an explicit `N/A` rationale.

For v0.3 and later implementation tasks, completion requires 100% method coverage for changed production code. The purpose is not to chase line coverage mechanically; it is to require meaningful unit tests that exercise every public or internal function introduced or changed by the task. Coverage evidence MUST be reviewed at method/function granularity so that no changed function remains at 0% coverage.

Default minimum targets:

| Work Area | Minimum Coverage Target |
|---|---|
| Core BDD/ZDD/MTBDD/ZMTBDD behavior | Method = 100% for changed production code; line >= 90%; branch >= 85% |
| Core safety and validation logic | Method = 100% for changed production code; line >= 90%; branch >= 85% |
| Diagnostics / Export | Method = 100% for changed production code; line >= 85%; branch >= 75%, plus golden tests where applicable |
| CodeAnalysis | Method = 100% for changed production code; line >= 85%; branch >= 75% |
| CLI / Samples | Coverage may be N/A, but build and run evidence is required |
| Documentation-only tasks | Coverage is N/A with documentation review evidence |
| Benchmarks | Coverage is N/A, but benchmark build/run evidence is required |

Coverage evidence SHOULD include the command used and the measured result.

Example:

```text
coverage.cobertura.xml: line 92.4%, branch 87.1% for DecisionDiagramSharp.Core
```

A task MUST NOT be marked `Done` if it fails its stated coverage target unless the exception is documented with rationale and follow-up work.

### 2.3 Unit Test Quality Policy

For v0.3 and later implementation tasks, unit tests MUST be written for behavior, not merely to execute functions.

The goal is that test code reads as a specification document for the library's public API. A failing test must make it immediately clear which specification was violated and under what input conditions.

#### 2.3.1 XML Documentation Comments

Every test method MUST have an XML documentation comment with the following structure:

```csharp
/// <summary>
/// 概要:
/// What this test verifies, stated as a specification claim.
/// </summary>
/// <remarks>
/// 狙い:
/// Which specification this test guards. Why this matters for the library.
/// What regression this test prevents. Whether this verifies logical semantics,
/// canonical representation, or API contracts.
/// </remarks>
```

The comment MUST describe the specification being verified, not the procedure being executed.

Bad example:
```csharp
/// <summary>
/// Creates A and B and evaluates Or.
/// </summary>
```

Good example:
```csharp
/// <summary>
/// 概要:
/// Verifies that compound Boolean expression evaluation matches an explicit truth table.
/// </summary>
/// <remarks>
/// 狙い:
/// Confirms that Evaluate returns specification-correct results for And, Or, Not, and Xor
/// combinations after BDD construction. By stating expected values as an explicit table
/// rather than recomputing them, the test reads as a specification document.
/// </remarks>
```

The legacy `// Purpose: ...` inline comment is superseded by this XML comment requirement for all new and refactored test methods from v0.6 onward.

#### 2.3.2 Arrange / Act / Assert Structure

Each test MUST be structured with `// Arrange`, `// Act`, and `// Assert` comments. When Act and Assert repeat inside a loop, the overall loop structure must still be readable:

```csharp
// Arrange
var cases = new[] { ... };

foreach (var c in cases)
{
    // Act
    var actual = manager.Evaluate(expr, BuildAssignment(c));

    // Assert
    Assert.AreEqual(c.Expected, actual, $"A={c.A}, B={c.B}");
}
```

#### 2.3.3 Test Naming

Test method names MUST express the specification, not just the API name.

Avoid:
- `Ite_Implies_Equivalent_Restrict_Quantifiers_Work`
- `EnumerateModels_And_Limits_Work`

Prefer names that encode: what is being tested, under what condition, and what the expected result is:
- `Implies_ShouldBeEquivalentToNotAOrB`
- `Restrict_AndByTrueVariable_ShouldReturnRemainingOperand`
- `EnumerateModels_ShouldThrowWhenModelCountExceedsMaxModels`
- `BinaryOperation_ShouldThrowWhenManagersDoNotMatch`

#### 2.3.4 One Test, One Specification

Each test method SHOULD verify one specification claim. Tests that bundle multiple independent specifications MUST be split unless the specifications are logically inseparable.

Mandatory split examples:
- `Ite`, `Implies`, `Equivalent`, `Restrict`, `Exists`, `ForAll` are separate specifications
- `EnumerateModels` normal path, limit enforcement, and boundary values are separate
- Manager mismatch, null arguments, unknown VariableId, and missing assignment are separate

#### 2.3.5 Explicit Truth Tables

Tests that verify logical semantics MUST express expected values as explicit case tables, not as recomputed logical expressions.

Avoid:
```csharp
var expected = (av && !bv) || (av ^ bv);
```

Prefer:
```csharp
var cases = new[]
{
    new { A = false, B = false, Expected = false },
    new { A = true,  B = false, Expected = true  },
    new { A = false, B = true,  Expected = true  },
    new { A = true,  B = true,  Expected = false },
};
```

Computing expected values with the same logic as the implementation under test makes the test unable to catch the most common bugs.

#### 2.3.6 Failure Messages

Assert calls in truth-table tests and multi-case loops MUST include a failure message that contains:
- The input values in specification terms (not as a bit mask)
- The expected value
- Which specification case was violated

```csharp
Assert.AreEqual(
    c.Expected,
    actual,
    $"A={c.A}, B={c.B}: result should match the explicit truth table.");
```

#### 2.3.7 Logical Equivalence vs. Node Identity

Tests MUST distinguish between two different properties:

1. **Logical equivalence**: the BDD represents the same Boolean function. Verify using `Evaluate` or `Equivalent`.
2. **Canonical representation**: the same Boolean function produces the same BDD node (reduced ordered BDD property). Verify using `Assert.AreEqual(node1, node2)`.

When `Assert.AreEqual` compares two `Bdd` values, the XML comment MUST state whether the test is asserting logical equivalence or canonical node identity.

#### 2.3.8 EnumerateModels Don't-Care Specification

Tests for `EnumerateModels` MUST explicitly verify the don't-care variable expansion behavior:

- Variables not appearing in the expression are still included in each model
- Both `true` and `false` assignments for don't-care variables appear in the enumerated set
- The total model count matches the expected count including don't-care expansions
- Assertions MUST NOT depend on enumeration order

#### 2.3.9 Exception Tests as API Contracts

Exception tests MUST read as API contract specifications. The XML comment MUST name the specific input contract being violated. Required cases for BDD/ZDD managers:

- Cross-manager operands: `DiagramManagerMismatchException`
- Null string variable name: `ArgumentNullException`
- Unknown `VariableId`: `ArgumentOutOfRangeException`
- Missing assignment entry: `ArgumentException`
- Null assignment dictionary: `ArgumentNullException`
- `MaxModels = 0` or negative: `ArgumentOutOfRangeException`
- Model count exceeds `MaxModels`: `DiagramEnumerationLimitExceededException`

Where the parameter name is a stable public contract, `ArgumentNullException.ParamName` and `ArgumentException.ParamName` SHOULD be verified.

#### 2.3.10 Required Test Coverage Areas

In addition to the above structural rules, BDD and ZDD test suites MUST include tests for the following specification areas. Tests that cover multiple items in one area are acceptable; tests that skip an area entirely are not.

| Area | Key Cases |
|---|---|
| **A. Terminals** | `True` / `False` evaluation; `Not(True)`, `Not(False)` |
| **B. Identity laws** | `A && A == A`; `A \|\| A == A`; `A && False == False`; `A \|\| True == True`; `A && True == A`; `A \|\| False == A`; `A ^ A == False`; `!!A == A` |
| **C. De Morgan** | `!(A && B) == !A \|\| !B`; `!(A \|\| B) == !A && !B` |
| **D. Implies / Equivalent** | `A => B == !A \|\| B`; `A <=> B == !(A ^ B)`; self-implication; self-equivalence |
| **E. ITE** | `Ite(True, X, Y) == X`; `Ite(False, X, Y) == Y`; Restrict selects correct branch |
| **F. Quantification** | `Exists(A, A && B) == B`; `ForAll(A, A && B) == False`; XOR cases |
| **G. Restrict** | Restrict And/Or by true/false; variable not in subtree |
| **H. Canonical form** | Same variable → same node; `A && A == A` canonically; `Ite(A, True, False) == A` |
| **I. Variable ordering** | Logical result is independent of variable registration order |
| **J. Extra / missing variables** | Missing required assignment throws; surplus assignments in assignment dictionary |
| **K. Boundary values** | `MaxModels = 1`; result count equals limit; result count exceeds limit; all null/invalid input cases |

These areas apply to `BddManagerTests.cs`. `ZddManagerTests.cs` MUST cover the equivalent ZDD-specific areas.

#### 2.3.11 Helper Methods

Shared helper methods are encouraged where they eliminate duplication without hiding the specification. Acceptable helpers:

- Manager and variable setup (`CreateManager`, `BuildBoolAssignment`)
- Explicit truth-table case construction
- Model enumeration normalization for order-independent comparison

Unacceptable helpers:

- Helpers that encapsulate the expected-value calculation
- Helpers that make the Assert invisible from the test body
- Helpers that mix Arrange, Act, and Assert into a single call

#### 2.3.12 White-Box and Reflection-Based Tests

Public API specification tests MUST NOT depend on private implementation details.

Tests that intentionally inspect or corrupt internal implementation state are allowed only as internal invariant tests. They MUST be isolated from normal public API tests and categorized clearly:

```csharp
[TestCategory("WhiteBox")]
[TestCategory("InternalInvariant")]
[TestCategory("Fragile")]
```

Examples of white-box dependencies:

- `BindingFlags.NonPublic`
- `GetPrivateField`
- `GetNestedType`
- `Activator.CreateInstance` for private node/key types
- invoking private methods through reflection
- reading private handle properties such as `NodeId`
- mutating private `_nodes`, `_uniqueTable`, or cache fields

White-box tests MUST explain why the invariant cannot be verified through public APIs. They MUST NOT be used as a substitute for public API semantic tests.

#### 2.3.13 Hash Code Assertions

Tests MUST follow the .NET hash-code contract:

- Equal values MUST have equal hash codes.
- Unequal values are NOT required to have different hash codes.

Therefore tests MUST NOT assert that two unequal values have different hash codes.

Bad:

```csharp
Assert.AreNotEqual(first.GetHashCode(), third.GetHashCode());
```

Good:

```csharp
Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
Assert.IsFalse(first.Equals(third));
```

Dictionary lookup behavior may also be used to verify equality and hash-code behavior together.

#### 2.3.14 Coverage Is Not Test Quality

Coverage evidence is required by §2.2, but high coverage MUST NOT be treated as sufficient proof of test quality.

In particular:

- A test that only executes private helpers through reflection is not a strong specification test.
- A test that builds its expected result with the same library operation under test is not an independent semantic oracle.
- A test that asserts implementation details without documenting the public contract may block useful refactoring.

Completion evidence for test-quality tasks MUST mention oracle quality, purpose separation, and refactoring resistance where those are relevant to the task.

---

## 3. Required Task Table Format

Tasks MUST be represented as Markdown tables.

Required columns:

| Column | Meaning |
|---|---|
| ID | Stable task identifier |
| Parent | Parent task ID if nested |
| Task | Specific implementation task |
| Completion Definition | Explicit Definition of Done |
| Verification Method | How completion is verified |
| Test First? | Whether test-first workflow is required, yes, or explicitly N/A with rationale |
| Failing Test Evidence | Evidence that a new or updated test failed before implementation, or N/A rationale |
| Passing Test Evidence | Evidence that the relevant tests passed after implementation |
| Coverage Target | Required line/branch coverage target or explicit N/A rationale |
| Coverage Evidence | Actual measured coverage result or N/A evidence |
| Status | Todo / InProgress / Blocked / Done |
| Evidence | Concrete evidence of completion |

Template:

| ID | Parent | Task | Completion Definition | Verification Method | Test First? | Failing Test Evidence | Passing Test Evidence | Coverage Target | Coverage Evidence | Status | Evidence |
|---|---|---|---|---|---|---|---|---|---|---|---|
| TASK-001 | - | Describe the task | Define the observable completed state | Unit test / golden test / benchmark / review | N/A or Yes; state rationale | N/A with rationale or failing test output | Review/build/run evidence required | State task-specific target or N/A with rationale | - | Todo | - |

---

## 4. Completion Definition Rules

Completion definitions MUST be:

- specific
- measurable
- verifiable
- implementation-oriented
- testable
- tied to observable behavior or produced artifacts

Avoid vague definitions.

Bad:

```text
Implement ZDD.
```

Good:

```text
Implement canonical ZDD Union operation using MakeNode, unique table, and operation cache.
```

Bad:

```text
Add tests.
```

Good:

```text
Add randomized comparison tests against a naive HashSet-based set-family implementation for Union, Intersect, and Difference.
```

---

## 5. Evidence Rules

Every completed task MUST include evidence.

Allowed evidence includes:

- unit test names
- randomized/property test results
- golden test output
- benchmark results
- generated files
- output snippets
- CI logs
- coverage reports
- DOT output examples
- Markdown/CSV/AsciiDoc export examples
- code locations when paired with verification results

A task MUST NOT be marked `Done` if evidence is missing.

Bad evidence:

```text
Looks correct.
```

Good evidence:

```text
ZddUnionTests.UnionMatchesNaiveSetFamily passed.
```

Good evidence:

```text
Randomized naive comparison passed for 10,000 generated set families.
```

---

## 6. Nested Tasks

Tasks MAY be hierarchical.

Example:

| ID | Parent | Task | Completion Definition | Verification Method | Test First? | Failing Test Evidence | Passing Test Evidence | Coverage Target | Coverage Evidence | Status | Evidence |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ZDD-100 | - | Implement ZDD foundation | ZDD foundation operations are implemented and verified | MSTest + naive comparison | Yes | Required before implementation | Required before Done | Line >= 90%; branch >= 85% for changed production code | - | Todo | - |
| ZDD-110 | ZDD-100 | Implement node table | Nodes are stored by stable integer IDs | Unit tests | Yes | Required before implementation | Required before Done | Line >= 90%; branch >= 85% for changed production code | - | Todo | - |
| ZDD-120 | ZDD-100 | Implement unique table | Equivalent nodes share the same ID | MSTest unit tests + validation | Yes | Required before implementation | Required before Done | Line >= 90%; branch >= 85% for changed production code | - | Todo | - |
| ZDD-130 | ZDD-100 | Implement operation cache | Recursive operations reuse cached results | MSTest unit tests + counters | Yes | Required before implementation | Required before Done | Line >= 90%; branch >= 85% for changed production code | - | Todo | - |

Nested tasks inherit context from their parents but MUST still define their own completion criteria and verification method.

---

## 7. Status Rules

Allowed statuses:

| Status | Meaning |
|---|---|
| Todo | Not started |
| InProgress | Actively being worked on |
| Blocked | Waiting for dependency, decision, or external input |
| Done | Verified complete with evidence |

Rules:

- `Done` requires evidence.
- `Blocked` requires the blocking reason in the Evidence column.
- `InProgress` should identify partial progress or active work.
- Planned work without implementation must remain `Todo`.

---

## 8. Persistent Backlog

Long-term work SHOULD be tracked in `docs/backlog.md`.

Recommended categories:

- Core
- BDD
- ZDD
- Diagnostics
- Export
- CodeAnalysis
- Performance
- Documentation
- Samples
- Tooling
- Future Research

Backlog items should be stable and should not be deleted merely because they are deferred. Deferred work should remain as `Todo` or `Blocked` with rationale.

---

## 9. AI Agent Requirements

AI coding agents MUST:

- create or update a task table before implementation for non-trivial work
- define completion criteria explicitly
- define verification methods explicitly
- update task status during work
- provide evidence for completed tasks
- avoid claiming completion without verification
- separate implemented behavior from planned behavior
- distinguish assumptions from verified facts
- record unresolved risks or follow-up items

AI coding agents MUST NOT:

- mark tasks as `Done` without evidence
- claim tests passed without running or citing them
- hide skipped verification
- conflate code generation with verified completion

---

## 10. Pull Request Requirements

Every pull request SHOULD include:

- task table
- completion criteria
- verification evidence
- test summary
- known limitations
- follow-up items if needed

Recommended PR summary format:

| ID | Task | Completion Definition | Verification Method | Test First? | Failing Test Evidence | Passing Test Evidence | Coverage Target | Coverage Evidence | Status | Evidence |
|---|---|---|---|---|---|---|---|---|---|---|
| PR-001 | Implement feature | Observable completion condition | Test/review/build | N/A or Yes; state rationale | N/A with rationale or failing test output | Review/build/run evidence required | State task-specific target or N/A with rationale | - | Done | Evidence |

---

## 11. DecisionDiagramSharp-Specific Verification Guidance

BDD operations SHOULD be verified against truth-table semantics for small variable counts.

ZDD operations SHOULD be verified against a naive set-family model such as:

```text
HashSet<SortedSet<int>>
```

Diagnostics and Export SHOULD use golden tests for stable text outputs.

Performance-sensitive changes SHOULD include BenchmarkDotNet measurements or an explicit note explaining why benchmarking is deferred.

---

## 12. Philosophy

The project prioritizes:

```text
verified completion
```

over:

```text
claimed completion
```

and:

```text
evidence
```

over:

```text
confidence
```
