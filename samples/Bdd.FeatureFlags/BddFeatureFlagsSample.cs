using System;
using DecisionDiagramSharp;
using DecisionDiagramSharp.Diagnostics;
using DecisionDiagramSharp.Export;

var manager = new BddManager();
var newCheckout = manager.GetOrAddVariable("NewCheckout");
var betaUser = manager.GetOrAddVariable("BetaUser");
var killSwitch = manager.GetOrAddVariable("KillSwitch");

var canUseNewCheckout = manager.And(
    manager.Or(manager.Var(newCheckout), manager.Var(betaUser)),
    manager.Not(manager.Var(killSwitch)));

Console.WriteLine("Satisfying configurations: " + manager.CountModels(canUseNewCheckout));
Console.WriteLine();
Console.WriteLine(MarkdownTableExporter.Export(BddDiagnostics.BuildTruthTable(manager, canUseNewCheckout)));
Console.WriteLine();
Console.WriteLine(BddDiagnostics.ToDot(manager, canUseNewCheckout));
