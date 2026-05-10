using System;
using System.Collections.Generic;

namespace DecisionDiagramSharp;

/// <summary>
/// Manages MTBDD creation, canonicalization, evaluation, traversal, and validation for integer-valued Boolean-domain functions.
/// </summary>
public sealed class MtbddManager
{
    private readonly VariableTable _variableTable = new VariableTable();
    private readonly List<MtbddNode> _nodes = new List<MtbddNode>();
    private readonly Dictionary<MtbddKey, int> _uniqueTable = new Dictionary<MtbddKey, int>();
    private readonly List<int> _terminalValues = new List<int>();
    private readonly Dictionary<int, int> _terminalByValue = new Dictionary<int, int>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MtbddManager"/> class.
    /// </summary>
    /// <param name="options">Optional configuration values.</param>
    public MtbddManager(DecisionDiagramOptions? options = null)
    {
        Options = options ?? new DecisionDiagramOptions();
    }

    /// <summary>
    /// Gets manager-level options.
    /// </summary>
    public DecisionDiagramOptions Options { get; }

    /// <summary>
    /// Gets the number of registered variables.
    /// </summary>
    public int VariableCount => _variableTable.Count;

    /// <summary>
    /// Gets the number of non-terminal nodes currently allocated.
    /// </summary>
    public int NonTerminalNodeCount => _nodes.Count;

    /// <summary>
    /// Gets the number of distinct integer terminals currently interned.
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
    /// Returns the canonical terminal for an integer value.
    /// </summary>
    public Mtbdd Constant(int value)
    {
        return Wrap(MakeTerminal(value));
    }

    /// <summary>
    /// Builds an MTBDD from a complete truth table whose row index uses variable IDs as bit positions.
    /// </summary>
    public Mtbdd Create(IReadOnlyList<int> values)
    {
        ValidateTruthTable(values);
        return Wrap(BuildNode(values, 0, 0));
    }

    /// <summary>
    /// Evaluates an MTBDD with a complete variable assignment.
    /// </summary>
    public int Evaluate(Mtbdd value, IReadOnlyDictionary<VariableId, bool> assignment)
    {
        EnsureOwned(value);
        EnsureCompleteAssignment(assignment);
        var nodeId = value.NodeId;
        while (!IsTerminalId(nodeId))
        {
            var node = GetNode(nodeId);
            nodeId = assignment[new VariableId(node.Variable)] ? node.High : node.Low;
        }

        return GetTerminalValueByNodeIdCore(nodeId);
    }

    /// <summary>
    /// Gets the integer value for a terminal handle.
    /// </summary>
    public int GetTerminalValue(Mtbdd value)
    {
        EnsureOwned(value);
        if (!IsTerminalId(value.NodeId))
        {
            throw new InvalidOperationException("The MTBDD value is not a terminal. Evaluate it with an assignment to obtain an integer result.");
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
            throw new ArgumentException("The provided MTBDD node id is not a terminal id.", nameof(nodeId));
        }

        EnsureValidNodeId(nodeId, "Terminal");
        return GetTerminalValueByNodeIdCore(nodeId);
    }

    /// <summary>
    /// Returns diagnostics node views reachable from <paramref name="root"/>.
    /// </summary>
    public IReadOnlyList<MtbddNodeView> GetReachableNodeViews(Mtbdd root)
    {
        EnsureOwned(root);
        var reachable = new List<MtbddNodeView>();
        var visited = new HashSet<int>();
        CollectReachable(root.NodeId, visited, reachable);
        reachable.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));
        return reachable;
    }

    /// <summary>
    /// Returns integer terminal values reachable from <paramref name="root"/>.
    /// </summary>
    public IReadOnlyList<int> GetReachableTerminalValues(Mtbdd root)
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
    public DiagramStatistics GetStatistics(Mtbdd root)
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
                throw new DiagramException("MTBDD reduction rule violation: non-terminal node has Low == High. Use MakeNode to canonicalize nodes.");
            }

            EnsureChildOrdering(nodeId, node.Variable, node.Low);
            EnsureChildOrdering(nodeId, node.Variable, node.High);
            var key = new MtbddKey(node.Variable, node.Low, node.High);
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
    public void Validate(Mtbdd root)
    {
        EnsureOwned(root);
        Validate();
        EnsureValidNodeId(root.NodeId, "Root");
    }

    internal static bool IsTerminalId(int nodeId)
    {
        return nodeId < 0;
    }

    private Mtbdd Wrap(int nodeId)
    {
        return new Mtbdd(this, nodeId);
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
        if (low == high)
        {
            return low;
        }

        var key = new MtbddKey(variable, low, high);
        int existingNodeId;
        if (_uniqueTable.TryGetValue(key, out existingNodeId))
        {
            return existingNodeId;
        }

        if (_nodes.Count >= Options.MaxNodeCount)
        {
            throw new DiagramSizeLimitExceededException(
                "The MTBDD manager exceeded MaxNodeCount (" + Options.MaxNodeCount + "). Increase DecisionDiagramOptions.MaxNodeCount or simplify the integer function.");
        }

        _nodes.Add(new MtbddNode(variable, low, high));
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

    private void ValidateTruthTable(IReadOnlyList<int> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var expected = PowerOfTwo(VariableCount);
        if (values.Count != expected)
        {
            throw new ArgumentException("MTBDD truth table length must equal 2^VariableCount. Expected " + expected + " rows.", nameof(values));
        }
    }

    private void EnsureOwned(Mtbdd value)
    {
        if (!ReferenceEquals(value.Manager, this))
        {
            throw new DiagramManagerMismatchException(
                "The MTBDD operand belongs to a different MtbddManager instance. MTBDD values can only be used with the manager that created them.");
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

    private MtbddNode GetNode(int nodeId)
    {
        return _nodes[IndexFromNodeId(nodeId)];
    }

    private int GetTerminalValueByNodeIdCore(int nodeId)
    {
        return _terminalValues[TerminalIndexFromNodeId(nodeId)];
    }

    private void CollectReachable(int nodeId, HashSet<int> visited, List<MtbddNodeView> result)
    {
        if (IsTerminalId(nodeId) || !visited.Add(nodeId))
        {
            return;
        }

        var node = GetNode(nodeId);
        result.Add(new MtbddNodeView(nodeId, new VariableId(node.Variable), node.Low, node.High));
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

    private readonly struct MtbddNode
    {
        public MtbddNode(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }
    }

    private readonly struct MtbddKey : IEquatable<MtbddKey>
    {
        public MtbddKey(int variable, int low, int high)
        {
            Variable = variable;
            Low = low;
            High = high;
        }

        public int Variable { get; }

        public int Low { get; }

        public int High { get; }

        public bool Equals(MtbddKey other)
        {
            return Variable == other.Variable && Low == other.Low && High == other.High;
        }

        public override bool Equals(object obj)
        {
            return obj is MtbddKey other && Equals(other);
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
