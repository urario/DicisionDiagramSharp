using System;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

// This sample demonstrates BDD-based feature flag logic.
// BDD operators (&, |, !) and handle-first extension methods are used
// instead of raw manager calls, showing the high-level API surface.

var dd = new DecisionDiagramManager();

var newCheckout = dd.Bdd.Var("NewCheckout");
var betaUser    = dd.Bdd.Var("BetaUser");
var killSwitch  = dd.Bdd.Var("KillSwitch");

// Feature is available when (NewCheckout or BetaUser) and not KillSwitch
var canUseNewCheckout = (newCheckout | betaUser) & !killSwitch;

Console.WriteLine("Satisfying configurations: " + dd.Bdd.CountModels(canUseNewCheckout));
Console.WriteLine();

// Export truth table using the extension method API
Console.WriteLine(canUseNewCheckout.ToMarkdownTruthTable());
Console.WriteLine();

// Export DOT graph
Console.WriteLine(canUseNewCheckout.ToDot());
