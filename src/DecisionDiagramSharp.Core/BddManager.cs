using System;
using System.Collections.Generic;

namespace DecisionDiagramSharp;

/// <summary>
/// Manages BDD creation, canonicalization, Boolean operations, traversal, and validation.
/// </summary>
public sealed class BddManager
{
    internal const int FalseNodeId = 0;
    internal const int TrueNodeId = 1;

    private readonly VariableTable _variableTable = new VariableTable();
    private readonly List<BddNode> _nodes = new List<BddNode>();
    private readonly Dictionary<BddKey, int> _uniqueTable = new Dictionary<BddKey, int>();
    private readonly Dictionary<IteKey, int> _iteCache = new Dictionary<IteKey, int>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BddManager"/> class.
    /// </summary>
    /// <param name="options">Optional configuration values.</param>
    public BddManager(DecisionDiagramOptions? options = null)
    {
        Options = options ?? new DecisionDiagramOptions();
    }

    /// <summary>
    /// Gets manager-level options.
    /// </summary>
    public DecisionDiagramOptions Options { get; }

    /// <summary>
    /// Gets the False terminal.
    /// </summary>
    public Bdd False => new Bdd(this, FalseNodeId);

    /// <summary>
    /// Gets the True terminal.
    /// </summary>
    public Bdd True => new Bdd(this, TrueNodeId);

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
    /// Creates or returns the canonical BDD node for a variable.
    /// </summary>
    public Bdd Var(VariableId variable)
    {
        EnsureRegisteredVariable(variable);
        return Wrap(MakeNode(variable.Value, FalseNodeId, TrueNodeId));
    }

    /// <summary>
    /// Resolves or creates a variable by name and returns its canonical BDD node.
    /// </summary>
    public Bdd Var(string name)
    {
        return Var(GetOrAddVariable(name));
    }

    /// <summary>
    /// Returns the logical negation of <paramref name="value"/>.
    /// </summary>
    public Bdd Not(Bdd value)
    {
        EnsureOwned(value);
        return Wrap(IteNode(value.NodeId, FalseNodeId, TrueNodeId));
    }

    /// <summary>
    /// Returns the conjunction of two BDD values.
    /// </summary>
    public Bdd And(Bdd left, Bdd right)
    {
        EnsureSameManager(left, right);
        return Wrap(IteNode(left.NodeId, right.NodeId, FalseNodeId));
    }

    /// <summary>
    /// Returns the disjunction of two BDD values.
    /// </summary>
    public Bdd Or(Bdd left, Bdd right)
    {
        EnsureSameManager(left, right);
        return Wrap(IteNode(left.NodeId, TrueNodeId, right.NodeId));
    }

    /// <summary>
    /// Returns the exclusive-or of two BDD values.
    /// </summary>
    public Bdd Xor(Bdd left, Bdd right)
    {
        EnsureSameManager(left, right);
        return Wrap(IteNode(left.NodeId, IteNode(right.NodeId, FalseNodeId, TrueNodeId), right.NodeId));
    }

    /// <summary>
    /// Returns if-then-else: <paramref name="ifValue"/> ? <paramref name="thenValue"/> : <paramref name="elseValue"/>.
    /// </summary>
    public Bdd Ite(Bdd ifValue, Bdd thenValue, Bdd elseValue)
    {
        EnsureSameManager(ifValue, thenValue);
        EnsureOwned(elseValue);
        return Wrap(IteNode(ifValue.NodeId, thenValue.NodeId, elseValue.NodeId));
    }

    /// <summary>
    /// Returns whether a BDD has at least one satisfying assignment.
    /// </summary>
    public bool IsSatisfiable(Bdd value)
    {
        EnsureOwned(value);
        return value.NodeId != FalseNodeId;
    }

    /// <summary>
    /// Returns logical implication <paramref name="left"/> -> <paramref name="right"/>.
    /// </summary>
    public Bdd Implies(Bdd left, Bdd right)
    {
        EnsureSameManager(left, right);
        return Or(Not(left), right);
    }

    /// <summary>
    /// Returns logical equivalence between two BDD values.
    /// </summary>
    public Bdd Equivalent(Bdd left, Bdd right)
    {
        EnsureSameManager(left, right);
        return Not(Xor(left, right));
    }

    /// <summary>
    /// Restricts one variable to a Boolean value.
    /// </summary>
    public Bdd Restrict(Bdd value, VariableId variable, bool assignment)
    {
        EnsureOwned(value);
        EnsureRegisteredVariable(variable);
        return Wrap(RestrictNode(value.NodeId, variable.Value, assignment));
    }

    /// <summary>
    /// Existentially quantifies one variable.
    /// </summary>
    public Bdd Exists(Bdd value, VariableId variable)
    {
        EnsureOwned(value);
        return Or(Restrict(value, variable, false), Restrict(value, variable, true));
    }

    /// <summary>
    /// Universally quantifies one variable.
    /// </summary>
    public Bdd ForAll(Bdd value, VariableId variable)
    {
        EnsureOwned(value);
        return And(Restrict(value, variable, false), Restrict(value, variable, true));
    }

    /// <summary>
    /// Evaluates a BDD with a complete variable assignment.
    /// </summary>
    public bool Evaluate(Bdd value, IReadOnlyDictionary<VariableId, bool> assignment)
    {
        EnsureOwned(value);
        if (assignment == null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        var nodeId = value.NodeId;
        while (!IsTerminal(nodeId))
        {
            var node = GetNode(nodeId);
            bool variableValue;
            var variable = new VariableId(node.Variable);
            if (!assignment.TryGetValue(variable, out variableValue))
            {
                throw new ArgumentException("Assignment does not contain a value for variable " + GetVariableName(variable) + ".", nameof(assignment));
            }

            nodeId = variableValue ? node.High : node.Low;
        }

        return nodeId == TrueNodeId;
    }

    /// <summary>
    /// Counts satisfying assignments across all variables registered in this manager.
    /// </summary>
    public long CountModels(Bdd value)
    {
        EnsureOwned(value);
        var memo = new Dictionary<CountKey, long>();
        return CountModelsNode(value.NodeId, 0, memo);
    }

    /// <summary>
    /// Enumerates satisfying assignments with a configurable maximum result count.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<VariableId, bool>> EnumerateModels(Bdd value, ModelEnumerationOptions? options = null)
    {
        EnsureOwned(value);
        var effective = options ?? new ModelEnumerationOptions();
        if (effective.MaxModels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "ModelEnumerationOptions.MaxModels must be greater than zero.");
        }

        var result = new List<IReadOnlyDictionary<VariableId, bool>>();
        var current = new bool[VariableCount];
        EnumerateModelsRecursive(value.NodeId, 0, current, result, effective.MaxModels);
        return result;
    }

    /// <summary>
    /// Returns diagnostics node views reachable from <paramref name="root"/>.
    /// </summary>
    public IReadOnlyList<BddNodeView> GetReachableNodeViews(Bdd root)
    {
        EnsureOwned(root);
        var reachable = new List<BddNodeView>();
        var visited = new HashSet<int>();
        CollectReachable(root.NodeId, visited, reachable);
        reachable.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));
        return reachable;
    }

    /// <summary>
    /// Builds statistics for <paramref name="root"/>.
    /// </summary>
    public DiagramStatistics GetStatistics(Bdd root)
    {
        EnsureOwned(root);
        var views = GetReachableNodeViews(root);
        var terminalCount = 0;
        if (IsReachable(root.NodeId, FalseNodeId))
        {
            terminalCount++;
        }

        if (IsReachable(root.NodeId, TrueNodeId))
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

            if (node.Low == node.High)
            {
                throw new DiagramException("BDD reduction rule violation: non-terminal node has Low == High. Use MakeNode to canonicalize nodes.");
            }

            EnsureChildOrdering(nodeId, node.Variable, node.Low);
            EnsureChildOrdering(nodeId, node.Variable, node.High);

            var key = new BddKey(node.Variable, node.Low, node.High);
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
    public void Validate(Bdd root)
    {
        EnsureOwned(root);
        Validate();
        EnsureValidNodeId(root.NodeId, "Root");
    }

    private static bool IsTerminal(int nodeId)
    {
        return nodeId == FalseNodeId || nodeId == TrueNodeId;
    }

    private Bdd Wrap(int nodeId)
    {
        return new Bdd(this, nodeId);
    }

    private void EnsureOwned(Bdd value)
    {
        if (!ReferenceEquals(value.Manager, this))
        {
            throw new DiagramManagerMismatchException(
                "The BDD operand belongs to a different BddManager instance. BDD values can only be used with the manager that created them.");
        }
    }

    private void EnsureSameManager(Bdd left, Bdd right)
    {
        EnsureOwned(left);
        EnsureOwned(right);
    }

    private void EnsureRegisteredVariable(VariableId variable)
    {
        if (variable.Value < 0 || variable.Value >= VariableCount)
        {
            throw new ArgumentOutOfRangeException(nameof(variable), "VariableId is not registered in this BddManager.");
        }
    }

    private void EnsureValidNodeId(int nodeId, string role)
    {
        if (nodeId < FalseNodeId || nodeId > MaxNodeId())
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
        if (low == high)
        {
            return low;
        }

        var key = new BddKey(variable, low, high);
        int existingNodeId;
        if (_uniqueTable.TryGetValue(key, out existingNodeId))
        {
            return existingNodeId;
        }

        if (_nodes.Count >= Options.MaxNodeCount)
        {
            throw new DiagramSizeLimitExceededException(
                "The BDD manager exceeded MaxNodeCount (" + Options.MaxNodeCount + "). Increase DecisionDiagramOptions.MaxNodeCount or simplify the Boolean function.");
        }

        _nodes.Add(new BddNode(variable, low, high));
        var nodeId = NodeIdFromIndex(_nodes.Count - 1);
        _uniqueTable.Add(key, nodeId);
        return nodeId;
    }

    private int IteNode(int ifNode, int thenNode, int elseNode)
    {
        if (ifNode == TrueNodeId)
        {
            return thenNode;
        }

        if (ifNode == FalseNodeId)
        {
            return elseNode;
        }

        if (thenNode == elseNode)
        {
            return thenNode;
        }

        var key = new IteKey(ifNode, thenNode, elseNode);
        int cached;
        if (_iteCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        var top = GetTopVariable(ifNode, thenNode, elseNode);
        Decompose(ifNode, top, out var ifLow, out var ifHigh);
        Decompose(thenNode, top, out var thenLow, out var thenHigh);
        Decompose(elseNode, top, out var elseLow, out var elseHigh);
        var low = IteNode(ifLow, thenLow, elseLow);
        var high = IteNode(ifHigh, thenHigh, elseHigh);
        var result = MakeNode(top, low, high);
        _iteCache[key] = result;
        return result;
    }

    private int RestrictNode(int nodeId, int variable, bool assignment)
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
            return assignment ? node.High : node.Low;
        }

        return MakeNode(node.Variable, RestrictNode(node.Low, variable, assignment), RestrictNode(node.High, variable, assignment));
    }

    private void Decompose(int nodeId, int variable, out int low, out int high)
    {
        if (IsTerminal(nodeId))
        {
            low = nodeId;
            high = nodeId;
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
        high = nodeId;
    }

    private int GetTopVariable(int first, int second, int third)
    {
        var firstVar = GetNodeVariableOrInfinity(first);
        var secondVar = GetNodeVariableOrInfinity(second);
        var thirdVar = GetNodeVariableOrInfinity(third);
        var top = firstVar < secondVar ? firstVar : secondVar;
        return top < thirdVar ? top : thirdVar;
    }

    private int GetNodeVariableOrInfinity(int nodeId)
    {
        return IsTerminal(nodeId) ? int.MaxValue : GetNode(nodeId).Variable;
    }

    private BddNode GetNode(int nodeId)
    {
        return _nodes[IndexFromNodeId(nodeId)];
    }

    private long CountModelsNode(int nodeId, int level, Dictionary<CountKey, long> memo)
    {
        var key = new CountKey(nodeId, level);
        long cached;
        if (memo.TryGetValue(key, out cached))
        {
            return cached;
        }

        long value;
        if (nodeId == FalseNodeId)
        {
            value = 0L;
        }
        else if (nodeId == TrueNodeId)
        {
            value = PowerOfTwo(VariableCount - level);
        }
        else
        {
            var node = GetNode(nodeId);
            var skipped = node.Variable - level;
            value = PowerOfTwo(skipped) *
                (CountModelsNode(node.Low, node.Variable + 1, memo) + CountModelsNode(node.High, node.Variable + 1, memo));
        }

        memo[key] = value;
        return value;
    }

    private void EnumerateModelsRecursive(
        int nodeId,
        int level,
        bool[] current,
        List<IReadOnlyDictionary<VariableId, bool>> result,
        int maxModels)
    {
        if (result.Count > maxModels)
        {
            throw new DiagramEnumerationLimitExceededException(
                "Model enumeration exceeded MaxModels (" + maxModels + "). Increase ModelEnumerationOptions.MaxModels to inspect larger functions.");
        }

        if (nodeId == FalseNodeId)
        {
            return;
        }

        if (level == VariableCount)
        {
            if (nodeId == TrueNodeId)
            {
                AddModel(current, result, maxModels);
            }

            return;
        }

        if (nodeId == TrueNodeId)
        {
            current[level] = false;
            EnumerateModelsRecursive(nodeId, level + 1, current, result, maxModels);
            current[level] = true;
            EnumerateModelsRecursive(nodeId, level + 1, current, result, maxModels);
            return;
        }

        var node = GetNode(nodeId);
        if (node.Variable > level)
        {
            current[level] = false;
            EnumerateModelsRecursive(nodeId, level + 1, current, result, maxModels);
            current[level] = true;
            EnumerateModelsRecursive(nodeId, level + 1, current, result, maxModels);
            return;
        }

        current[level] = false;
        EnumerateModelsRecursive(node.Low, level + 1, current, result, maxModels);
        current[level] = true;
        EnumerateModelsRecursive(node.High, level + 1, current, result, maxModels);
    }

    private void AddModel(bool[] current, List<IReadOnlyDictionary<VariableId, bool>> result, int maxModels)
    {
        var model = new Dictionary<VariableId, bool>();
        for (var i = 0; i < current.Length; i++)
        {
            model.Add(new VariableId(i), current[i]);
        }

        result.Add(model);
        if (result.Count > maxModels)
        {
            throw new DiagramEnumerationLimitExceededException(
                "Model enumeration exceeded MaxModels (" + maxModels + "). Increase ModelEnumerationOptions.MaxModels to inspect larger functions.");
        }
    }

    private void CollectReachable(int nodeId, HashSet<int> visited, List<BddNodeView> result)
    {
        if (IsTerminal(nodeId) || !visited.Add(nodeId))
        {
            return;
        }

        var node = GetNode(nodeId);
        result.Add(new BddNodeView(nodeId, new VariableId(node.Variable), node.Low, node.High));
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

    private static long PowerOfTwo(int exponent)
    {
        var value = 1L;
        for (var i = 0; i < exponent; i++)
        {
            value *= 2L;
        }

        return value;
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

    private readonly struct BddNode
    {
        public BddNode(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }
    }

    private readonly struct BddKey : IEquatable<BddKey>
    {
        public BddKey(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }

        public bool Equals(BddKey other)
        {
            return Variable == other.Variable && Low == other.Low && High == other.High;
        }

        public override bool Equals(object obj)
        {
            return obj is BddKey other && Equals(other);
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

    private readonly struct IteKey : IEquatable<IteKey>
    {
        public IteKey(int ifNode, int thenNode, int elseNode)
        {
            IfNode = ifNode;
            ThenNode = thenNode;
            ElseNode = elseNode;
        }

        public int IfNode { get; }

        public int ThenNode { get; }

        public int ElseNode { get; }

        public bool Equals(IteKey other)
        {
            return IfNode == other.IfNode && ThenNode == other.ThenNode && ElseNode == other.ElseNode;
        }

        public override bool Equals(object obj)
        {
            return obj is IteKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = IfNode;
                hash = (hash * 397) ^ ThenNode;
                hash = (hash * 397) ^ ElseNode;
                return hash;
            }
        }
    }

    private readonly struct CountKey : IEquatable<CountKey>
    {
        public CountKey(int nodeId, int level)
        {
            NodeId = nodeId;
            Level = level;
        }

        public int NodeId { get; }

        public int Level { get; }

        public bool Equals(CountKey other)
        {
            return NodeId == other.NodeId && Level == other.Level;
        }

        public override bool Equals(object obj)
        {
            return obj is CountKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (NodeId * 397) ^ Level;
            }
        }
    }
}
