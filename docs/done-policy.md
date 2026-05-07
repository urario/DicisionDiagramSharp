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

For Core BDD/ZDD operations, test-first development is REQUIRED. A Core behavior task MUST NOT be marked `Done` unless it has test evidence or a documented exception approved in the task table.

---

## 2.2 Coverage Policy

Completion definitions MUST include a concrete coverage target or an explicit `N/A` rationale.

Default minimum targets:

| Work Area | Minimum Coverage Target |
|---|---|
| Core BDD/ZDD behavior | Line >= 90%; branch >= 85% for changed production code |
| Core safety and validation logic | Line >= 90%; branch >= 85% for changed production code |
| Diagnostics / Export | Line >= 85%; branch >= 75% for changed production code, plus golden tests where applicable |
| CodeAnalysis | Line >= 85%; branch >= 75% for changed production code |
| CLI / Samples | Coverage may be N/A, but build and run evidence is required |
| Documentation-only tasks | Coverage is N/A with documentation review evidence |
| Benchmarks | Coverage is N/A, but benchmark build/run evidence is required |

Coverage evidence SHOULD include the command used and the measured result.

Example:

```text
coverage.cobertura.xml: line 92.4%, branch 87.1% for DecisionDiagramSharp.Core
```

A task MUST NOT be marked `Done` if it fails its stated coverage target unless the exception is documented with rationale and follow-up work.

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
