using System;
using System.Collections.Generic;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

// This sample demonstrates ZMTBDD construction, evaluation, and export.
// A ZMTBDD is zero-suppressed: branches leading only to zero are eliminated.
// This is useful for sparse functions where most inputs map to zero.

var dd = new DecisionDiagramManager();
var flagA = dd.Zmtbdd.GetOrAddVariable("FlagA");
var flagB = dd.Zmtbdd.GetOrAddVariable("FlagB");

// Sparse scoring table (LSB-first variable ordering):
//   FlagA=false, FlagB=false -> 0   (suppressed by zero-suppression)
//   FlagA=true,  FlagB=false -> 42  (non-zero: kept)
//   FlagA=false, FlagB=true  -> 0   (suppressed)
//   FlagA=true,  FlagB=true  -> 0   (suppressed)
var sparse = dd.Zmtbdd.Create(new[] { 0, 42, 0, 0 });

Console.WriteLine("Is zero terminal: " + sparse.IsZero);
Console.WriteLine();

// Evaluate assignments
var resultA = dd.Zmtbdd.Evaluate(
    sparse,
    new Dictionary<VariableId, bool>
    {
        { flagA, true },
        { flagB, false }
    });
Console.WriteLine("Score (FlagA=true, FlagB=false): " + resultA);

var resultB = dd.Zmtbdd.Evaluate(
    sparse,
    new Dictionary<VariableId, bool>
    {
        { flagA, false },
        { flagB, true }
    });
Console.WriteLine("Score (FlagA=false, FlagB=true): " + resultB);
Console.WriteLine();

// Export value table showing sparse structure
var markdown = sparse.ToMarkdownValueTable();
Console.WriteLine("Value table (Markdown):");
Console.WriteLine(markdown);

// Export node table to show suppressed zero branches
var nodeMarkdown = MarkdownTableExporter.Export(sparse.ToNodeTable());
Console.WriteLine("Node table (Markdown):");
Console.WriteLine(nodeMarkdown);
