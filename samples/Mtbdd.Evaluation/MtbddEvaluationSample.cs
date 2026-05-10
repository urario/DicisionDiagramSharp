using System;
using System.Collections.Generic;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

// This sample demonstrates MTBDD construction, evaluation, and export.
// An MTBDD encodes an integer-valued Boolean-domain function.
// Here we encode a simple scoring function over two feature flags.

var dd = new DecisionDiagramManager();
var enabled = dd.Mtbdd.GetOrAddVariable("Enabled");
var premium = dd.Mtbdd.GetOrAddVariable("Premium");

// Scoring table (LSB-first variable ordering):
//   Enabled=false, Premium=false -> 0
//   Enabled=true,  Premium=false -> 10
//   Enabled=false, Premium=true  -> 5
//   Enabled=true,  Premium=true  -> 25
var scores = dd.Mtbdd.Create(new[] { 0, 10, 5, 25 });

// Evaluate for a specific assignment
var result = dd.Mtbdd.Evaluate(
    scores,
    new Dictionary<VariableId, bool>
    {
        { enabled, true },
        { premium, true }
    });

Console.WriteLine("Score (Enabled=true, Premium=true): " + result);
Console.WriteLine();

// Export value table as Markdown
var markdown = scores.ToMarkdownValueTable();
Console.WriteLine("Value table (Markdown):");
Console.WriteLine(markdown);

// Export value table as CSV
var csv = scores.ToCsvValueTable();
Console.WriteLine("Value table (CSV):");
Console.WriteLine(csv);
