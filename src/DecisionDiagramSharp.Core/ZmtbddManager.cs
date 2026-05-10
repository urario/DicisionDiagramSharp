using System;
using System.Collections.Generic;

namespace DecisionDiagramSharp;

/// <summary>
/// Manages zero-suppressed MTBDD creation, canonicalization, sparse evaluation, traversal, and validation.
/// </summary>
public sealed class ZmtbddManager
{
    internal const int ZeroNodeId = -1;

    private readonly VariableTable _variableTable = new VariableTable();
    private readonly List<ZmtbddNode> _nodes = new List<ZmtbddNode>();
    private readonly Dictionary<ZmtbddKey, int> _uniqueTable = new Dictionary<ZmtbddKey, int>();
    private readonly List<int> _terminalValues = new List<int>();
    private readonly Dictionary<int, int> _terminalByValue = new Dictionary<int, int>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ZmtbddManager"/> class.
    /// </summary>
    /// <param name="options">Optional configuration values.</param>
    public ZmtbddManager(DecisionDiagramOptions? options = null)
    {
        Options = options ?? new DecisionDiagramOptions();
        MakeTerminal(0);
    }

    /// <summary>
    /// Gets manager-level options.
    /// </summary>
    public DecisionDiagramOptions Options { get; }

    /// <summary>
    /// Gets the numeric zero terminal.
    /// </summary>
    public Zmtbdd Zero => new Zmtbdd(this, ZeroNodeId);

    /// <summary>
    /// Gets the number of registered variables.
    /// </summary>
    public int VariableCount => _variableTable.Count;

    /// <summary>
    /// Gets the number of non-terminal nodes currently allocated.
    /// </summary>
    public int NonTerminalNodeCount => _nodes.Count;

    /// <summary>
    /// Gets the number of distinct numeric terminals currently interned.
    /// </summary>
    public int TerminalCount => _terminalValues.Count;

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
    /// Returns the canonical terminal for a numeric value.
    /// </summary>
    public Zmtbdd Constant(int value)
    {
        return Wrap(MakeTerminal(value));
    }

    /// <summary>
    /// Builds a ZMTBDD from a complete truth table whose row index uses variable IDs as bit positions.
    /// </summary>
    public Zmtbdd Create(IReadOnlyList<int> values)
    {
        ValidateTruthTable(values);
        return Wrap(BuildNode(values, 0, 0));
    }

    /// <summary>
    /// Evaluates a ZMTBDD with a complete variable assignment and zero-suppressed skipped-variable semantics.
    /// </summary>
    public int Evaluate(Zmtbdd value, IReadOnlyDictionary<VariableId, bool> assignment)
    {
        EnsureOwned(value);
        EnsureCompleteAssignment(assignment);
        return EvaluateNode(value.NodeId, 0, assignment);
    }

    /// <summary>
    /// Gets the integer value for a terminal handle.
    /// </summary>
    public int GetTerminalValue(Zmtbdd value)
    {
        EnsureOwned(value);
        if (!IsTerminalId(value.NodeId))
        {
            throw new InvalidOperationException("The ZMTBDD value is not a terminal. Evaluate it with an assignment to obtain an integer result.");
        }

        return GetTerminalValueByNodeIdCore(value.NodeId);
    }

    /// <summary>
    /// Gets the integer terminal value for a diagnostics node identifier.
    /// </summary>
    public int GetTerminalValueByNodeId(int nodeId)
    {
        if (!IsTerminalId(nodeId))
        {
            throw new ArgumentException("The provided ZMTBDD node id is not a terminal id.", nameof(nodeId));
        }

        EnsureValidNodeId(nodeId, "Terminal");
        return GetTerminalValueByNodeIdCore(nodeId);
    }

    /// <summary>
    /// Returns diagnostics node views reachable from <paramref name="root"/>.
    /// </summary>
    public IReadOnlyList<ZmtbddNodeView> GetReachableNodeViews(Zmtbdd root)
    {
        EnsureOwned(root);
        var reachable = new List<ZmtbddNodeView>();
        var visited = new HashSet<int>();
        CollectReachable(root.NodeId, visited, reachable);
        reachable.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));
        return reachable;
    }

    /// <summary>
    /// Returns numeric terminal values reachable from <paramref name="root"/>.
    /// </summary>
    public IReadOnlyList<int> GetReachableTerminalValues(Zmtbdd root)
    {
        EnsureOwned(root);
        var terminalNodeIds = new HashSet<int>();
        CollectReachableTerminalIds(root.NodeId, new HashSet<int>(), terminalNodeIds);
        var values = new List<int>(terminalNodeIds.Count);
        foreach (var nodeId in terminalNodeIds)
        {
            values.Add(GetTerminalValueByNodeIdCore(nodeId));
        }

        values.Sort();
        return values;
    }

    /// <summary>
    /// Builds statistics for <paramref name="root"/>.
    /// </summary>
    public DiagramStatistics GetStatistics(Zmtbdd root)
    {
        EnsureOwned(root);
        return new DiagramStatistics
        {
            ReachableNodeCount = GetReachableNodeViews(root).Count,
            ReachableTerminalCount = GetReachableTerminalValues(root).Count,
            TotalNodeCount = _nodes.Count,
            VariableCount = VariableCount
        };
    }

    /// <summary>
    /// Validates manager-level invariants for terminals, nodes, ordering, zero suppression, and unique-table consistency.
    /// </summary>
    public void Validate()
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            var nodeId = NodeIdFromIndex(i);
            var node = _nodes[i];
            EnsureValidNodeId(node.Low, "Low");
            EnsureValidNodeId(node.High, "High");
            if (node.High == ZeroNodeId)
            {
                throw new DiagramException("ZMTBDD reduction rule violation: non-terminal node has High == Zero. Use MakeNode to canonicalize nodes.");
            }

            EnsureChildOrdering(nodeId, node.Variable, node.Low);
            EnsureChildOrdering(nodeId, node.Variable, node.High);
            var key = new ZmtbddKey(node.Variable, node.Low, node.High);
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
    public void Validate(Zmtbdd root)
    {
        EnsureOwned(root);
        Validate();
        EnsureValidNodeId(root.NodeId, "Root");
    }

    internal static bool IsTerminalId(int nodeId)
    {
        return nodeId < 0;
    }

    private Zmtbdd Wrap(int nodeId)
    {
        return new Zmtbdd(this, nodeId);
    }

    private int MakeTerminal(int value)
    {
        int nodeId;
        if (_terminalByValue.TryGetValue(value, out nodeId))
        {
            return nodeId;
        }

        _terminalValues.Add(value);
        nodeId = TerminalNodeIdFromIndex(_terminalValues.Count - 1);
        _terminalByValue.Add(value, nodeId);
        return nodeId;
    }

    private int MakeNode(int variable, int low, int high)
    {
        if (high == ZeroNodeId)
        {
            return low;
        }

        var key = new ZmtbddKey(variable, low, high);
        int existingNodeId;
        if (_uniqueTable.TryGetValue(key, out existingNodeId))
        {
            return existingNodeId;
        }

        if (_nodes.Count >= Options.MaxNodeCount)
        {
            throw new DiagramSizeLimitExceededException(
                "The ZMTBDD manager exceeded MaxNodeCount (" + Options.MaxNodeCount + "). Increase DecisionDiagramOptions.MaxNodeCount or simplify the sparse numeric function.");
        }

        _nodes.Add(new ZmtbddNode(variable, low, high));
        var nodeId = NodeIdFromIndex(_nodes.Count - 1);
        _uniqueTable.Add(key, nodeId);
        return nodeId;
    }

    private int BuildNode(IReadOnlyList<int> values, int variable, int mask)
    {
        if (variable == VariableCount)
        {
            return MakeTerminal(values[mask]);
        }

        var low = BuildNode(values, variable + 1, mask);
        var high = BuildNode(values, variable + 1, mask | (1 << variable));
        return MakeNode(variable, low, high);
    }

    private int EvaluateNode(int nodeId, int level, IReadOnlyDictionary<VariableId, bool> assignment)
    {
        if (IsTerminalId(nodeId))
        {
            for (var variable = level; variable < VariableCount; variable++)
            {
                if (assignment[new VariableId(variable)])
                {
                    return 0;
                }
            }

            return GetTerminalValueByNodeIdCore(nodeId);
        }

        var node = GetNode(nodeId);
        for (var skipped = level; skipped < node.Variable; skipped++)
        {
            if (assignment[new VariableId(skipped)])
            {
                return 0;
            }
        }

        return EvaluateNode(assignment[new VariableId(node.Variable)] ? node.High : node.Low, node.Variable + 1, assignment);
    }

    private void ValidateTruthTable(IReadOnlyList<int> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var expected = PowerOfTwo(VariableCount);
        if (values.Count != expected)
        {
            throw new ArgumentException("ZMTBDD truth table length must equal 2^VariableCount. Expected " + expected + " rows.", nameof(values));
        }
    }

    private void EnsureOwned(Zmtbdd value)
    {
        if (!ReferenceEquals(value.Manager, this))
        {
            throw new DiagramManagerMismatchException(
                "The ZMTBDD operand belongs to a different ZmtbddManager instance. ZMTBDD values can only be used with the manager that created them.");
        }
    }

    private void EnsureCompleteAssignment(IReadOnlyDictionary<VariableId, bool> assignment)
    {
        if (assignment == null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        for (var i = 0; i < VariableCount; i++)
        {
            var variable = new VariableId(i);
            if (!assignment.ContainsKey(variable))
            {
                throw new ArgumentException("Assignment does not contain a value for variable " + GetVariableName(variable) + ".", nameof(assignment));
            }
        }
    }

    private void EnsureValidNodeId(int nodeId, string role)
    {
        if (IsTerminalId(nodeId))
        {
            var index = TerminalIndexFromNodeId(nodeId);
            if (index < 0 || index >= _terminalValues.Count)
            {
                throw new DiagramException(role + " terminal id is out of range: " + nodeId + ".");
            }

            return;
        }

        if (nodeId < 1 || nodeId > MaxNodeId())
        {
            throw new DiagramException(role + " node id is out of range: " + nodeId + ".");
        }
    }

    private void EnsureChildOrdering(int parentNodeId, int parentVariable, int childNodeId)
    {
        if (IsTerminalId(childNodeId))
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

    private ZmtbddNode GetNode(int nodeId)
    {
        return _nodes[IndexFromNodeId(nodeId)];
    }

    private int GetTerminalValueByNodeIdCore(int nodeId)
    {
        return _terminalValues[TerminalIndexFromNodeId(nodeId)];
    }

    private void CollectReachable(int nodeId, HashSet<int> visited, List<ZmtbddNodeView> result)
    {
        if (IsTerminalId(nodeId) || !visited.Add(nodeId))
        {
            return;
        }

        var node = GetNode(nodeId);
        result.Add(new ZmtbddNodeView(nodeId, new VariableId(node.Variable), node.Low, node.High));
        CollectReachable(node.Low, visited, result);
        CollectReachable(node.High, visited, result);
    }

    private void CollectReachableTerminalIds(int nodeId, HashSet<int> visitedNodes, HashSet<int> terminals)
    {
        if (IsTerminalId(nodeId))
        {
            terminals.Add(nodeId);
            return;
        }

        if (!visitedNodes.Add(nodeId))
        {
            return;
        }

        var node = GetNode(nodeId);
        CollectReachableTerminalIds(node.Low, visitedNodes, terminals);
        CollectReachableTerminalIds(node.High, visitedNodes, terminals);
    }

    private static int PowerOfTwo(int exponent)
    {
        var value = 1;
        for (var i = 0; i < exponent; i++)
        {
            value *= 2;
        }

        return value;
    }

    private static int NodeIdFromIndex(int index)
    {
        return index + 1;
    }

    private static int IndexFromNodeId(int nodeId)
    {
        return nodeId - 1;
    }

    private static int TerminalNodeIdFromIndex(int index)
    {
        return -index - 1;
    }

    private static int TerminalIndexFromNodeId(int nodeId)
    {
        return -nodeId - 1;
    }

    private int MaxNodeId()
    {
        return NodeIdFromIndex(_nodes.Count - 1);
    }

    private readonly struct ZmtbddNode
    {
        public ZmtbddNode(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }
    }

    private readonly struct ZmtbddKey : IEquatable<ZmtbddKey>
    {
        public ZmtbddKey(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }

        public bool Equals(ZmtbddKey other)
        {
            return Variable == other.Variable && Low == other.Low && High == other.High;
        }

        public override bool Equals(object obj)
        {
            return obj is ZmtbddKey other && Equals(other);
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
}
