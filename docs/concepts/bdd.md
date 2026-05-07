# BDD Concepts

## What BDD Represents

BDD (Binary Decision Diagram) represents a Boolean function.

- one BDD variable = one Boolean input
- `False` terminal = function evaluates to false
- `True` terminal = function evaluates to true
- one root handle = one canonical Boolean function for a fixed variable order

## Terminals

- `False` (`0`) means the Boolean constant false.
- `True` (`1`) means the Boolean constant true.

## Reduction Rule

BDD uses this canonical reduction:

```text
if Low == High then return Low
```

This removes tests that do not affect the result.

## Core Operations

`BddManager` provides manager-level APIs:

- `Var`
- `Not`
- `And`
- `Or`
- `Xor`
- `Ite`
- `Implies`
- `Equivalent`
- `Restrict`
- `Exists`
- `ForAll`
- `Evaluate`
- `CountModels`
- `EnumerateModels`

All operands must belong to the same `BddManager`.

## Truth Tables and Enumeration Safety

Truth tables and model enumeration grow exponentially with variable count.

- `TruthTableOptions.MaxVariables` defaults to `16`.
- `TruthTableOptions.MaxRows` defaults to `65536`.
- `ModelEnumerationOptions.MaxModels` defaults to `1000`.

When a limit is exceeded, the library throws `DiagramEnumerationLimitExceededException` with an actionable message.
