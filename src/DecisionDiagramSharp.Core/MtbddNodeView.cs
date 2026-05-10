namespace DecisionDiagramSharp;

/// <summary>
/// Immutable diagnostics view of a reachable MTBDD node.
/// </summary>
public readonly struct MtbddNodeView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MtbddNodeView"/> struct.
    /// </summary>
    public MtbddNodeView(int nodeId, VariableId variable, int lowNodeId, int highNodeId)
    {
        NodeId = nodeId;
        Variable = variable;
        LowNodeId = lowNodeId;
        HighNodeId = highNodeId;
    }

    /// <summary>
    /// Gets the internal node identifier.
    /// </summary>
    public int NodeId { get; }

    /// <summary>
    /// Gets the variable tested by the node.
    /// </summary>
    public VariableId Variable { get; }

    /// <summary>
    /// Gets the node reached when the variable is false.
    /// </summary>
    public int LowNodeId { get; }

    /// <summary>
    /// Gets the node reached when the variable is true.
    /// </summary>
    public int HighNodeId { get; }
}
