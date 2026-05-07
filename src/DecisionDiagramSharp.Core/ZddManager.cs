using System;
using System.Collections.Generic;

namespace DecisionDiagramSharp;

/// <summary>
/// Manages ZDD creation, canonicalization, operations, and validation.
/// </summary>
public sealed class ZddManager
{
    internal const int EmptyNodeId = 0;
    internal const int BaseNodeId = 1;

    private readonly VariableTable _variableTable = new VariableTable();
    private readonly List<ZddNode> _nodes = new List<ZddNode>();
    private readonly Dictionary<ZddKey, int> _uniqueTable = new Dictionary<ZddKey, int>();
    private readonly Dictionary<BinaryOpKey, int> _unionCache = new Dictionary<BinaryOpKey, int>();
    private readonly Dictionary<BinaryOpKey, int> _intersectCache = new Dictionary<BinaryOpKey, int>();
    private readonly Dictionary<BinaryOpKey, int> _differenceCache = new Dictionary<BinaryOpKey, int>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ZddManager"/> class.
    /// </summary>
    /// <param name="options">Optional configuration values.</param>
    public ZddManager(DecisionDiagramOptions? options = null)
    {
        Options = options ?? new DecisionDiagramOptions();
    }

    /// <summary>
    /// Gets manager-level options.
    /// </summary>
    public DecisionDiagramOptions Options { get; }

    /// <summary>
    /// Gets the Empty terminal, representing the empty family <c>{}</c>.
    /// </summary>
    public Zdd Empty => new Zdd(this, EmptyNodeId);

    /// <summary>
    /// Gets the Base terminal, representing the family containing only the empty set <c>{{}}</c>.
    /// </summary>
    public Zdd Base => new Zdd(this, BaseNodeId);

    /// <summary>
    /// Gets the number of registered variables.
    /// </summary>
    public int VariableCount => _variableTable.Count;

    /// <summary>
    /// Gets the number of non-terminal nodes currently allocated.
    /// </summary>
    public int NonTerminalNodeCount => _nodes.Count;

    /// <summary>
    /// Resolves or creates a variable identifier for <paramref name="name"/>.
    /// </summary>
    public VariableId GetOrAddVariable(string name)
    {
        return _variableTable.GetOrAdd(name);
    }

    /// <summary>
    /// Gets the display name for a variable.
    /// </summary>
    public string GetVariableName(VariableId id)
    {
        return _variableTable.GetName(id);
    }

    /// <summary>
    /// Creates a ZDD family containing exactly one set.
    /// </summary>
    public Zdd MakeSet(IEnumerable<VariableId> set)
    {
        var ids = BuildSortedDistinctIds(set);
        var nodeId = BaseNodeId;
        for (var i = ids.Count - 1; i >= 0; i--)
        {
            nodeId = MakeNode(ids[i], EmptyNodeId, nodeId);
        }

        return Wrap(nodeId);
    }

    /// <summary>
    /// Creates a ZDD family from multiple sets.
    /// </summary>
    public Zdd MakeFamily(IEnumerable<IEnumerable<VariableId>> sets)
    {
        if (sets == null)
        {
            throw new ArgumentNullException(nameof(sets));
        }

        var family = Empty;
        foreach (var set in sets)
        {
            family = Union(family, MakeSet(set));
        }

        return family;
    }

    /// <summary>
    /// Returns the union of two ZDD families.
    /// </summary>
    public Zdd Union(Zdd left, Zdd right)
    {
        EnsureSameManager(left, right);
        return Wrap(UnionNode(left.NodeId, right.NodeId));
    }

    /// <summary>
    /// Returns the intersection of two ZDD families.
    /// </summary>
    public Zdd Intersect(Zdd left, Zdd right)
    {
        EnsureSameManager(left, right);
        return Wrap(IntersectNode(left.NodeId, right.NodeId));
    }

    /// <summary>
    /// Returns the set difference <paramref name="left"/> \ <paramref name="right"/>.
    /// </summary>
    public Zdd Difference(Zdd left, Zdd right)
    {
        EnsureSameManager(left, right);
        return Wrap(DifferenceNode(left.NodeId, right.NodeId));
    }

    /// <summary>
    /// Selects sets that do not contain <paramref name="variable"/>.
    /// </summary>
    public Zdd Subset0(Zdd family, VariableId variable)
    {
        EnsureOwned(family);
        return Wrap(Subset0Node(family.NodeId, variable.Value));
    }

    /// <summary>
    /// Selects sets that contain <paramref name="variable"/> and removes the variable from each selected set.
    /// </summary>
    public Zdd Subset1(Zdd family, VariableId variable)
    {
        EnsureOwned(family);
        return Wrap(Subset1Node(family.NodeId, variable.Value));
    }

    /// <summary>
    /// Selects sets that contain <paramref name="variable"/> while preserving the variable in each selected set.
    /// </summary>
    public Zdd Containing(Zdd family, VariableId variable)
    {
        EnsureOwned(family);
        return Wrap(ContainingNode(family.NodeId, variable.Value));
    }

    /// <summary>
    /// Selects sets that do not contain <paramref name="variable"/>.
    /// </summary>
    public Zdd NotContaining(Zdd family, VariableId variable)
    {
        EnsureOwned(family);
        return Wrap(NotContainingNode(family.NodeId, variable.Value));
    }

    /// <summary>
    /// Toggles membership of <paramref name="variable"/> across all sets in <paramref name="family"/>.
    /// </summary>
    public Zdd Change(Zdd family, VariableId variable)
    {
        EnsureOwned(family);
        return Wrap(ChangeNode(family.NodeId, variable.Value));
    }

    /// <summary>
    /// Returns whether the family contains exactly <paramref name="set"/>.
    /// </summary>
    public bool ContainsSet(Zdd family, IEnumerable<VariableId> set)
    {
        EnsureOwned(family);
        var singleton = MakeSet(set);
        var remainder = Difference(singleton, family);
        return remainder.IsEmpty;
    }

    /// <summary>
    /// Counts sets in <paramref name="family"/>.
    /// </summary>
    public long CountSets(Zdd family)
    {
        EnsureOwned(family);
        var memo = new Dictionary<int, long>();
        return CountNode(family.NodeId, memo);
    }

    /// <summary>
    /// Enumerates sets in a family with a configurable limit.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<VariableId>> EnumerateSets(Zdd family, SetEnumerationOptions? options = null)
    {
        EnsureOwned(family);
        var effective = options ?? new SetEnumerationOptions();
        if (effective.MaxSets <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "SetEnumerationOptions.MaxSets must be greater than zero.");
        }

        var result = new List<IReadOnlyList<VariableId>>();
        var path = new List<VariableId>();
        EnumerateSetsRecursive(family.NodeId, path, result, effective.MaxSets);
        return result;
    }

    /// <summary>
    /// Returns diagnostics node views reachable from <paramref name="root"/>.
    /// </summary>
    public IReadOnlyList<ZddNodeView> GetReachableNodeViews(Zdd root)
    {
        EnsureOwned(root);
        var reachable = new List<ZddNodeView>();
        var visited = new HashSet<int>();
        CollectReachable(root.NodeId, visited, reachable);
        reachable.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));
        return reachable;
    }

    /// <summary>
    /// Builds statistics for <paramref name="root"/>.
    /// </summary>
    public DiagramStatistics GetStatistics(Zdd root)
    {
        EnsureOwned(root);
        var views = GetReachableNodeViews(root);
        var terminalCount = 0;
        if (IsReachable(root.NodeId, EmptyNodeId))
        {
            terminalCount++;
        }

        if (IsReachable(root.NodeId, BaseNodeId))
        {
            terminalCount++;
        }

        return new DiagramStatistics
        {
            ReachableNodeCount = views.Count,
            ReachableTerminalCount = terminalCount,
            TotalNodeCount = _nodes.Count,
            VariableCount = VariableCount
        };
    }

    /// <summary>
    /// Validates manager-level invariants for terminals, nodes, ordering, and unique-table consistency.
    /// </summary>
    public void Validate()
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            var nodeId = NodeIdFromIndex(i);
            var node = _nodes[i];
            EnsureValidNodeId(node.Low, "Low");
            EnsureValidNodeId(node.High, "High");

            if (node.High == EmptyNodeId)
            {
                throw new DiagramException("ZDD reduction rule violation: non-terminal node has High == Empty. Use MakeNode to canonicalize nodes.");
            }

            EnsureChildOrdering(nodeId, node.Variable, node.Low);
            EnsureChildOrdering(nodeId, node.Variable, node.High);

            var key = new ZddKey(node.Variable, node.Low, node.High);
            int uniqueNodeId;
            if (!_uniqueTable.TryGetValue(key, out uniqueNodeId) || uniqueNodeId != nodeId)
            {
                throw new DiagramException("Unique table consistency check failed. Rebuild nodes through canonical MakeNode only.");
            }
        }
    }

    /// <summary>
    /// Validates manager invariants and that the provided root is owned by this manager.
    /// </summary>
    public void Validate(Zdd root)
    {
        EnsureOwned(root);
        Validate();
        EnsureValidNodeId(root.NodeId, "Root");
    }

    private static bool IsTerminal(int nodeId)
    {
        return nodeId == EmptyNodeId || nodeId == BaseNodeId;
    }

    private Zdd Wrap(int nodeId)
    {
        return new Zdd(this, nodeId);
    }

    private void EnsureOwned(Zdd value)
    {
        if (!ReferenceEquals(value.Manager, this))
        {
            throw new DiagramManagerMismatchException(
                "The ZDD operand belongs to a different ZddManager instance. ZDD values can only be used with the manager that created them.");
        }
    }

    private void EnsureSameManager(Zdd left, Zdd right)
    {
        EnsureOwned(left);
        EnsureOwned(right);
    }

    private void EnsureValidNodeId(int nodeId, string role)
    {
        if (nodeId < EmptyNodeId || nodeId > MaxNodeId())
        {
            throw new DiagramException(role + " node id is out of range: " + nodeId + ".");
        }
    }

    private void EnsureChildOrdering(int parentNodeId, int parentVariable, int childNodeId)
    {
        if (IsTerminal(childNodeId))
        {
            return;
        }

        var child = GetNode(childNodeId);
        if (child.Variable <= parentVariable)
        {
            throw new InvalidVariableOrderingException(
                "Invalid variable ordering detected at node " + parentNodeId + ". Child variable must be greater than parent variable.");
        }
    }

    private int MakeNode(int variable, int low, int high)
    {
        if (high == EmptyNodeId)
        {
            return low;
        }

        var key = new ZddKey(variable, low, high);
        int existingNodeId;
        if (_uniqueTable.TryGetValue(key, out existingNodeId))
        {
            return existingNodeId;
        }

        if (_nodes.Count >= Options.MaxNodeCount)
        {
            throw new DiagramSizeLimitExceededException(
                "The ZDD manager exceeded MaxNodeCount (" + Options.MaxNodeCount + "). Increase DecisionDiagramOptions.MaxNodeCount or simplify the input family.");
        }

        _nodes.Add(new ZddNode(variable, low, high));
        var nodeId = NodeIdFromIndex(_nodes.Count - 1);
        _uniqueTable.Add(key, nodeId);
        return nodeId;
    }

    private int UnionNode(int left, int right)
    {
        if (left == right)
        {
            return left;
        }

        if (left == EmptyNodeId)
        {
            return right;
        }

        if (right == EmptyNodeId)
        {
            return left;
        }

        var key = BinaryOpKey.Create(left, right);
        int cached;
        if (_unionCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        var top = GetTopVariable(left, right);
        Decompose(left, top, out var leftLow, out var leftHigh);
        Decompose(right, top, out var rightLow, out var rightHigh);
        var low = UnionNode(leftLow, rightLow);
        var high = UnionNode(leftHigh, rightHigh);
        var result = MakeNode(top, low, high);
        _unionCache[key] = result;
        return result;
    }

    private int IntersectNode(int left, int right)
    {
        if (left == EmptyNodeId || right == EmptyNodeId)
        {
            return EmptyNodeId;
        }

        if (left == right)
        {
            return left;
        }

        var key = BinaryOpKey.Create(left, right);
        int cached;
        if (_intersectCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        var top = GetTopVariable(left, right);
        Decompose(left, top, out var leftLow, out var leftHigh);
        Decompose(right, top, out var rightLow, out var rightHigh);
        var low = IntersectNode(leftLow, rightLow);
        var high = IntersectNode(leftHigh, rightHigh);
        var result = MakeNode(top, low, high);
        _intersectCache[key] = result;
        return result;
    }

    private int DifferenceNode(int left, int right)
    {
        if (left == EmptyNodeId)
        {
            return EmptyNodeId;
        }

        if (right == EmptyNodeId)
        {
            return left;
        }

        if (left == right)
        {
            return EmptyNodeId;
        }

        var key = new BinaryOpKey(left, right);
        int cached;
        if (_differenceCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        var top = GetTopVariable(left, right);
        Decompose(left, top, out var leftLow, out var leftHigh);
        Decompose(right, top, out var rightLow, out var rightHigh);
        var low = DifferenceNode(leftLow, rightLow);
        var high = DifferenceNode(leftHigh, rightHigh);
        var result = MakeNode(top, low, high);
        _differenceCache[key] = result;
        return result;
    }

    private int Subset0Node(int nodeId, int variable)
    {
        if (IsTerminal(nodeId))
        {
            return nodeId;
        }

        var node = GetNode(nodeId);
        if (node.Variable > variable)
        {
            return nodeId;
        }

        if (node.Variable == variable)
        {
            return node.Low;
        }

        return MakeNode(node.Variable, Subset0Node(node.Low, variable), Subset0Node(node.High, variable));
    }

    private int Subset1Node(int nodeId, int variable)
    {
        if (IsTerminal(nodeId))
        {
            return EmptyNodeId;
        }

        var node = GetNode(nodeId);
        if (node.Variable > variable)
        {
            return EmptyNodeId;
        }

        if (node.Variable == variable)
        {
            return node.High;
        }

        return MakeNode(node.Variable, Subset1Node(node.Low, variable), Subset1Node(node.High, variable));
    }

    private int ContainingNode(int nodeId, int variable)
    {
        if (IsTerminal(nodeId))
        {
            return EmptyNodeId;
        }

        var node = GetNode(nodeId);
        if (node.Variable > variable)
        {
            return EmptyNodeId;
        }

        if (node.Variable == variable)
        {
            return MakeNode(node.Variable, EmptyNodeId, node.High);
        }

        return MakeNode(node.Variable, ContainingNode(node.Low, variable), ContainingNode(node.High, variable));
    }

    private int NotContainingNode(int nodeId, int variable)
    {
        if (IsTerminal(nodeId))
        {
            return nodeId;
        }

        var node = GetNode(nodeId);
        if (node.Variable > variable)
        {
            return nodeId;
        }

        if (node.Variable == variable)
        {
            return node.Low;
        }

        return MakeNode(node.Variable, NotContainingNode(node.Low, variable), NotContainingNode(node.High, variable));
    }

    private int ChangeNode(int nodeId, int variable)
    {
        if (nodeId == EmptyNodeId)
        {
            return EmptyNodeId;
        }

        if (nodeId == BaseNodeId)
        {
            return MakeNode(variable, EmptyNodeId, BaseNodeId);
        }

        var node = GetNode(nodeId);
        if (node.Variable > variable)
        {
            return MakeNode(variable, EmptyNodeId, nodeId);
        }

        if (node.Variable == variable)
        {
            return MakeNode(node.Variable, node.High, node.Low);
        }

        return MakeNode(node.Variable, ChangeNode(node.Low, variable), ChangeNode(node.High, variable));
    }

    private void Decompose(int nodeId, int variable, out int low, out int high)
    {
        if (IsTerminal(nodeId))
        {
            low = nodeId;
            high = EmptyNodeId;
            return;
        }

        var node = GetNode(nodeId);
        if (node.Variable == variable)
        {
            low = node.Low;
            high = node.High;
            return;
        }

        low = nodeId;
        high = EmptyNodeId;
    }

    private int GetTopVariable(int left, int right)
    {
        var leftVar = GetNodeVariableOrInfinity(left);
        var rightVar = GetNodeVariableOrInfinity(right);
        return leftVar < rightVar ? leftVar : rightVar;
    }

    private int GetNodeVariableOrInfinity(int nodeId)
    {
        return IsTerminal(nodeId) ? int.MaxValue : GetNode(nodeId).Variable;
    }

    private ZddNode GetNode(int nodeId)
    {
        return _nodes[IndexFromNodeId(nodeId)];
    }

    private long CountNode(int nodeId, Dictionary<int, long> memo)
    {
        long cached;
        if (memo.TryGetValue(nodeId, out cached))
        {
            return cached;
        }

        long value;
        if (nodeId == EmptyNodeId)
        {
            value = 0L;
        }
        else if (nodeId == BaseNodeId)
        {
            value = 1L;
        }
        else
        {
            var node = GetNode(nodeId);
            value = CountNode(node.Low, memo) + CountNode(node.High, memo);
        }

        memo[nodeId] = value;
        return value;
    }

    private void EnumerateSetsRecursive(
        int nodeId,
        List<VariableId> current,
        List<IReadOnlyList<VariableId>> result,
        int maxSets)
    {
        if (result.Count > maxSets)
        {
            throw new DiagramEnumerationLimitExceededException(
                "Set enumeration exceeded MaxSets (" + maxSets + "). Increase SetEnumerationOptions.MaxSets to inspect larger families.");
        }

        if (nodeId == EmptyNodeId)
        {
            return;
        }

        if (nodeId == BaseNodeId)
        {
            result.Add(new List<VariableId>(current));
            if (result.Count > maxSets)
            {
                throw new DiagramEnumerationLimitExceededException(
                    "Set enumeration exceeded MaxSets (" + maxSets + "). Increase SetEnumerationOptions.MaxSets to inspect larger families.");
            }

            return;
        }

        var node = GetNode(nodeId);
        EnumerateSetsRecursive(node.Low, current, result, maxSets);
        current.Add(new VariableId(node.Variable));
        EnumerateSetsRecursive(node.High, current, result, maxSets);
        current.RemoveAt(current.Count - 1);
    }

    private void CollectReachable(int nodeId, HashSet<int> visited, List<ZddNodeView> result)
    {
        if (IsTerminal(nodeId) || !visited.Add(nodeId))
        {
            return;
        }

        var node = GetNode(nodeId);
        result.Add(new ZddNodeView(nodeId, new VariableId(node.Variable), node.Low, node.High));
        CollectReachable(node.Low, visited, result);
        CollectReachable(node.High, visited, result);
    }

    private bool IsReachable(int rootNodeId, int targetNodeId)
    {
        if (rootNodeId == targetNodeId)
        {
            return true;
        }

        if (IsTerminal(rootNodeId))
        {
            return false;
        }

        var visited = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(rootNodeId);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == targetNodeId)
            {
                return true;
            }

            if (IsTerminal(current))
            {
                continue;
            }

            var node = GetNode(current);
            stack.Push(node.Low);
            stack.Push(node.High);
        }

        return false;
    }

    private List<int> BuildSortedDistinctIds(IEnumerable<VariableId> set)
    {
        if (set == null)
        {
            throw new ArgumentNullException(nameof(set));
        }

        var ids = new List<int>();
        foreach (var variable in set)
        {
            if (variable.Value < 0 || variable.Value >= VariableCount)
            {
                throw new ArgumentOutOfRangeException(nameof(set), "Set contains a VariableId not registered in this manager.");
            }

            ids.Add(variable.Value);
        }

        ids.Sort();

        var distinct = new List<int>(ids.Count);
        var hasPrevious = false;
        var previous = -1;
        for (var i = 0; i < ids.Count; i++)
        {
            var value = ids[i];
            if (!hasPrevious || value != previous)
            {
                distinct.Add(value);
                previous = value;
                hasPrevious = true;
            }
        }

        return distinct;
    }

    private static int NodeIdFromIndex(int index)
    {
        return index + 2;
    }

    private static int IndexFromNodeId(int nodeId)
    {
        return nodeId - 2;
    }

    private int MaxNodeId()
    {
        return NodeIdFromIndex(_nodes.Count - 1);
    }

    private readonly struct ZddNode
    {
        public ZddNode(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }
    }

    private readonly struct ZddKey : IEquatable<ZddKey>
    {
        public ZddKey(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }

        public bool Equals(ZddKey other)
        {
            return Variable == other.Variable && Low == other.Low && High == other.High;
        }

        public override bool Equals(object obj)
        {
            return obj is ZddKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Variable;
                hash = (hash * 397) ^ Low;
                hash = (hash * 397) ^ High;
                return hash;
            }
        }
    }

    private readonly struct BinaryOpKey : IEquatable<BinaryOpKey>
    {
        public BinaryOpKey(int left, int right)
        {
            Left = left;
            Right = right;
        }

        public int Left { get; }

        public int Right { get; }

        public static BinaryOpKey Create(int left, int right)
        {
            return left <= right ? new BinaryOpKey(left, right) : new BinaryOpKey(right, left);
        }

        public bool Equals(BinaryOpKey other)
        {
            return Left == other.Left && Right == other.Right;
        }

        public override bool Equals(object obj)
        {
            return obj is BinaryOpKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Left * 397) ^ Right;
            }
        }
    }
}
