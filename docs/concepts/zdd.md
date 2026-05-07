# ZDD Concepts

## What ZDD Represents

ZDD (Zero-suppressed Decision Diagram) represents a family of sets.

- one ZDD variable = one set element
- one path to `Base` = one concrete set
- all paths in the diagram = the whole set family

## Terminals

- `Empty` (`0`) means no sets: `{}`
- `Base` (`1`) means one set, the empty set: `{{}}`

## Reduction Rule

ZDD uses this canonical reduction:

```text
if High == Empty then return Low
```

This removes nodes where taking the variable never contributes a valid set.

## `Subset1` vs `Containing`

These operations are intentionally different.

- `Subset1(f, x)`:
  - selects sets containing `x`
  - removes `x` from each selected set
- `Containing(f, x)`:
  - selects sets containing `x`
  - keeps `x` in each selected set

Example:

```text
f = { {A}, {A,B}, {B} }

Subset1(f, A)  = { {}, {B} }
Containing(f,A)= { {A}, {A,B} }
```

## Enumeration Safety

Set-family enumeration can grow quickly.

`ZddManager.EnumerateSets` requires a `MaxSets` bound and throws
`DiagramEnumerationLimitExceededException` when the result would exceed that bound.

Use a small bound for previews and a larger bound only when you explicitly want deeper inspection.
