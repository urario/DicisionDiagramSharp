# MTBDD Baseline Design

## Purpose

The v0.4 MTBDD baseline provides direct integer-valued Boolean-domain function support.

## Semantics

An MTBDD represents a total function:

```text
f : {0,1}^n -> int
```

Variables are Boolean-domain variables managed by `MtbddManager`. Terminals are signed integer values. The terminal value `0` is just the numeric value zero.

## Reduction Rule

MTBDD uses ordinary reduced ordered decision diagram semantics:

```text
if Low == High, return Low
```

All node creation must pass through the manager's canonical construction path and unique table.

## Public Surface

The baseline exposes:

- `Mtbdd` typed handle
- `MtbddManager`
- integer `Constant`
- truth-table `Create`
- `Evaluate`
- reachable node and terminal views
- statistics
- validation
- DOT, node-table, value-table, and statistics diagnostics

## Testing

Core tests compare MTBDD evaluation against naive integer truth tables for small generated functions. Tests include canonicalization, ownership validation, invalid inputs, validation failures, and size-limit failures.

## Benchmark Scope

Benchmarks cover MTBDD construction and evaluation on generated integer truth tables. Later refinement work compares direct MTBDD behavior against ZMTBDD behavior where sparse numeric functions are relevant.
