# ZMTBDD Baseline Design

## Purpose

The v0.5 ZMTBDD baseline provides a zero-suppressed sparse numeric representation for integer-valued Boolean-domain functions.

## Semantics

A ZMTBDD represents a sparse integer-valued function over Boolean variables:

```text
f : {0,1}^n -> int
```

The zero terminal represents the actual numeric value `0`.

## Reduction Rule

ZMTBDD uses zero suppression:

```text
if High == Zero, return Low
```

It does not apply the ordinary MTBDD `Low == High` reduction unless that case is already implied by zero suppression. This preserves the distinction between ordinary direct integer MTBDD reduction and sparse numeric zero-suppressed reduction.

## Sparse Evaluation

When a variable is skipped because its high branch was zero-suppressed, a true assignment for that skipped variable evaluates to zero. A false assignment continues through the retained low branch.

## Public Surface

The baseline exposes:

- `Zmtbdd` typed handle
- `ZmtbddManager`
- numeric `Zero` and `Constant`
- truth-table `Create`
- sparse `Evaluate`
- reachable node and terminal views
- statistics
- validation
- DOT, node-table, value-table, and statistics diagnostics

## Testing

Core tests compare ZMTBDD evaluation against naive sparse integer truth tables for small generated functions. Tests explicitly cover high-zero suppression, the absence of Low==High reduction for non-zero equal children, ownership validation, invalid inputs, validation failures, and size-limit failures.

## Benchmark Scope

Benchmarks cover ZMTBDD construction and evaluation on generated sparse integer truth tables, with a direct MTBDD baseline available in the same benchmark project.
