using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DecisionDiagramSharp.Core.Tests;

[TestClass]
public sealed class ZddManagerTests
{
    // ─── Terminal Semantics ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the VariableTable deterministically maps the same name to the same VariableId.
    /// </summary>
    /// <remarks>
    ///Confirms that variable registration is idempotent and that different names produce different IDs.
    /// </remarks>
    [TestMethod]
    public void VariableTable_ShouldMapNamesToStableIds()
    {
        // Arrange
        var table = new VariableTable();

        // Act
        var a1 = table.GetOrAdd("A");
        var a2 = table.GetOrAdd("A");
        var b = table.GetOrAdd("B");

        // Assert
        Assert.AreEqual(a1, a2);
        Assert.AreNotEqual(a1, b);
        Assert.AreEqual("A", table.GetName(a1));
        Assert.AreEqual(2, table.Count);
    }

    /// <summary>
    /// Verifies that Empty has 0 sets and Base has 1 set.
    /// </summary>
    /// <remarks>
    ///Confirms the foundational terminal semantics that all ZDD operations build upon.
    /// </remarks>
    [TestMethod]
    public void TerminalSemantics_EmptyHasZeroSets_BaseHasOneSet()
    {
        // Arrange
        var manager = new ZddManager();

        // Act / Assert
        Assert.AreEqual(0L, manager.CountSets(manager.Empty));
        Assert.AreEqual(1L, manager.CountSets(manager.Base));
        Assert.IsTrue(manager.Empty.IsEmpty);
        Assert.IsFalse(manager.Empty.IsBase);
        Assert.IsTrue(manager.Base.IsBase);
        Assert.IsFalse(manager.Base.IsEmpty);
    }

    // ─── MakeSet / MakeFamily ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that MakeSet produces a ZDD containing exactly the specified set.
    /// </summary>
    /// <remarks>
    ///Confirms round-trip fidelity: the enumerated set matches the input set regardless of input order.
    /// </remarks>
    [TestMethod]
    public void MakeSet_ShouldRoundTripThroughEnumeration()
    {
        // Arrange
        var manager = CreateManager("A", "B", "C");

        // Act
        var set = manager.MakeSet(new[] { Var(manager, "C"), Var(manager, "A"), Var(manager, "A") });
        var rows = manager.EnumerateSets(set);

        // Assert
        Assert.HasCount(1, rows);
        CollectionAssert.AreEqual(
            new[] { "A", "C" },
            rows[0].Select(v => manager.GetVariableName(v)).ToArray());
    }

    /// <summary>
    /// Verifies that MakeFamily produces a ZDD containing exactly the specified sets.
    /// </summary>
    /// <remarks>
    ///Confirms that multiple sets are correctly encoded into one canonical ZDD representation.
    /// </remarks>
    [TestMethod]
    public void MakeFamily_ShouldProduceCanonicalRepresentationOfMultipleSets()
    {
        // Arrange
        var manager = CreateManager("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");

        // Act
        var family = manager.MakeFamily(new[] { new[] { a, b }, new[] { a } });
        var keys = EnumeratedKeys(manager, family);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "A,B", "A" }, keys);
        Assert.AreEqual(2L, manager.CountSets(family));
    }

    // ─── ContainsSet ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that ContainsSet returns true for a set in the family and false for a set not in it.
    /// </summary>
    /// <remarks>
    ///Confirms the membership query returns the correct Boolean result against specific known inputs.
    /// </remarks>
    [TestMethod]
    public void ContainsSet_ShouldReportMembershipCorrectly()
    {
        // Arrange
        var manager = CreateManager("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");
        var family = manager.MakeFamily(new[] { new[] { a, b }, new[] { a } });

        // Act / Assert
        Assert.IsTrue(manager.ContainsSet(family, new[] { a, b }));
        Assert.IsTrue(manager.ContainsSet(family, new[] { a }));
        Assert.IsFalse(manager.ContainsSet(family, new[] { b }));
        Assert.IsFalse(manager.ContainsSet(family, Array.Empty<VariableId>()));
    }

    // ─── Set Operations ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Union, Intersect, and Difference return correct explicit results for small cases.
    /// </summary>
    /// <remarks>
    ///Provides deterministic, human-readable evidence for set-operation correctness before randomized tests run.
    /// </remarks>
    [TestMethod]
    public void SetOperations_ShouldMatchExplicitSmallCases()
    {
        // Arrange
        var manager = CreateManager("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");
        var left = manager.MakeFamily(new[] { new[] { a, b }, new[] { a } });
        var right = manager.MakeFamily(new[] { new[] { a, b }, new[] { b } });

        // Act
        var union = EnumeratedKeys(manager, manager.Union(left, right));
        var intersect = EnumeratedKeys(manager, manager.Intersect(left, right));
        var difference = EnumeratedKeys(manager, manager.Difference(left, right));

        // Assert
        CollectionAssert.AreEquivalent(new[] { "A,B", "A", "B" }, union);
        CollectionAssert.AreEquivalent(new[] { "A,B" }, intersect);
        CollectionAssert.AreEquivalent(new[] { "A" }, difference);
    }

    // ─── Subset Operations ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Subset1 removes the pivot variable and Containing preserves it.
    /// </summary>
    /// <remarks>
    ///Confirms the behavioral distinction between Subset1 (projects out the variable) and
    /// Containing (filters but retains the variable), which is a common source of confusion.
    /// </remarks>
    [TestMethod]
    public void Subset1AndContaining_ShouldDifferInVariableRetention()
    {
        // Arrange
        var manager = CreateManager("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");
        var family = manager.MakeFamily(new[] { new[] { a, b }, new[] { a } });

        // Act
        var subset1 = EnumeratedKeys(manager, manager.Subset1(family, a));
        var containing = EnumeratedKeys(manager, manager.Containing(family, a));

        // Assert
        CollectionAssert.AreEquivalent(new[] { "", "B" }, subset1);
        CollectionAssert.AreEquivalent(new[] { "A", "A,B" }, containing);
    }

    /// <summary>
    /// Verifies that Subset0 and NotContaining select sets not containing the pivot variable.
    /// </summary>
    /// <remarks>
    ///Confirms both operations select the correct complement subset and that they agree for small known inputs.
    /// </remarks>
    [TestMethod]
    public void Subset0AndNotContaining_ShouldSelectSetsWithoutPivot()
    {
        // Arrange
        var manager = CreateManager("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");
        var family = manager.MakeFamily(new[] { new[] { a, b }, new[] { b } });

        // Act
        var subset0 = EnumeratedKeys(manager, manager.Subset0(family, a));
        var notContaining = EnumeratedKeys(manager, manager.NotContaining(family, a));

        // Assert
        CollectionAssert.AreEquivalent(new[] { "B" }, subset0);
        CollectionAssert.AreEquivalent(new[] { "B" }, notContaining);
    }

    /// <summary>
    /// Verifies that Change toggles the pivot variable in each set.
    /// </summary>
    /// <remarks>
    ///Confirms the symmetric difference-per-set semantics of Change for known inputs.
    /// </remarks>
    [TestMethod]
    public void Change_ShouldTogglePivotVariableInEachSet()
    {
        // Arrange
        var manager = CreateManager("A", "B");
        var a = Var(manager, "A");
        var b = Var(manager, "B");
        var family = manager.MakeFamily(new[] { new[] { b } });

        // Act — add A to the {B} set
        var toggled = EnumeratedKeys(manager, manager.Change(family, a));

        // Assert
        CollectionAssert.AreEquivalent(new[] { "A,B" }, toggled);
    }

    // ─── Limit Enforcement ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that EnumerateSets throws when the family has more sets than MaxSets.
    /// </summary>
    /// <remarks>
    ///Guards the safety limit that prevents unbounded enumeration from consuming excessive memory.
    /// </remarks>
    [TestMethod]
    public void EnumerateSets_ShouldThrowWhenFamilyExceedsMaxSets()
    {
        // Arrange
        var manager = CreateManager("A", "B", "C");
        var family = manager.MakeFamily(new[]
        {
            new[] { Var(manager, "A") },
            new[] { Var(manager, "B") },
            new[] { Var(manager, "C") }
        });

        // Act / Assert
        Assert.Throws<DiagramEnumerationLimitExceededException>(
            () => manager.EnumerateSets(family, new SetEnumerationOptions { MaxSets = 2 }));
    }

    /// <summary>
    /// Verifies that creating too many nodes throws DiagramSizeLimitExceededException.
    /// </summary>
    /// <remarks>
    ///Guards the node-count limit; operations must fail with an actionable exception before memory is exhausted.
    /// </remarks>
    [TestMethod]
    public void SizeLimit_ShouldThrowWhenNodeCountExceedsMaximum()
    {
        // Arrange
        var manager = new ZddManager(new DecisionDiagramOptions { MaxNodeCount = 1 });
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");
        var c = manager.GetOrAddVariable("C");

        // Act / Assert
        Assert.Throws<DiagramSizeLimitExceededException>(
            () => manager.MakeFamily(new[] { new[] { a }, new[] { b }, new[] { c } }));
    }

    // ─── Manager Mismatch ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Union throws DiagramManagerMismatchException when operands come from different managers.
    /// </summary>
    /// <remarks>
    ///Guards manager ownership isolation; mixing handles from different managers must produce an actionable error.
    /// </remarks>
    [TestMethod]
    public void ManagerMismatch_ShouldThrowActionableException()
    {
        // Arrange
        var leftManager = CreateManager("A");
        var rightManager = CreateManager("A");
        var left = leftManager.MakeSet(new[] { Var(leftManager, "A") });
        var right = rightManager.MakeSet(new[] { Var(rightManager, "A") });

        // Act / Assert
        var ex = Assert.Throws<DiagramManagerMismatchException>(() => leftManager.Union(left, right));
        StringAssert.Contains(ex.Message, "different ZddManager");
    }

    // ─── Null and Invalid Argument Validation ─────────────────────────────────

    /// <summary>
    /// Verifies that MakeSet, MakeFamily, and EnumerateSets throw on null or invalid inputs.
    /// </summary>
    /// <remarks>
    ///Guards the API contract that null arguments and invalid options are detected at the boundary.
    /// </remarks>
    [TestMethod]
    public void ArgumentValidation_ShouldThrowOnNullOrInvalidInputs()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var setA = manager.MakeSet(new[] { a });

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => manager.MakeSet((IEnumerable<VariableId>)null!));
        Assert.Throws<ArgumentNullException>(() => manager.MakeFamily((IEnumerable<IEnumerable<VariableId>>)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.MakeSet(new[] { new VariableId(999) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.EnumerateSets(setA, new SetEnumerationOptions { MaxSets = 0 }));
    }

    // ─── Validation and Corrupted Internal State ──────────────────────────────

    /// <summary>
    /// Verifies that Validate detects corrupted internal ZDD node state.
    /// </summary>
    /// <remarks>
    ///Covers internal invariant checks (High==Empty violation, out-of-range references, variable ordering, unique table)
    /// that protect canonicalization correctness. Intentionally corrupts internal state via reflection.
    /// </remarks>
    [TestMethod]
    public void InternalValidation_ShouldDetectCorruptedZddNodeState()
    {
        // Arrange / Act / Assert — High == Empty violated (both children are terminal 0)
        var managerHighEmpty = CreateManager("A", "B");
        var a1 = Var(managerHighEmpty, "A");
        var b1 = Var(managerHighEmpty, "B");
        _ = managerHighEmpty.MakeSet(new[] { a1, b1 });
        SetNode(managerHighEmpty, 0, CreateNode(a1.Value, 0, 0));
        Assert.Throws<DiagramException>(() => managerHighEmpty.Validate());

        // Arrange / Act / Assert — out-of-range child
        var managerOutOfRange = CreateManager("A", "B");
        var a2 = Var(managerOutOfRange, "A");
        var b2 = Var(managerOutOfRange, "B");
        _ = managerOutOfRange.MakeSet(new[] { a2, b2 });
        SetNode(managerOutOfRange, 0, CreateNode(a2.Value, -1, 1));
        Assert.Throws<DiagramException>(() => managerOutOfRange.Validate());

        // Arrange / Act / Assert — variable ordering violation
        var managerOrdering = CreateManager("A", "B");
        var a3 = Var(managerOrdering, "A");
        var b3 = Var(managerOrdering, "B");
        _ = managerOrdering.MakeSet(new[] { a3, b3 });
        SetNode(managerOrdering, 1, CreateNode(b3.Value, 0, 2));
        Assert.Throws<InvalidVariableOrderingException>(() => managerOrdering.Validate());

        // Arrange / Act / Assert — unique table cleared
        var managerUnique = CreateManager("A", "B");
        var a4 = Var(managerUnique, "A");
        var b4 = Var(managerUnique, "B");
        _ = managerUnique.MakeSet(new[] { a4, b4 });
        var uniqueTable = (System.Collections.IDictionary)TestHelpers.GetPrivateField(managerUnique, "_uniqueTable");
        uniqueTable.Clear();
        Assert.Throws<DiagramException>(() => managerUnique.Validate());
    }

    /// <summary>
    /// Verifies that private key types (ZddKey, BinaryOpKey) implement value equality correctly.
    /// </summary>
    /// <remarks>
    ///Covers internal unique-table and cache key semantics; incorrect equality would break canonicalization and caching.
    /// </remarks>
    [TestMethod]
    public void InternalKeyTypes_ShouldImplementValueEquality()
    {
        // Arrange / Act / Assert — ZddKey
        var zddKeyType = typeof(ZddManager).GetNestedType("ZddKey", System.Reflection.BindingFlags.NonPublic)!;
        var zddKey1 = Activator.CreateInstance(zddKeyType, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var zddKey2 = Activator.CreateInstance(zddKeyType, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new object[] { 1, 0, 1 }, null)!;
        var zddKey3 = Activator.CreateInstance(zddKeyType, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new object[] { 2, 0, 1 }, null)!;
        var zddKeyEquals = zddKeyType.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)zddKeyEquals.Invoke(zddKey1, new[] { zddKey2 })!);
        Assert.IsFalse((bool)zddKeyEquals.Invoke(zddKey1, new object[] { zddKey3 })!);
        Assert.IsFalse((bool)zddKeyEquals.Invoke(zddKey1, new object[] { "not-a-key" })!);
        Assert.AreNotEqual(zddKey1.GetHashCode(), zddKey3.GetHashCode());

        // Arrange / Act / Assert — BinaryOpKey
        var binaryOpKeyType = typeof(ZddManager).GetNestedType("BinaryOpKey", System.Reflection.BindingFlags.NonPublic)!;
        var createMethod = binaryOpKeyType.GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        var binaryKey1 = createMethod.Invoke(null, new object[] { 2, 1 })!;
        var binaryKey2 = createMethod.Invoke(null, new object[] { 1, 2 })!;
        var binaryKey3 = createMethod.Invoke(null, new object[] { 2, 3 })!;
        var binaryKeyEquals = binaryOpKeyType.GetMethod("Equals", new[] { typeof(object) })!;
        Assert.IsTrue((bool)binaryKeyEquals.Invoke(binaryKey1, new[] { binaryKey2 })!);
        Assert.IsFalse((bool)binaryKeyEquals.Invoke(binaryKey1, new object[] { binaryKey3 })!);
        Assert.AreNotEqual(binaryKey1.GetHashCode(), binaryKey3.GetHashCode());
    }

    /// <summary>
    /// Verifies that EnumerateSetsRecursive throws when the seed result already exceeds the limit.
    /// </summary>
    /// <remarks>
    ///Covers the edge case where the limit is exceeded before any new sets are added during recursion.
    /// </remarks>
    [TestMethod]
    public void InternalEnumerationHelper_ShouldThrowWhenSeedResultAlreadyExceedsLimit()
    {
        // Arrange
        var manager = new ZddManager();
        var enumerateSetsRecursive = typeof(ZddManager).GetMethod(
            "EnumerateSetsRecursive",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Act / Assert
        var target = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            enumerateSetsRecursive.Invoke(
                manager,
                new object[]
                {
                    1,
                    new List<VariableId>(),
                    new List<IReadOnlyList<VariableId>> { Array.Empty<VariableId>() },
                    0
                }));
        Assert.IsInstanceOfType(target.InnerException, typeof(DiagramEnumerationLimitExceededException));
    }

    // ─── Randomized Naive Model Comparison ───────────────────────────────────

    /// <summary>
    /// Verifies that Union, Intersect, Difference, Subset0/1, Containing, NotContaining, Change, and CountSets
    /// all match a naive HashSet-based model over 150 randomized inputs.
    /// </summary>
    /// <remarks>
    ///Provides broad behavioral coverage for all ZDD set operations; catches systematic errors
    /// in the recursive algorithms that explicit small-case tests cannot enumerate.
    /// </remarks>
    [TestMethod]
    public void RandomizedOperations_ShouldMatchNaiveModel()
    {
        // Arrange
        var random = new Random(12345);

        for (var iteration = 0; iteration < 150; iteration++)
        {
            var manager = CreateManager("A", "B", "C", "D", "E", "F");
            var variableIds = new[]
            {
                Var(manager, "A"), Var(manager, "B"), Var(manager, "C"),
                Var(manager, "D"), Var(manager, "E"), Var(manager, "F")
            };

            var leftNaive = ZddNaiveModel.CreateRandom(random, variableIds, 20);
            var rightNaive = ZddNaiveModel.CreateRandom(random, variableIds, 20);
            var pivot = variableIds[random.Next(variableIds.Length)];

            var left = manager.MakeFamily(ZddNaiveModel.ToSets(leftNaive, variableIds));
            var right = manager.MakeFamily(ZddNaiveModel.ToSets(rightNaive, variableIds));

            // Act / Assert — set operations
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.Union(leftNaive, rightNaive),
                ZddNaiveModel.Enumerate(manager, manager.Union(left, right)));
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.Intersect(leftNaive, rightNaive),
                ZddNaiveModel.Enumerate(manager, manager.Intersect(left, right)));
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.Difference(leftNaive, rightNaive),
                ZddNaiveModel.Enumerate(manager, manager.Difference(left, right)));

            // Act / Assert — subset operations
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.Subset0(leftNaive, pivot.Value),
                ZddNaiveModel.Enumerate(manager, manager.Subset0(left, pivot)));
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.Subset1(leftNaive, pivot.Value),
                ZddNaiveModel.Enumerate(manager, manager.Subset1(left, pivot)));
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.Containing(leftNaive, pivot.Value),
                ZddNaiveModel.Enumerate(manager, manager.Containing(left, pivot)));
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.NotContaining(leftNaive, pivot.Value),
                ZddNaiveModel.Enumerate(manager, manager.NotContaining(left, pivot)));
            ZddNaiveModel.AssertEqual(
                ZddNaiveModel.Change(leftNaive, pivot.Value),
                ZddNaiveModel.Enumerate(manager, manager.Change(left, pivot)));

            // Act / Assert — count
            Assert.AreEqual(leftNaive.Count, manager.CountSets(left),
                $"iteration={iteration}: CountSets must match naive count.");
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ZddManager CreateManager(params string[] names)
    {
        var manager = new ZddManager();
        for (var i = 0; i < names.Length; i++)
        {
            manager.GetOrAddVariable(names[i]);
        }

        return manager;
    }

    private static VariableId Var(ZddManager manager, string name)
    {
        return manager.GetOrAddVariable(name);
    }

    private static string[] EnumeratedKeys(ZddManager manager, Zdd value)
    {
        var sets = manager.EnumerateSets(value);
        var keys = new string[sets.Count];
        for (var i = 0; i < sets.Count; i++)
        {
            keys[i] = string.Join(",", sets[i].Select(v => manager.GetVariableName(v)));
        }

        return keys;
    }

    private static void SetNode(ZddManager manager, int index, object node)
    {
        var nodes = (System.Collections.IList)TestHelpers.GetPrivateField(manager, "_nodes");
        nodes[index] = node;
    }

    private static object CreateNode(int variable, int low, int high)
    {
        var nodeType = typeof(ZddManager).GetNestedType("ZddNode", System.Reflection.BindingFlags.NonPublic)!;
        return Activator.CreateInstance(
            nodeType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new object[] { variable, low, high },
            null)!;
    }

    // ─── String-name helpers (V07) ───────────────────────────────────────────

    /// <summary>
    /// Verifies that MakeSet(IEnumerable&lt;string&gt;) produces the same ZDD as the VariableId overload.
    /// </summary>
    /// <remarks>
    /// Confirms that the string-name shorthand resolves names via GetOrAddVariable and produces
    /// the canonical ZDD node identical to the VariableId overload.
    /// </remarks>
    [TestMethod]
    public void MakeSet_StringNames_ShouldReturnEquivalentToVariableIdOverload()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");

        // Act
        var byId = manager.MakeSet(new[] { a, b });
        var byName = manager.MakeSet(new[] { "A", "B" });

        // Assert — canonical representation: same set family → same node
        Assert.AreEqual(byId, byName, "MakeSet(string[]) and MakeSet(VariableId[]) must return the same canonical node.");
    }

    /// <summary>
    /// Verifies that MakeFamily(IEnumerable&lt;IEnumerable&lt;string&gt;&gt;) produces the same ZDD as the VariableId overload.
    /// </summary>
    /// <remarks>
    /// Confirms that the string-name shorthand resolves names for multiple sets and produces
    /// the canonical ZDD family node identical to the VariableId overload.
    /// </remarks>
    [TestMethod]
    public void MakeFamily_StringNames_ShouldReturnEquivalentToVariableIdOverload()
    {
        // Arrange
        var manager = new ZddManager();
        var a = manager.GetOrAddVariable("A");
        var b = manager.GetOrAddVariable("B");

        // Act
        var byId = manager.MakeFamily(new[] { new[] { a }, new[] { a, b } });
        var byName = manager.MakeFamily(new[] { new[] { "A" }, new[] { "A", "B" } });

        // Assert — canonical representation: same set family → same node
        Assert.AreEqual(byId, byName, "MakeFamily(string[][]) and MakeFamily(VariableId[][]) must return the same canonical node.");
    }
}
