using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class BddManagerTests
{
    [TestMethod]
    public void Terminals_And_Variables_HaveExpectedSemantics()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var aNodeAgain = manager.Var("A");

        Assert.IsTrue(manager.False.IsFalse);
        Assert.IsTrue(manager.True.IsTrue);
        Assert.IsFalse(manager.False.IsTrue);
        Assert.IsFalse(manager.True.IsFalse);
        Assert.AreEqual(aNode, aNodeAgain);
        Assert.IsTrue(manager.IsSatisfiable(aNode));
        Assert.IsFalse(manager.IsSatisfiable(manager.False));
        Assert.AreEqual(2L, manager.CountModels(aNode));
        Assert.IsGreaterThan(0, manager.NonTerminalNodeCount);
        Assert.AreEqual("Bdd(2)", aNode.ToString());
        Assert.AreEqual("A", manager.GetVariableName(a));
        Assert.AreEqual("B", manager.GetVariableName(b));

        Assert.IsTrue(aNode.Equals((object)aNodeAgain));
        Assert.IsFalse(aNode.Equals("not-bdd"));
        Assert.AreEqual(aNode.GetHashCode(), aNodeAgain.GetHashCode());
        Assert.IsTrue(aNode == aNodeAgain);
        Assert.IsFalse(aNode != aNodeAgain);

        var defaultBdd = default(Bdd);
        Assert.IsTrue(defaultBdd.IsFalse);
        Assert.IsFalse(defaultBdd.IsTrue);
        Assert.AreEqual("Bdd(0)", defaultBdd.ToString());
        Assert.IsTrue(defaultBdd.Equals((object)defaultBdd));
        _ = defaultBdd.GetHashCode();
    }

    [TestMethod]
    public void BooleanOperations_MatchExpectedTruthTable()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);
        var expression = manager.Or(manager.And(aNode, manager.Not(bNode)), manager.Xor(aNode, bNode));

        for (var mask = 0; mask < 4; mask++)
        {
            var av = (mask & 1) != 0;
            var bv = (mask & 2) != 0;
            var assignment = new Dictionary<VariableId, bool>
            {
                { a, av },
                { b, bv }
            };
            var expected = (av && !bv) || (av ^ bv);
            Assert.AreEqual(expected, manager.Evaluate(expression, assignment));
        }
    }

    [TestMethod]
    public void Ite_Implies_Equivalent_Restrict_Quantifiers_Work()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var aNode = manager.Var(a);
        var bNode = manager.Var(b);
        var ite = manager.Ite(aNode, bNode, manager.Not(bNode));

        Assert.IsTrue(manager.Equivalent(manager.Implies(aNode, bNode), manager.Or(manager.Not(aNode), bNode)).IsTrue);
        Assert.AreEqual(bNode, manager.Restrict(ite, a, true));
        Assert.AreEqual(manager.Not(bNode), manager.Restrict(ite, a, false));
        Assert.AreEqual(manager.True, manager.Exists(ite, a));
        Assert.AreEqual(manager.False, manager.ForAll(manager.Xor(aNode, bNode), a));
    }

    [TestMethod]
    public void EnumerateModels_And_Limits_Work()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var c = manager.GetOrAddVariable("C");
        var expression = manager.And(manager.Var(a), manager.Not(manager.Var(b)));

        var models = manager.EnumerateModels(expression, new ModelEnumerationOptions { MaxModels = 10 });
        Assert.HasCount(2, models);
        for (var i = 0; i < models.Count; i++)
        {
            Assert.IsTrue(models[i][a]);
            Assert.IsFalse(models[i][b]);
            Assert.IsTrue(models[i].ContainsKey(c));
        }

        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => manager.EnumerateModels(expression, new ModelEnumerationOptions { MaxModels = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => manager.EnumerateModels(expression, new ModelEnumerationOptions { MaxModels = 0 }));
    }

    [TestMethod]
    public void ManagerMismatch_And_InvalidInputs_Throw()
    {
        var left = new BddManager();
        var right = new BddManager();
        var a = left.GetOrAddVariable("A");
        var b = right.GetOrAddVariable("B");
        var leftNode = left.Var(a);
        var rightNode = right.Var(b);

        Assert.Throws<DiagramManagerMismatchException>(() => left.And(leftNode, rightNode));
        Assert.Throws<ArgumentNullException>(() => left.Var(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => left.Var(new VariableId(99)));
        Assert.Throws<ArgumentException>(() => left.Evaluate(leftNode, new Dictionary<VariableId, bool>()));
        Assert.Throws<ArgumentNullException>(() => left.Evaluate(leftNode, null!));
    }

    [TestMethod]
    public void Traversal_Statistics_And_TerminalBranches_Work()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var expression = manager.And(manager.Var(a), manager.Var(b));

        manager.Validate();
        manager.Validate(expression);

        var views = manager.GetReachableNodeViews(expression);
        Assert.IsNotEmpty(views);
        var first = views[0];
        var copy = new BddNodeView(first.NodeId, first.Variable, first.LowNodeId, first.HighNodeId);
        Assert.AreEqual(first.NodeId, copy.NodeId);
        Assert.AreEqual(first.Variable, copy.Variable);
        Assert.AreEqual(first.LowNodeId, copy.LowNodeId);
        Assert.AreEqual(first.HighNodeId, copy.HighNodeId);

        var stats = manager.GetStatistics(expression);
        Assert.IsGreaterThan(0, stats.ReachableNodeCount);
        Assert.IsGreaterThanOrEqualTo(stats.ReachableNodeCount, stats.TotalNodeCount);
        Assert.AreEqual(2, stats.VariableCount);

        Assert.AreEqual(0L, manager.CountModels(manager.False));
        Assert.AreEqual(4L, manager.CountModels(manager.True));
        Assert.HasCount(4, manager.EnumerateModels(manager.True, new ModelEnumerationOptions { MaxModels = 4 }));
        Assert.HasCount(0, manager.EnumerateModels(manager.False));
        Assert.AreEqual(1, new TruthTableOptions { MaxVariables = 1, MaxRows = 2 }.MaxVariables);
    }

    [TestMethod]
    public void Restrict_SizeLimit_And_TerminalStatistics_EdgeBranches_Work()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var bNode = manager.Var(b);
        var expression = manager.And(manager.Var(a), bNode);

        Assert.AreEqual(manager.True, manager.Restrict(manager.True, a, true));
        Assert.AreEqual(bNode, manager.Restrict(bNode, a, true));
        Assert.AreEqual(manager.Var(a), manager.Restrict(expression, b, true));
        Assert.AreEqual(1, manager.GetStatistics(manager.False).ReachableTerminalCount);
        Assert.AreEqual(1, manager.GetStatistics(manager.True).ReachableTerminalCount);
        Assert.HasCount(2, manager.EnumerateModels(bNode, new ModelEnumerationOptions { MaxModels = 2 }));

        var limited = new BddManager(new DecisionDiagramOptions { MaxNodeCount = 1 });
        var la = limited.GetOrAddVariable("A");
        var lb = limited.GetOrAddVariable("B");
        Assert.Throws<DiagramSizeLimitExceededException>(() => limited.And(limited.Var(la), limited.Var(lb)));
    }

    [TestMethod]
    public void Validate_CatchesCorruptedInternalState()
    {
        var managerEqualChildren = CreateManagerWithTwoVariableConjunction(out var a1, out _);
        SetNode(managerEqualChildren, 0, CreateNode(a1.Value, 0, 0));
        Assert.Throws<DiagramException>(() => managerEqualChildren.Validate());

        var managerOutOfRange = CreateManagerWithTwoVariableConjunction(out var a2, out _);
        SetNode(managerOutOfRange, 0, CreateNode(a2.Value, -1, 1));
        Assert.Throws<DiagramException>(() => managerOutOfRange.Validate());

        var managerOrdering = CreateManagerWithTwoVariableConjunction(out var a3, out _);
        SetNode(managerOrdering, 0, CreateNode(a3.Value, 2, 1));
        Assert.Throws<InvalidVariableOrderingException>(() => managerOrdering.Validate());

        var managerUnique = CreateManagerWithTwoVariableConjunction(out _, out _);
        var uniqueTable = (IDictionary)GetPrivateField(managerUnique, "_uniqueTable");
        uniqueTable.Clear();
        Assert.Throws<DiagramException>(() => managerUnique.Validate());
    }

    [TestMethod]
    public void PrivateKeyTypes_ObjectEqualsAndHashCode_Work()
    {
        var bddKeyType = typeof(BddManager).GetNestedType("BddKey", BindingFlags.NonPublic)!;
        var bddKey1 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var bddKey2 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var bddKey3 = Activator.CreateInstance(bddKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 0, 1 }, null)!;
        AssertPrivateObjectEquals(bddKeyType, bddKey1, bddKey2, bddKey3);

        var iteKeyType = typeof(BddManager).GetNestedType("IteKey", BindingFlags.NonPublic)!;
        var iteKey1 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1, 0 }, null)!;
        var iteKey2 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1, 0 }, null)!;
        var iteKey3 = Activator.CreateInstance(iteKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 3, 1, 0 }, null)!;
        AssertPrivateObjectEquals(iteKeyType, iteKey1, iteKey2, iteKey3);

        var countKeyType = typeof(BddManager).GetNestedType("CountKey", BindingFlags.NonPublic)!;
        var countKey1 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1 }, null)!;
        var countKey2 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 1 }, null)!;
        var countKey3 = Activator.CreateInstance(countKeyType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { 2, 3 }, null)!;
        AssertPrivateObjectEquals(countKeyType, countKey1, countKey2, countKey3);
    }

    [TestMethod]
    public void PrivateRecursiveHelpers_CoverRareBranches()
    {
        var manager = new BddManager();
        var a = manager.GetOrAddVariable("A");
        var expression = manager.Var(a);

        var countModelsNode = typeof(BddManager).GetMethod("CountModelsNode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var countKeyType = typeof(BddManager).GetNestedType("CountKey", BindingFlags.NonPublic)!;
        var memoType = typeof(Dictionary<,>).MakeGenericType(countKeyType, typeof(long));
        var memo = Activator.CreateInstance(memoType)!;
        var expressionNodeId = GetBddNodeId(expression);
        Assert.AreEqual(1L, countModelsNode.Invoke(manager, new[] { (object)expressionNodeId, 0, memo }));
        Assert.AreEqual(1L, countModelsNode.Invoke(manager, new[] { (object)expressionNodeId, 0, memo }));

        var enumerateModelsRecursive = typeof(BddManager).GetMethod("EnumerateModelsRecursive", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var target = Assert.Throws<TargetInvocationException>(() =>
            enumerateModelsRecursive.Invoke(
                manager,
                new object[]
                {
                    1,
                    1,
                    new bool[manager.VariableCount],
                    new List<IReadOnlyDictionary<VariableId, bool>> { new Dictionary<VariableId, bool>() },
                    0
                }));
        Assert.IsInstanceOfType(target.InnerException, typeof(DiagramEnumerationLimitExceededException));

        var isReachable = typeof(BddManager).GetMethod("IsReachable", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.IsFalse((bool)isReachable.Invoke(manager, new object[] { expressionNodeId, 999 })!);
    }

    [TestMethod]
    public void RandomizedOperations_MatchNaiveTruthTables()
    {
        var random = new Random(20260507);
        for (var iteration = 0; iteration < 100; iteration++)
        {
            var manager = new BddManager();
            var variables = new[]
            {
                manager.GetOrAddVariable("A"),
                manager.GetOrAddVariable("B"),
                manager.GetOrAddVariable("C"),
                manager.GetOrAddVariable("D")
            };
            var leaves = new Bdd[variables.Length];
            for (var i = 0; i < variables.Length; i++)
            {
                leaves[i] = manager.Var(variables[i]);
            }

            var leftIndex = random.Next(leaves.Length);
            var rightIndex = random.Next(leaves.Length);
            var op = random.Next(7);
            var expression = BuildExpression(manager, leaves[leftIndex], leaves[rightIndex], op);

            for (var mask = 0; mask < 16; mask++)
            {
                var assignment = BuildAssignment(variables, mask);
                var lv = assignment[variables[leftIndex]];
                var rv = assignment[variables[rightIndex]];
                Assert.AreEqual(EvaluateNaive(lv, rv, op), manager.Evaluate(expression, assignment));
            }
        }
    }

    private static Bdd BuildExpression(BddManager manager, Bdd left, Bdd right, int op)
    {
        switch (op)
        {
            case 0:
                return manager.Not(left);
            case 1:
                return manager.And(left, right);
            case 2:
                return manager.Or(left, right);
            case 3:
                return manager.Xor(left, right);
            case 4:
                return manager.Ite(left, right, manager.False);
            case 5:
                return manager.Implies(left, right);
            default:
                return manager.Equivalent(left, right);
        }
    }

    private static bool EvaluateNaive(bool left, bool right, int op)
    {
        switch (op)
        {
            case 0:
                return !left;
            case 1:
                return left && right;
            case 2:
                return left || right;
            case 3:
                return left ^ right;
            case 4:
                return left ? right : false;
            case 5:
                return !left || right;
            default:
                return left == right;
        }
    }

    private static Dictionary<VariableId, bool> BuildAssignment(IReadOnlyList<VariableId> variables, int mask)
    {
        var assignment = new Dictionary<VariableId, bool>();
        for (var i = 0; i < variables.Count; i++)
        {
            assignment[variables[i]] = (mask & (1 << i)) != 0;
        }

        return assignment;
    }

    private static BddManager CreateManagerWithTwoVariableConjunction(out VariableId a, out VariableId b)
    {
        var manager = new BddManager();
        a = manager.GetOrAddVariable("A");
        b = manager.GetOrAddVariable("B");
        _ = manager.And(manager.Var(a), manager.Var(b));
        return manager;
    }

    private static void SetNode(BddManager manager, int index, object node)
    {
        var nodes = (IList)GetPrivateField(manager, "_nodes");
        nodes[index] = node;
    }

    private static object CreateNode(int variable, int low, int high)
    {
        var nodeType = typeof(BddManager).GetNestedType("BddNode", BindingFlags.NonPublic)!;
        return Activator.CreateInstance(
            nodeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { variable, low, high },
            null)!;
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return field.GetValue(target)!;
    }

    private static int GetBddNodeId(Bdd value)
    {
        var property = typeof(Bdd).GetProperty("NodeId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (int)property.GetValue(value)!;
    }

    private static void AssertPrivateObjectEquals(Type type, object first, object second, object third)
    {
        var equalsObject = type.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)equalsObject.Invoke(first, new[] { second })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { third })!);
        Assert.IsFalse((bool)equalsObject.Invoke(first, new object[] { "not-a-key" })!);
        Assert.AreNotEqual(first.GetHashCode(), third.GetHashCode());
    }
}
