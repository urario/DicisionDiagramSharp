namespace DecisionDiagramSharp;

/// <summary>
/// Read-only view of a ZDD node for diagnostics.
/// </summary>
public readonly struct ZddNodeView
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZddNodeView"/> struct.
    /// </summary>
    public ZddNodeView(int nodeId, VariableId variable, int lowNodeId, int highNodeId)
    {
        NodeId = nodeId;
        Variable = variable;
        LowNodeId = lowNodeId;
        HighNodeId = highNodeId;
    }

    /// <summary>
    /// Gets the node identifier.
    /// </summary>
    public int NodeId { get; }

    /// <summary>
    /// Gets the variable assigned to this node.
    /// </summary>
    public VariableId Variable { get; }

    /// <summary>
    /// Gets the low child node identifier.
    /// </summary>
    public int LowNodeId { get; }

    /// <summary>
    /// Gets the high child node identifier.
    /// </summary>
    public int HighNodeId { get; }
}
